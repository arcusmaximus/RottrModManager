using System.IO;
using System.Text;
using RottrModManager.Shared.Util;

namespace RottrModManager.Shared.Cdc
{
    internal class CdcSound
    {
        public CdcSound(Stream stream)
        {
            BinaryReader reader = new BinaryReader(stream);
            reader.ReadBytes(0x14);

            string magic = Encoding.ASCII.GetString(reader.ReadBytes(4));
            if (magic != "FSB5")
                return;

            int version = reader.ReadInt32();
            int numSamples = reader.ReadInt32();
            int sampleHeaderSize = reader.ReadInt32();
            int nameTableSize = reader.ReadInt32();
            int dataSize = reader.ReadInt32();
            int mode = reader.ReadInt32();
            reader.ReadBytes(0x20);

            if (numSamples != 1 || nameTableSize == 0)
                return;

            reader.ReadBytes(sampleHeaderSize);
            reader.ReadBytes(4);
            Name = reader.ReadZeroTerminatedString();
        }

        public string Name
        {
            get;
        }
    }
}
