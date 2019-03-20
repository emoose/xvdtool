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
            b.Append(String.Format("LBA: 0x{0:X} (0x{1:X}), ", LBA, LBA * 0x1000));
            b.Append(String.Format("Length: 0x{0:X} (0x{1:X}), ", Length, Length * 0x1000));
            b.Append(String.Format("Reserved: 0x{0:X}", Reserved));

            return b.ToString();
        }
    }
}