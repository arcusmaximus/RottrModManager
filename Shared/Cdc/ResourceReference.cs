namespace RottrModManager.Shared.Cdc
{
    public class ResourceReference : ArchiveBlobReference
    {
        public ResourceReference(
            int archiveId,
            int archivePart,
            int offsetInArchive,
            int sizeInArchive,
            int offsetInBatch,
            int uncompressedSize,
            ResourceType type)
            : base(archiveId, archivePart, offsetInArchive, sizeInArchive)
        {
            OffsetInBatch = offsetInBatch;
            UncompressedSize = uncompressedSize;
            Type = type;
        }

        public int OffsetInBatch
        {
            get;
        }

        public int UncompressedSize
        {
            get;
        }

        public ResourceType Type
        {
            get;
        }
    }
}
