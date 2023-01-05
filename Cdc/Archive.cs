using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using RottrModManager.Util;

namespace RottrModManager.Cdc
{
    internal class Archive : IDisposable
    {
        private static readonly byte[] Platform =
        {
            0x70, 0x63, 0x78, 0x36, 0x34, 0x2D, 0x77, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        private const int MaxResourceChunkSize = 0x40000;

        private static readonly byte[] NonLastResourceEndMarker =
        {
            0x4E, 0x45, 0x58, 0x54, 0x10, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        private static readonly byte[] LastResourceEndMarker =
        {
            0x4E, 0x45, 0x58, 0x54, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        private readonly string _enabledNfoFilePath;
        private int _numParts = 1;
        private List<Stream> _partStreams;
        private readonly List<ResourceCollectionReference> _collectionRefs = new List<ResourceCollectionReference>();
        private int _maxResourceCollections;

        private Archive(string nfoFilePath, string baseFilePath)
        {
            _enabledNfoFilePath = Regex.Replace(nfoFilePath, @"\.disabled", "");
            BaseFilePath = baseFilePath;
        }

        public static Archive Create(
            string baseFilePath,
            int gameId,
            int version,
            int id,
            int maxResourceCollections)
        {
            string nfoFilePath = Path.ChangeExtension(baseFilePath, ".nfo");
            Archive archive = new Archive(nfoFilePath, baseFilePath)
                              {
                                  Id = id,
                                  MetaData = ArchiveMetaData.Create(nfoFilePath, gameId, version, id, id),
                                  _maxResourceCollections = maxResourceCollections
                              };

            Stream stream = File.Open(baseFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            archive._partStreams = new List<Stream> { stream };

            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(0x53464154);   // Magic
            writer.Write(4);            // Version
            writer.Write(1);            // Number of parts
            writer.Write(0);            // Number of resource collections
            writer.Write(id);           // ID
            writer.Write(Platform);

            for (int i = 0; i < maxResourceCollections; i++)
            {
                writer.Write(0);            // Hash
                writer.Write(0);            // Locale
                writer.Write(0);            // Uncompressed size
                writer.Write(0);            // Compressed size
                writer.Write((short)0);     // Archive part
                writer.Write((short)0);     // Archive ID
                writer.Write(0);            // Offset
            }

            return archive;
        }

        public static Archive Open(string baseFilePath)
        {
            string nfoFilePath = Path.ChangeExtension(baseFilePath, ".nfo");
            if (!File.Exists(nfoFilePath))
            {
                nfoFilePath += ".disabled";
                if (!File.Exists(nfoFilePath))
                    throw new FileNotFoundException();
            }

            Archive archive = new Archive(nfoFilePath, baseFilePath)
                              {
                                  MetaData = ArchiveMetaData.Load(nfoFilePath)
                              };

            using Stream stream = File.OpenRead(baseFilePath);
            BinaryReader reader = new BinaryReader(stream);

            int magic = reader.ReadInt32();
            if (magic != 0x53464154)
                throw new InvalidDataException("Invalid magic in tiger file");

            int version = reader.ReadInt32();
            if (version != 4)
                throw new NotSupportedException("Only version 4 archive files are supported");

            archive._numParts = reader.ReadInt32();

            int numResourceCollections = reader.ReadInt32();
            archive._maxResourceCollections = numResourceCollections;

            archive.Id = reader.ReadInt32();

            stream.Position = 0x34;
            for (int i = 0; i < numResourceCollections; i++)
            {
                uint hash = reader.ReadUInt32();
                int locale = reader.ReadInt32();
                int decompressedSize = reader.ReadInt32();
                int compressedSize = reader.ReadInt32();
                short archivePart = reader.ReadInt16();
                short archiveId = reader.ReadInt16();
                int offset = reader.ReadInt32();

                if (locale == -1)
                    archive._collectionRefs.Add(new ResourceCollectionReference(hash, archiveId, archivePart, offset, decompressedSize));
            }

            return archive;
        }

        private List<Stream> PartStreams
        {
            get
            {
                if (_partStreams == null)
                {
                    _partStreams = new List<Stream>();
                    for (int i = 0; i < _numParts; i++)
                    {
                        string extraPartFilePath = GetPartFilePath(i);
                        _partStreams.Add(File.Open(extraPartFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite));
                    }
                }
                return _partStreams;
            }
        }

        public string NfoFilePath => _enabledNfoFilePath + (Enabled ? "" : ".disabled");

        public string BaseFilePath
        {
            get;
        }

        public int Id
        {
            get;
            private set;
        }

        public bool Enabled
        {
            get { return File.Exists(_enabledNfoFilePath); }
            set
            {
                string disabledNfoFilePath = _enabledNfoFilePath + ".disabled";
                if (value)
                {
                    if (File.Exists(disabledNfoFilePath))
                        File.Move(disabledNfoFilePath, _enabledNfoFilePath);
                }
                else
                {
                    if (File.Exists(_enabledNfoFilePath))
                        File.Move(_enabledNfoFilePath, disabledNfoFilePath);
                }
            }
        }

        public ArchiveMetaData MetaData
        {
            get;
            private set;
        }

        public string ModName
        {
            get
            {
                string entry = MetaData.CustomEntries.FirstOrDefault(c => c.StartsWith("mod:"));
                return entry?.Substring("mod:".Length);
            }
        }

        public IReadOnlyCollection<ResourceCollectionReference> ResourceCollections => _collectionRefs;

        public ResourceCollection GetResourceCollection(ResourceCollectionReference collectionRef)
        {
            if (collectionRef.ArchiveId != Id)
                throw new ArgumentException();

            Stream stream = PartStreams[collectionRef.ArchivePart];
            stream.Position = collectionRef.Offset;
            return new ResourceCollection(collectionRef.NameHash, stream);
        }

        public Stream OpenResource(ResourceReference resourceRef)
        {
            if (resourceRef.ArchiveId != Id)
                throw new ArgumentException();

            Stream stream = PartStreams[resourceRef.ArchivePart];
            return new ResourceReadStream(stream, resourceRef, true);
        }

        public byte[] GetBytes(ArchiveItemReference itemRef)
        {
            if (itemRef.ArchiveId != Id)
                throw new ArgumentException();

            byte[] data = new byte[itemRef.Length];

            Stream stream = PartStreams[itemRef.ArchivePart];
            stream.Position = itemRef.Offset;
            stream.Read(data, 0, data.Length);
            return data;
        }

        public ResourceCollection AddResourceCollection(uint nameHash, byte[] data)
        {
            if (_collectionRefs.Count == _maxResourceCollections)
                throw new InvalidOperationException("Can't add any further resource collections");

            Stream collectionStream = PartStreams.Last();
            BinaryWriter collectionWriter = new BinaryWriter(collectionStream);
            int offset = (int)collectionStream.Length;
            collectionStream.Position = offset;
            collectionWriter.Write(data);

            Stream indexStream = PartStreams[0];
            BinaryWriter indexWriter = new BinaryWriter(indexStream);
            indexStream.Position = 0x34 + _collectionRefs.Count * 0x18;
            indexWriter.Write(nameHash);
            indexWriter.Write(-1);
            indexWriter.Write(data.Length);
            indexWriter.Write(0);
            indexWriter.Write((short)0);
            indexWriter.Write((short)Id);
            indexWriter.Write(offset);

            _collectionRefs.Add(new ResourceCollectionReference(nameHash, Id, 0, offset, data.Length));
            indexStream.Position = 0xC;
            indexWriter.Write(_collectionRefs.Count);

            collectionStream.Position = offset;
            return new ResourceCollection(nameHash, collectionStream);
        }

        public ArchiveItemReference AddResource(Stream contentStream)
        {
            int archivePart = PartStreams.Count - 1;
            Stream partStream = PartStreams[archivePart];
            partStream.Position = partStream.Length;

            BinaryWriter writer = new BinaryWriter(partStream);
            writer.Align16();

            partStream.Position -= 0x10;
            BinaryReader reader = new BinaryReader(partStream);
            byte[] prevMarker = reader.ReadBytes(0x10);
            if (prevMarker.SequenceEqual(LastResourceEndMarker))
            {
                partStream.Position -= 0x10;
                writer.Write(NonLastResourceEndMarker);
            }

            int resourceOffset = (int)partStream.Position;
            WriteResource(contentStream, writer);
            int resourceLength = (int)partStream.Position - resourceOffset;

            writer.Write(LastResourceEndMarker);

            return new ArchiveItemReference(Id, archivePart, resourceOffset, resourceLength);
        }

        private void WriteResource(Stream contentStream, BinaryWriter writer)
        {
            if (contentStream is ResourceReadStream resourceStream)
            {
                Stream archivePartStream = resourceStream.ArchivePartStream;
                ResourceReference resourceRef = resourceStream.ResourceReference;
                archivePartStream.CopySegmentTo(resourceRef.Offset, resourceRef.Length, writer.BaseStream);
                return;
            }

            Stream partStream = writer.BaseStream;

            int numChunks = (int)(contentStream.Length / MaxResourceChunkSize);
            if ((contentStream.Length % MaxResourceChunkSize) != 0)
                numChunks++;

            writer.Write(0x4D524443);      // Magic
            writer.Write(0);               // Type
            writer.Write(numChunks);
            writer.Write(0);

            int chunkSizesOffset = (int)partStream.Position;
            for (int i = 0; i < numChunks; i++)
            {
                writer.Write(0);
                writer.Write(0);
            }
            writer.Align16();

            long remainingSize = contentStream.Length;
            byte[] uncompressedChunkData = new byte[MaxResourceChunkSize];
            for (int i = 0; i < numChunks; i++)
            {
                int uncompressedChunkSize = (int)Math.Min(remainingSize, MaxResourceChunkSize);
                contentStream.Read(uncompressedChunkData, 0, uncompressedChunkSize);

                int chunkOffset = (int)partStream.Position;
                writer.Write((byte)0x78);
                writer.Write((byte)0x9C);
                using (DeflateStream compressor = new DeflateStream(partStream, CompressionMode.Compress, true))
                {
                    compressor.Write(uncompressedChunkData, 0, uncompressedChunkSize);
                }
                writer.Write(0);
                int compressedChunkSize = (int)partStream.Position - chunkOffset;

                partStream.Position = chunkSizesOffset;
                writer.Write((uncompressedChunkSize << 8) | 0x02);
                writer.Write(compressedChunkSize);
                chunkSizesOffset += 8;

                partStream.Position = partStream.Length;
                writer.Align16();

                remainingSize -= uncompressedChunkSize;
            }
        }

        private string GetPartFilePath(int part)
        {
            return BaseFilePath.Replace(".000.tiger", $".{part:d03}.tiger");
        }

        public void CloseStreams()
        {
            if (_partStreams == null)
                return;

            foreach (Stream stream in _partStreams)
            {
                stream.Dispose();
            }
            _partStreams = null;
        }

        public void Delete()
        {
            File.Delete(_enabledNfoFilePath);
            File.Delete(_enabledNfoFilePath + ".disabled");
            Dispose();
            for (int i = 0; i < _numParts; i++)
            {
                File.Delete(GetPartFilePath(i));
            }
        }

        public override string ToString()
        {
            return Path.GetFileName(BaseFilePath);
        }

        public void Dispose()
        {
            CloseStreams();
        }
    }
}
