namespace RottrModManager.Shared.Cdc
{
    public class ResourceCollectionItemReference
    {
        public ResourceCollectionItemReference(ArchiveFileReference collectionRef, int resourceIndex)
        {
            CollectionReference = collectionRef;
            ResourceIndex = resourceIndex;
        }

        public ArchiveFileReference CollectionReference
        {
            get;
        }

        public int ResourceIndex
        {
            get;
        }

        protected bool Equals(ResourceCollectionItemReference other)
        {
            return Equals(CollectionReference, other.CollectionReference) &&
                   ResourceIndex == other.ResourceIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is ResourceCollectionItemReference other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (CollectionReference.GetHashCode() * 397) ^ ResourceIndex;
            }
        }
    }
}
