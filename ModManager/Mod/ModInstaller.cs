using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using RottrModManager.Shared;
using RottrModManager.Shared.Cdc;
using RottrModManager.Shared.Util;
using RottrModManager.Util;

namespace RottrModManager.Mod
{
    internal class ModInstaller
    {
        private readonly ArchiveSet _archiveSet;
        private readonly ResourceUsageCache _resourceUsageCache;

        public ModInstaller(ArchiveSet archiveSet, ResourceUsageCache resourceUsageCache)
        {
            _archiveSet = archiveSet;
            _resourceUsageCache = resourceUsageCache;
        }

        public InstalledMod InstallFromZip(string filePath, ITaskProgress progress, CancellationToken cancellationToken)
        {
            string modName = Regex.Replace(Path.GetFileNameWithoutExtension(filePath), @"-\d+$", "");
            if (IsModAlreadyInstalled(modName))
                return null;

            using ZipTempExtractor extractor = new ZipTempExtractor(filePath);
            extractor.Extract(progress, cancellationToken);

            return InstallWithDuplicateCheck(modName, extractor.FolderPath, progress, cancellationToken);
        }

        public InstalledMod InstallFromFolder(string folderPath, ITaskProgress progress, CancellationToken cancellationToken)
        {
            string modName = Path.GetFileName(folderPath);
            if (IsModAlreadyInstalled(modName))
                return null;

            return InstallWithDuplicateCheck(modName, folderPath, progress, cancellationToken);
        }

        public void ReinstallAll(ITaskProgress progress, CancellationToken cancellationToken)
        {
            List<ArchiveInfo> originalArchives = new List<ArchiveInfo>();
            foreach (Archive archive in _archiveSet.Archives
                                                   .Where(a => a.ModName != null)
                                                   .OrderByDescending(a => a.MetaData.Version))
            {
                archive.CloseStreams();
                ArchiveInfo archiveInfo = new ArchiveInfo
                                          {
                                              NfoFilePath = Path.Combine(_archiveSet.FolderPath, "_orig_" + Path.GetFileName(archive.NfoFilePath)),
                                              BaseFilePath = Path.Combine(_archiveSet.FolderPath, "_orig_" + Path.GetFileName(archive.BaseFilePath)),
                                              ModName = archive.ModName,
                                              Enabled = archive.Enabled
                                          };

                File.Delete(archiveInfo.NfoFilePath);
                File.Move(archive.NfoFilePath, archiveInfo.NfoFilePath);

                File.Delete(archiveInfo.BaseFilePath);
                File.Move(archive.BaseFilePath, archiveInfo.BaseFilePath);

                _archiveSet.Delete(archive, progress, cancellationToken);
                originalArchives.Insert(0, archiveInfo);
            }

            foreach (ArchiveInfo archiveInfo in originalArchives)
            {
                using (ModPackage modPackage = new TigerModPackage(archiveInfo.BaseFilePath))
                {
                    InstalledMod installedMod = Install(archiveInfo.ModName, modPackage, progress, cancellationToken);
                    if (installedMod == null)
                        continue;

                    if (!archiveInfo.Enabled)
                    {
                        Archive archive = _archiveSet.GetArchive(installedMod.ArchiveId);
                        _archiveSet.Disable(archive, progress, cancellationToken);
                    }
                }

                File.Delete(archiveInfo.NfoFilePath);
                File.Delete(archiveInfo.BaseFilePath);
            }
        }

        private struct ArchiveInfo
        {
            public string NfoFilePath;
            public string BaseFilePath;
            public string ModName;
            public bool Enabled;
        }

