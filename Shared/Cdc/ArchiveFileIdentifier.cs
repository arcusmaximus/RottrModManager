namespace RottrModManager.Shared.Cdc
{
    public readonly struct ArchiveFileIdentifier
    {
        public ArchiveFileIdentifier(uint nameHash, int locale)
        {
            NameHash = nameHash;
            Locale = locale;
        }

        public uint NameHash
        {
            get;
        }

        public int Locale
        {
            get;
        }

        public bool Equals(ArchiveFileIdentifier other)
        {
            return NameHash == other.NameHash && Locale == other.Locale;
        }

        public override bool Equals(object obj)
        {
            return obj is ArchiveFileIdentifier other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)NameHash * 397) ^ Locale;
            }
        }
    }
}
