using System.IO;
using System.Text;

namespace RottrModManager.Shared.Cdc
{
    internal class CdcMaterial
    {
        public CdcMaterial(Stream stream)
        {
            byte[] data = new byte[(int)stream.Length];
            stream.Read(data, 0, data.Length);

            int nameOffset = data.Length - 1;
            if (data[nameOffset] != 0x00)
                return;

            while (nameOffset > 0 && data[nameOffset - 1] >= 0x40)
            {
                nameOffset--;
            }
            if (nameOffset == 0 || nameOffset == data.Length - 1)
                return;

            Name = Encoding.ASCII.GetString(data, nameOffset, data.Length - 1 - nameOffset);
        }

        public string Name
        {
            get;
        }
    }
}
