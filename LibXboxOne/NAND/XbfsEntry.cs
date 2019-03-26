using System;
using System.Runtime.InteropServices;
using System.Text;

namespace LibXboxOne.Nand
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct XbfsEntry
    {
        /* 0x0 */ public uint LBA;
        /* 0x4 */ public uint Length;
        /* 0x8 */ public ulong Reserved;

        public override string ToString()
        {
            return ToString(false);
        }

        public string ToString(bool formatted)
        {
            string fmt = formatted ? "    " : "";

            var b = new StringBuilder();
            b.Append($"LBA: 0x{LBA:X} (0x{LBA * 0x1000:X}), ");
            b.Append($"Length: 0x{Length:X} (0x{Length * 0x1000:X}), ");
            b.Append($"Reserved: 0x{Reserved:X}");

            return b.ToString();
        }
    }
}