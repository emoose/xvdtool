using System;
using System.IO;
using System.Runtime.InteropServices;

namespace LibXboxOne.Vhd
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VhdFooter
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x8)]
        /* 0x0  */ public char[] Cookie;
        /* 0x8  */ public VhdDiskFeatures Features;
        /* 0xC  */ public uint FileFormatVersion;
        /* 0x10 */ public ulong DataOffset;
        /* 0x18 */ public uint TimeStamp;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x4)]
        /* 0x1C */ public byte[] CreatorApp;
        /* 0x20 */ public uint CreatorVersion;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x4)]
        /* 0x24 */ public byte[] CreatorHostOS;
        /* 0x28 */ public ulong OriginalSize;
        /* 0x30 */ public ulong CurrentSize;
        /* 0x38 */ public uint DiskGeometry;
        /* 0x3C */ public VhdDiskType DiskType;
        /* 0x40 */ public uint Checksum;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        /* 0x44 */ public byte[] UniqueId;

        /* 0x54 */ public byte SavedState;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x1AB)]
        /* 0x55 */ public byte[] Reserved;

        public static char[] GetHeaderCookie()
        {
            return new char[]{'c','o','n','e','c','t','i','x'};
        }

        public void InitDefaults()
        {
            Cookie = GetHeaderCookie(); // conectix
            Features = VhdDiskFeatures.None;
            FileFormatVersion = 0x00010000;
            DataOffset = 0xffffffffffffffff; // Fixed disk: 0xffffffffffffffff, Others: Real value
            TimeStamp = 0x0;
            CreatorApp = VhdCreatorApplication.WindowsDiskMngmt;
            CreatorVersion = 0x03000600;
            CreatorHostOS = VhdCreatorHostOs.Windows;
            OriginalSize = 0x40000000;
            CurrentSize = 0x40000000;
            DiskGeometry = 0x2008;
            DiskType = VhdDiskType.None;
            Checksum = 0x0;
            UniqueId = new byte[0x10];
            SavedState = 0;
            Reserved = new byte[0x1AB];
        }

        internal static uint CalculateChecksum(VhdFooter data)
        {
            uint checksum = 0;
            using (var ms = new MemoryStream())
            {
                var writer = new BinaryWriter(ms);
                var reader = new BinaryReader(ms);
                writer.WriteStruct(data);
                reader.BaseStream.Position = 0;
                while (reader.BaseStream.Length > reader.BaseStream.Position)
                    checksum += reader.ReadByte();
            }
            checksum = ~checksum;
            return checksum.EndianSwap();
        }

        public void FixChecksum()
        {
            Checksum = CalculateChecksum(this);
        }
    }
}