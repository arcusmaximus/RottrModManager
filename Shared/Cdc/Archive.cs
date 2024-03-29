﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using RottrModManager.Shared.Util;

namespace RottrModManager.Shared.Cdc
{
    public class Archive : IDisposable
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
        private readonly List<ArchiveFileReference> _fileRefs = new List<ArchiveFileReference>();
        private int _maxFiles;

        private Archive(string nfoFilePath, string baseFilePath)
        {
            _enabledNfoFilePath = Regex.Replace(nfoFilePath, @"\.disabled", "");
            BaseFilePath = baseFilePath;
        }

        public static Archive Create(string baseFilePath, int gameId, int version, int id, int maxFiles)
        {
            string nfoFilePath = Path.ChangeExtension(baseFilePath, ".nfo");
            Archive archive = new Archive(nfoFilePath, baseFilePath)
                              {
                                  Id = id,
                                  MetaData = ArchiveMetaData.Create(nfoFilePath, gameId, version, id, id),
                                  _maxFiles = maxFiles
                              };

            Stream stream = File.Open(baseFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            archive._partStreams = new List<Stream> { stream };

            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(0x53464154);   // Magic
            writer.Write(4);            // Version
            writer.Write(1);            // Number of parts
            writer.Write(0);            // Number of files
            writer.Write(id);           // ID
            writer.Write(Platform);     // Platform

            for (int i = 0; i < maxFiles; i++)
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

            int numFiles = reader.ReadInt32();
            archive._maxFiles = numFiles;

            archive.Id = reader.ReadInt32();

            stream.Position = 0x34;
            for (int i = 0; i < numFiles; i++)
            {
                uint hash = reader.ReadUInt32();
                int locale = reader.ReadInt32();
                int decompressedSize = reader.ReadInt32();
                int compressedSize = reader.ReadInt32();
                short archivePart = reader.ReadInt16();
                short archiveId = reader.ReadInt16();
                int offset = reader.ReadInt32();

                archive._fileRefs.Add(new ArchiveFileReference(hash, locale, archiveId, archivePart, offset, decompressedSize));
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
                        string partFilePath = GetPartFilePath(i);
                        _partStreams.Add(File.Open(partFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite));
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

        public IReadOnlyCollection<ArchiveFileReference> Files => _fileRefs;

        public ResourceCollection GetResourceCollection(ArchiveFileReference file)
        {
            if (file.ArchiveId != Id)
                throw new ArgumentException();

            Stream stream = PartStreams[file.ArchivePart];
            stream.Position = file.Offset;
            try
            {
                return new ResourceCollection(file.NameHash, stream);
            }
            catch
            {
                return null;
            }
        }

        public Stream OpenResource(ResourceReference resourceRef)
        {
            if (resourceRef.ArchiveId != Id)
                throw new ArgumentException();

            Stream stream = PartStreams[resourceRef.ArchivePart];
            return new ResourceReadStream(stream, resourceRef, true);
        }

        public byte[] GetBlob(ArchiveBlobReference blobRef)
        {
            if (blobRef.ArchiveId != Id)
                throw new ArgumentException();

            byte[] data = new byte[blobRef.Length];

            Stream stream = PartStreams[blobRef.ArchivePart];
            stream.Position = blobRef.Offset;
            stream.Read(data, 0, data.Length);
            return data;
        }

        public ArchiveFileReference AddFile(ArchiveFileIdentifier identifier, byte[] data)
        {
            return AddFile(identifier.NameHash, identifier.Locale, data);
        }

        public ArchiveFileReference AddFile(uint nameHash, int locale, byte[] data)
        {
            if (_fileRefs.Count == _maxFiles)
                throw new InvalidOperationException("Can't add any further files");

            Stream contentStream = PartStreams.Last();
            BinaryWriter contentWriter = new BinaryWriter(contentStream);
            int offset = (int)contentStream.Length;
            contentStream.Position = offset;
            contentWriter.Write(data);

            Stream indexStream = PartStreams[0];
            BinaryWriter indexWriter = new BinaryWriter(indexStream);
            indexStream.Position = 0x34 + _fileRefs.Count * 0x18;
            indexWriter.Write(nameHash);
            indexWriter.Write(locale);
            indexWriter.Write(data.Length);
            indexWriter.Write(0);
            indexWriter.Write((short)0);
            indexWriter.Write((short)Id);
            indexWriter.Write(offset);

            ArchiveFileReference fileRef = new ArchiveFileReference(nameHash, locale, Id, 0, offset, data.Length);
            _fileRefs.Add(fileRef);
            indexStream.Position = 0xC;
            indexWriter.Write(_fileRefs.Count);

            return fileRef;
        }

        public ArchiveBlobReference AddResource(Stream contentStream)
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

            return new ArchiveBlobReference(Id, archivePart, resourceOffset, resourceLength);
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

        public string GetPartFilePath(int part)
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
