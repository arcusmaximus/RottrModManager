using System.Collections.Generic;
using System.IO;
using System.Threading;
using RottrModManager.Shared;
using RottrModManager.Shared.Cdc;

namespace RottrExtractor
{
    internal class ResourceExtractor
    {
        private readonly ArchiveSet _archiveSet;

        public ResourceExtractor(ArchiveSet archiveSet)
        {
            _archiveSet = archiveSet;
        }

        public void Extract(
            ArchiveFileReference collectionRef,
            List<ResourceReference> resourceRefs,
            string folderPath,
            ITaskProgress progress,
            CancellationToken cancellationToken)
        {
            try
            {
                progress.Begin("Extracting...");

                string collectionFileName = Path.GetFileName(collectionRef.Name);
                folderPath = Path.Combine(folderPath, Path.GetFileNameWithoutExtension(collectionFileName));
                Directory.CreateDirectory(folderPath);

                byte[] collectionBytes = _archiveSet.GetBlob(collectionRef);
                File.WriteAllBytes(Path.Combine(folderPath, collectionFileName), collectionBytes);

                for (int i = 0; i < resourceRefs.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    ResourceReference resourceRef = resourceRefs[i];
                    string filePath = Path.Combine(folderPath, resourceRef.Type.ToString(), ResourceNaming.GetFilePath(_archiveSet, resourceRef, i));
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                    using Stream resourceStream = _archiveSet.OpenResource(resourceRef);
                    using Stream fileStream = File.Create(filePath);
                    resourceStream.CopyTo(fileStream);

                    progress.Report((float)(i + 1) / resourceRefs.Count);
                }
            }
            finally
            {
                _archiveSet.CloseStreams();
                progress.End();
            }
        }
    }
}
