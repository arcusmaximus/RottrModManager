namespace RottrModManager.Shared.Cdc
{
    public class ResourceCollectionReference : ArchiveItemReference
    {
        public ResourceCollectionReference(uint nameHash, int archiveId, int archivePart, int offset, int length)
            : base(archiveId, archivePart, offset, length)
        {
            NameHash = nameHash;
        }

        public uint NameHash
        {
            get;
        }
    }
}
