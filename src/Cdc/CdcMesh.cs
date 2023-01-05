using System.IO;
using RottrModManager.Util;

namespace RottrModManager.Cdc
{
    internal class CdcMesh
    {
        public CdcMesh(Stream stream)
        {
            BinaryReader reader = new BinaryReader(stream);
            reader.ReadBytes(0x10);
            long numMaterials = reader.ReadInt64();
            if (numMaterials < 0 || numMaterials > 0x1000)
                return;

            int dataSize = reader.ReadInt32();
            for (int i = 0; i < 5; i++)
            {
                int meshGroup = reader.ReadInt32();
            }
            reader.ReadInt32();
            for (long i = 0; i < numMaterials; i++)
            {
                int material = reader.ReadInt32();
            }
            reader.ReadBytes(8 * 4 + 8);
            long numHashes = reader.ReadInt64();
            if (numHashes < 0 || numHashes > 0x1000)
                return;

            for (long i = 0; i < numHashes; i++)
            {
                long hash = reader.ReadInt64();
            }

            long offsetMeshStart = numHashes * 4 + 52 + dataSize;
            reader.ReadBytes((int)(offsetMeshStart - stream.Position));

            reader.ReadBytes(0xC0);
            long offsetModelName = reader.ReadInt64();
            if (offsetModelName <= 0 || offsetModelName > 0x1000)
                return;

            reader.ReadBytes((int)(offsetMeshStart + offsetModelName - stream.Position));
            Name = reader.ReadZeroTerminatedString();
        }

        public string Name
        {
            get;
        }
    }
}
