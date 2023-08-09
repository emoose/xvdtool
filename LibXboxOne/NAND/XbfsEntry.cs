using System.Runtime.InteropServices;
using System.Text;

namespace LibXboxOne.Nand
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct XbfsEntry
    {
        /* 0x0 */ public uint LBA;
        /* 0x4 */ public uint BlockCount;
        /* 0x8 */ public ulong Reserved;

        public long Length => BlockCount * XbfsFile.BlockSize;

        public long Offset(XbfsFlavor flavor) {
            var offset = LBA * XbfsFile.BlockSize;
            if (flavor == XbfsFlavor.XboxSeries && offset >= XbfsFile.SeriesOffsetDiff)
                offset -= XbfsFile.SeriesOffsetDiff;
            return offset;
        }

        public override string ToString()
        {
            return ToString(false);
        }

        public string ToString(bool formatted)
        {
            var b = new StringBuilder();
            b.Append($"LBA: 0x{LBA:X} (One: 0x{Offset(XbfsFlavor.XboxOne):X} Series: 0x{Offset(XbfsFlavor.XboxSeries):X}), ");
            b.Append($"Length: 0x{BlockCount:X} (0x{Length:X}), ");
            b.Append($"Reserved: 0x{Reserved:X}");

            return b.ToString();
        }
    }
}