using System.IO;
using System.Text;
using RottrModManager.Shared.Util;

namespace RottrModManager.Shared.Cdc
{
    public class CdcTexture
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
