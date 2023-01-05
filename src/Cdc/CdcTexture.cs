using System.IO;
using System.Text;
using RottrModManager.Util;

namespace RottrModManager.Cdc
{
    internal class CdcTexture
    {
        public CdcTexture(Stream stream)
        {
            using BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, true);
            reader.ReadBytes(0x1C);
            Name = reader.ReadZeroTerminatedString();
        }

        public string Name
        {
            get;
        }
    }
}
