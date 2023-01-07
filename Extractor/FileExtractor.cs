using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using RottrModManager.Shared;
using RottrModManager.Shared.Cdc;

namespace RottrExtractor
{
    internal class FileExtractor
    {
        private readonly ArchiveSet _archiveSet;

        public FileExtractor(ArchiveSet archiveSet)
        {
            _archiveSet = archiveSet;
        }

        public void Extract(List<ArchiveFileReference> files, string folderPath, ITaskProgress progress, CancellationToken cancellationToken)
        {
            try
            {
                progress.Begin("Extracting files...");

                for (int i = 0; i < files.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    ArchiveFileReference file = files[i];
                    byte[] data = _archiveSet.GetBlob(file);

                    string filePath = Path.Combine(folderPath, file.Name);
                    if (file.Locale != -1)
                    {
                        string localeFileName = $"Locale {file.Locale:X08}";
                        string localeName = GetLocaleName(file.Locale);
                        if (localeName != null)
                            localeFileName += " " + localeName;

                        filePath = Path.Combine(filePath, localeFileName + Path.GetExtension(filePath));
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                    File.WriteAllBytes(filePath, data);

                    progress.Report((float)(i + 1) / files.Count);
                }
            }
            finally
            {
                _archiveSet.CloseStreams();
                progress.End();
            }
        }

        private static string GetLocaleName(int value)
        {
            foreach (Locale locale in Enum.GetValues(typeof(Locale)).Cast<Locale>().OrderBy(l => (int)l))
            {
                if (((int)locale & value) != 0)
                    return Regex.Replace(locale.ToString(), @"(?<=[a-z])[A-Z0-9]", "-$0").ToLower();
            }
            return null;
        }
    }
}
