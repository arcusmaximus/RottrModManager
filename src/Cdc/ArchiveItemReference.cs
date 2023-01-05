namespace RottrModManager.Cdc
{
    internal class ArchiveItemReference
    {
        public ArchiveItemReference(int archiveId, int archivePart, int offset, int length)
        {
            ArchiveId = archiveId;
            ArchivePart = archivePart;
            Offset = offset;
            Length = length;
        }

        public int ArchiveId
        {
            get;
        }

        public int ArchivePart
        {
            get;
        }

        public int Offset
        {
            get;
        }

        public int Length
        {
            get;
        }

        protected bool Equals(ArchiveItemReference other)
        {
            return ArchiveId == other.ArchiveId &&
                   ArchivePart == other.ArchivePart &&
                   Offset == other.Offset &&
                   Length == other.Length;
        }

        public override bool Equals(object obj)
        {
            return obj is ArchiveItemReference other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = ArchiveId;
                hashCode = (hashCode * 397) ^ ArchivePart;
                hashCode = (hashCode * 397) ^ Offset;
                hashCode = (hashCode * 397) ^ Length;
                return hashCode;
            }
        }
    }
}
