using System;
using System.Collections.Generic;
using System.IO;

namespace RottrModManager.Shared.Cdc
{
    public class ResourceCollection
    {
        private readonly Stream _stream;
        private readonly int _numResources;
        private readonly int _resourceInfosOffset;
        private readonly int _resourceEntriesOffset;
        private List<ResourceReference> _resourceReferences;

        public ResourceCollection(uint nameHash, Stream stream)
        {
            NameHash = nameHash;
            _stream = stream;

            BinaryReader reader = new BinaryReader(stream);
            int version = reader.ReadInt32();
            if (version != 22)
                throw new NotSupportedException("Only version 22 .drm files are supported");

            int stringLength1 = reader.ReadInt32();
            int stringLength2 = reader.ReadInt32();
            int paddingLength = reader.ReadInt32();
            int drmSize = reader.ReadInt32();
            int flags = reader.ReadInt32();
            _numResources = reader.ReadInt32();
            int primary = reader.ReadInt32();

            _resourceInfosOffset = (int)_stream.Position;
            _resourceEntriesOffset = (int)stream.Position + 0x14 * _numResources + stringLength1 + stringLength2;
        }

        public uint NameHash
        {
            get;
        }

        public IReadOnlyList<ResourceReference> ResourceReferences
        {
            get
            {
                if (_resourceReferences == null)
                {
                    _resourceReferences = new List<ResourceReference>();

                    BinaryReader reader = new BinaryReader(_stream);
                    List<int> uncompressedSizes = new List<int>();
                    List<ResourceType> resourceTypes = new List<ResourceType>(_numResources);

                    _stream.Position = _resourceInfosOffset;
                    for (int i = 0; i < _numResources; i++)
                    {
                        int bodySize = reader.ReadInt32();
                        ResourceType type = (ResourceType)reader.ReadInt32();
                        int headerSize = reader.ReadInt32();
                        int id = reader.ReadInt32();
                        int locale = reader.ReadInt32();

                        uncompressedSizes.Add((headerSize >> 8) + bodySize);
                        resourceTypes.Add(type);
                    }

                    _stream.Position = _resourceEntriesOffset;
                    for (int i = 0; i < _numResources; i++)
                    {
                        int unknown1 = reader.ReadInt32();
                        int unknown2 = reader.ReadInt32();
                        short archivePart = reader.ReadInt16();
                        short archiveId = reader.ReadInt16();
                        int offsetInArchive = reader.ReadInt32();
                        int sizeInArchive = reader.ReadInt32();
                        int offsetInBatch = reader.ReadInt32();

                        _resourceReferences.Add(new ResourceReference(archiveId, archivePart, offsetInArchive, sizeInArchive, offsetInBatch, uncompressedSizes[i], resourceTypes[i]));
                    }
                }
                return _resourceReferences;
            }
        }

        public void UpdateResourceReference(int resourceIdx, ResourceReference resourceRef)
        {
            BinaryReader reader = new BinaryReader(_stream);
            BinaryWriter writer = new BinaryWriter(_stream);

            _stream.Position = _resourceInfosOffset + 0x14 * resourceIdx + 8;
            int headerSize = reader.ReadInt32() >> 8;

            _stream.Position = _resourceInfosOffset + 0x14 * resourceIdx;
            writer.Write(resourceRef.UncompressedSize - headerSize);

            _stream.Position = _resourceEntriesOffset + resourceIdx * 0x18 + 8;
            writer.Write((ushort)resourceRef.ArchivePart);
            writer.Write((ushort)resourceRef.ArchiveId);
            writer.Write(resourceRef.Offset);
            writer.Write(resourceRef.Length);
            writer.Write(resourceRef.OffsetInBatch);

            if (_resourceReferences != null)
                _resourceReferences[resourceIdx] = resourceRef;
        }
    }
}