        private bool IsModAlreadyInstalled(string modName)
        {
            if (_archiveSet.Archives.Any(a => a.ModName == modName))
            {
                MessageBox.Show($"The mod {modName} is already installed.", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }
            return false;
        }

        private InstalledMod InstallWithDuplicateCheck(string modName, string modFolderPath, ITaskProgress progress, CancellationToken cancellationToken)
        {
            bool hasDuplicates = false;
            HashSet<string> drmFileNames = new HashSet<string>();
            foreach (string drmFilePath in Directory.EnumerateFiles(modFolderPath, "*.drm", SearchOption.AllDirectories))
            {
                if (!drmFileNames.Add(Path.GetFileName(drmFilePath)))
                {
                    hasDuplicates = true;
                    break;
                }
            }

            if (hasDuplicates &&
                MessageBox.Show(
                    "The mod contains duplicate files. Possibly it has subfolders with variants to choose from. " +
                    "If this is the case, please abort now and install a subfolder instead.\r\n\r\n" +
                    "Continue with the installation?",
                    "Warning",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                ) == DialogResult.No)
            {
                return null;
            }

            return Install(modName, new FolderModPackage(modFolderPath), progress, cancellationToken);
        }

        private InstalledMod Install(string modName, ModPackage modPackage, ITaskProgress progress, CancellationToken cancellationToken)
        {
            Archive archive = null;
            try
            {
                progress.Begin($"Installing mod {modName}...");

                Dictionary<string, List<ResourceCollectionItemReference>> modResourceUsages = GetModResourceUsages(modPackage);

                List<ArchiveFileReference> origCollectionRefs = modResourceUsages.Values
                                                                                 .SelectMany(c => c)
                                                                                 .Select(c => c.Collection)
                                                                                 .Distinct()
                                                                                 .ToList();
                archive = _archiveSet.CreateModArchive(modName, origCollectionRefs.Count);

                Dictionary<uint, ResourceCollection> newCollections = AddResourceCollections(archive, origCollectionRefs);
                AddResources(archive, modPackage, modResourceUsages, newCollections, progress, cancellationToken);

                _archiveSet.Add(archive, progress, cancellationToken);
                return new InstalledMod(archive.Id, modName, true);
            }
            catch
            {
                if (archive != null)
                {
                    archive.CloseStreams();
                    File.Delete(Path.ChangeExtension(archive.BaseFilePath, ".nfo"));
                    File.Delete(archive.BaseFilePath);
                }
                throw;
            }
            finally
            {
                _archiveSet.CloseStreams();
                progress.End();
            }
        }

        private Dictionary<string, List<ResourceCollectionItemReference>> GetModResourceUsages(ModPackage modPackage)
        {
            var modResourceUsages = new Dictionary<string, List<ResourceCollectionItemReference>>();
            Dictionary<string, string> modResourceNames = GetModResourceNames(modPackage);
            AddModResourceUsagesByName(modResourceNames, modResourceUsages);
            AddModResourceUsagesByIndex(modPackage, modResourceNames, modResourceUsages);
            return modResourceUsages;
        }

        private Dictionary<string, string> GetModResourceNames(ModPackage modPackage)
        {
            Dictionary<string, string> modResourceNames = new Dictionary<string, string>();
            foreach (string resourceKey in modPackage.ResourceKeys)
            {
                using Stream stream = modPackage.OpenResource(resourceKey);
                string name = ResourceNaming.GetName(stream, modPackage.GetResourceType(resourceKey));
                if (name != null && !modResourceNames.Values.Contains(name))
                    modResourceNames.Add(resourceKey, name);
            }
            return modResourceNames;
        }

        private void AddModResourceUsagesByName(
            Dictionary<string, string> modResourceNames,
            Dictionary<string, List<ResourceCollectionItemReference>> modResourceUsages)
        {
            foreach ((string resourceKey, string resourceName) in modResourceNames)
            {
                modResourceUsages[resourceKey] = _resourceUsageCache.GetUsages(resourceName).ToList();
            }
        }

        private void AddModResourceUsagesByIndex(
            ModPackage modPackage,
            Dictionary<string, string> modResourceNames,
            Dictionary<string, List<ResourceCollectionItemReference>> modResourceUsages)
        {
            foreach (string resourceKey in modPackage.ResourceKeys.Where(k => !modResourceNames.ContainsKey(k)))
            {
                ResourceType type = modPackage.GetResourceType(resourceKey);
                if (type == ResourceType.Unknown)
                    continue;

                foreach ((ResourceCollection modCollection, int modResourceIdx) in modPackage.GetResourceUsages(resourceKey))
                {
                    ArchiveFileReference gameCollectionRef = _archiveSet.GetFileReference(modCollection.NameHash);
                    if (gameCollectionRef == null)
                        continue;

                    ResourceCollection gameCollection = _archiveSet.GetResourceCollection(gameCollectionRef);

                    int resourceIdxWithinType = -1;
                    int numModResourcesOfType = 0;
                    for (int i = 0; i < modCollection.ResourceReferences.Count; i++)
                    {
                        if (modCollection.ResourceReferences[i].Type != type)
                            continue;

                        if (i == modResourceIdx)
                            resourceIdxWithinType = numModResourcesOfType;

                        numModResourcesOfType++;
                    }

                    int gameResourceIdx = -1;
                    int numGameResourcesOfType = 0;
                    for (int i = 0; i < gameCollection.ResourceReferences.Count; i++)
                    {
                        if (gameCollection.ResourceReferences[i].Type != type)
                            continue;

                        if (numGameResourcesOfType == resourceIdxWithinType)
                            gameResourceIdx = i;

                        numGameResourcesOfType++;
                    }

                    if (numModResourcesOfType == numGameResourcesOfType)
                    {
                        modResourceUsages.GetOrAdd(resourceKey, () => new List<ResourceCollectionItemReference>())
                                         .Add(new ResourceCollectionItemReference(gameCollectionRef, gameResourceIdx));
                    }
                }
            }
        }

        private Dictionary<uint, ResourceCollection> AddResourceCollections(Archive archive, List<ArchiveFileReference> origCollectionRefs)
        {
            Dictionary<uint, ResourceCollection> newCollections = new Dictionary<uint, ResourceCollection>();
            foreach (ArchiveFileReference origCollectionRef in origCollectionRefs.OrderBy(c => c.NameHash))
            {
                ArchiveFileReference newCollectionRef = archive.AddFile(origCollectionRef.NameHash, -1, _archiveSet.GetBlob(origCollectionRef));
                ResourceCollection newCollection = archive.GetResourceCollection(newCollectionRef);
                newCollections.Add(newCollection.NameHash, newCollection);
            }
            return newCollections;
        }

        private void AddResources(
            Archive archive,
            ModPackage modPackage,
            Dictionary<string, List<ResourceCollectionItemReference>> modResourceUsages,
            Dictionary<uint, ResourceCollection> newCollections,
            ITaskProgress progress,
            CancellationToken cancellationToken)
        {
            int resourceIdx = 0;
            int offsetInBatch = 0;
            foreach ((string modResourceKey, List<ResourceCollectionItemReference> collectionItemRefs) in modResourceUsages)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using Stream modResourceStream = modPackage.OpenResource(modResourceKey);
                ArchiveBlobReference newResource = archive.AddResource(modResourceStream);
                foreach (ResourceCollectionItemReference collectionItemRef in collectionItemRefs)
                {
                    ResourceCollection newCollection = newCollections[collectionItemRef.Collection.NameHash];
                    newCollection.UpdateResourceReference(
                        collectionItemRef.ResourceIndex,
                        new ResourceReference(
                            newResource.ArchiveId,
                            newResource.ArchivePart,
                            newResource.Offset,
                            newResource.Length,
                            offsetInBatch,
                            (int)modResourceStream.Length,
                            modPackage.GetResourceType(modResourceKey)
                        )
                    );
                }

                offsetInBatch += (int)modResourceStream.Length;
                offsetInBatch = (offsetInBatch + 0xF) & ~0xF;

                resourceIdx++;
                progress.Report((float)(resourceIdx + 1) / modResourceUsages.Count);
            }
        }
    }
}
