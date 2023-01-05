﻿namespace RottrModManager.Cdc
{
    internal class ResourceCollectionItemReference
    {
        public ResourceCollectionItemReference(ResourceCollectionReference collectionRef, int resourceIndex)
        {
            Collection = collectionRef;
            ResourceIndex = resourceIndex;
        }

        public ResourceCollectionReference Collection
        {
            get;
        }

        public int ResourceIndex
        {
            get;
        }

        protected bool Equals(ResourceCollectionItemReference other)
        {
            return Equals(Collection, other.Collection) &&
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
                return (Collection.GetHashCode() * 397) ^ ResourceIndex;
            }
        }
    }
}