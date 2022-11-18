using System;
using System.Text;
using System.Runtime.InteropServices;

namespace LibXboxOne.Nand
{
    // XBFS header, can be at 0x10000, 0x810000 or 0x820000
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct XbfsHeader
    {
        public static readonly int DataToHash = 0x3E0;
        public static readonly string XbfsMagic = "SFBX";

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        /* 0x0 */ public char[] Magic; // SFBX

        /* 0x4 */ public byte FormatVersion;
        /* 0x5 */ public byte SequenceNumber; // Indicates latest filesystem, wraps around: 0xFF -> 0x00
        /* 0x6 */ public ushort LayoutVersion; // 3
        /* 0x8 */ public ulong Reserved08; // 0
        /* 0x10 */ public ulong Reserved10; // 0
        /* 0x18 */ public ulong Reserved18; // 0

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x3A)]
        /* 0x20 */ public XbfsEntry[] Files;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        /* 0x3C0 */ public byte[] Reserved3C0;

        /* 0x3D0 */ public Guid SystemXVID; // GUID

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x20)]
        /* 0x3E0 */ public byte[] XbfsHash; // SHA256 hash of 0x0 - 0x3E0

        public string MagicString => new string(Magic);

        public bool IsValid => MagicString == XbfsMagic;

        public bool IsHashValid => XbfsHash.IsEqualTo(CalculateHash());

        byte[] CalculateHash()
        {
            byte[] data = Shared.StructToBytes(this);
            return HashUtils.ComputeSha256(data, 0, DataToHash);
        }

        public void Rehash()
        {
            XbfsHash = CalculateHash();
        }

        public override string ToString()
        {
            return ToString(false);
        }

        public string ToString(bool formatted)
        {            
            string fmt = formatted ? "    " : "";

            var b = new StringBuilder();
            b.AppendLineSpace(fmt + $"Magic: {new string(Magic)}");
            b.AppendLineSpace(fmt + $"Format Version: 0x{FormatVersion:X}");
            b.AppendLineSpace(fmt + $"Sequence Number: 0x{SequenceNumber:X}");
            b.AppendLineSpace(fmt + $"Layout Version: 0x{LayoutVersion:X}");
            b.AppendLineSpace(fmt + $"Reserved08: 0x{Reserved08:X}");
            b.AppendLineSpace(fmt + $"Reserved10: 0x{Reserved10:X}");
            b.AppendLineSpace(fmt + $"Reserved18: 0x{Reserved18:X}");
            b.AppendLineSpace(fmt + $"Reserved3C0: {Reserved3C0.ToHexString()}");
            b.AppendLineSpace(fmt + $"System XVID: {SystemXVID}");
            b.AppendLineSpace(fmt + $"XBFS header hash: {Environment.NewLine}{fmt}{XbfsHash.ToHexString()}");
            b.AppendLine();

            for(int i = 0; i < Files.Length; i++)
            {
                XbfsEntry entry = Files[i];
                if (entry.Length == 0)
                    continue;
                b.AppendLine($"File {i}: {XbfsFile.GetFilenameForIndex(i) ?? "<Unknown>"} {entry.ToString(formatted)}");
            }

            return b.ToString();
        }
    }
}