using System;
using System.IO;
using System.Runtime.InteropServices;

namespace LibXboxOne.Vhd
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VhdDynamicDiskHeader
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x8)]
        /* 0x0  */ public char[] Cookie;
        /* 0x8  */ public ulong DataOffset;
        /* 0x10 */ public ulong TableOffset;
        /* 0x18 */ public uint HeaderVersion;
        /* 0x1C */ public uint MaxTableEntries;
        /* 0x20 */ public uint BlockSize;
        /* 0x24 */ public uint Checksum;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        /* 0x28 */ public byte[] ParentUuid;

        /* 0x38 */ public uint ParentTimeStamp;
        /* 0x3C */ public uint Reserved1;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x200)]
        /* 0x40 */ public byte[] ParentUnicodeName;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x8)]
        /* 0x240 */ public VhdParentLocatorEntry[] ParentLocatorEntries;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x100)]
        /* 0x300 */ public byte[] Reserved2;

        /* END = 0x400 */

        public static char[] GetHeaderCookie()
        {
            return new char[]{'c','x','s','p','a','r','s','e'};
        }

        public void InitDefaults()
        {
            Cookie = GetHeaderCookie(); // cxsparse
            DataOffset = 0xFFFFFFFFFFFFFFFF; // Currently unused, 0xFFFFFFFFFFFFFFFF
            TableOffset = ((ulong) 0x600).EndianSwap();
            HeaderVersion = 0x100;
            MaxTableEntries = 0;
            BlockSize = 0x00200000;
            Checksum = 0x6FF4FFFF;
            ParentUuid = new byte[0x10];
            ParentTimeStamp = 0x0;
            ParentUnicodeName = new byte[0x200];
            ParentLocatorEntries = new VhdParentLocatorEntry[8];
            Reserved2 = new byte[0x100];
        }

        internal static uint CalculateChecksum(VhdDynamicDiskHeader data)
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