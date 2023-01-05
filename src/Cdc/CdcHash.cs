namespace RottrModManager.Cdc
{
    internal static class CdcHash
    {
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
    }
}
