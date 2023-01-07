namespace RottrModManager.Shared.Cdc
{
    public class ArchiveFileReference : ArchiveBlobReference
    {
        public ArchiveFileReference(uint nameHash, int locale, int archiveId, int archivePart, int offset, int length)
            : base(archiveId, archivePart, offset, length)
        {
            NameHash = nameHash;
            Locale = locale;
        }

        public uint NameHash
        {
            get;
        }

        public string Name
        {
            get
            {
                string name = CdcHash.Lookup(NameHash);
                return name ?? $"Unknown\\{NameHash:X08}";
            }
        }

        public int Locale
        {
            get;
        }
    }
}
