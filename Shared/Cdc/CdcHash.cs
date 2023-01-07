using System.Collections.Generic;
using System.IO;
using RottrModManager.Shared.Util;

namespace RottrModManager.Shared.Cdc
{
    public static class CdcHash
    {
        private static readonly Dictionary<uint, string> LookupTable = new Dictionary<uint, string>();

        public static void AddLookups(string filePath)
        {
            using StreamReader reader = new StreamReader(filePath);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                LookupTable[Calculate(line)] = line;
            }
        }

        public static uint Calculate(string str)
        {
            uint dwHash = 0xFFFFFFFFu;
            foreach (char c in str)
            {
                dwHash ^= (uint)c << 24;

                for (int j = 0; j < 8; j++)
                {
                    if ((dwHash & 0x80000000) != 0)
                    {
                        dwHash = (dwHash << 1) ^ 0x04C11DB7u;
                    }
                    else
                    {
                        dwHash <<= 1;
                    }
                }
            }
            return ~dwHash;
        }

        public static string Lookup(uint hash)
        {
            return LookupTable.GetOrDefault(hash);
        }
    }
}
