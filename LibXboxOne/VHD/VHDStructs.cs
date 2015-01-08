using System.IO;
using System.Runtime.InteropServices;

namespace LibXboxOne
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VhdParentLocatorEntry
    {
        /* 0x0  */ public uint Code;
        /* 0x4  */ public uint DataSpace;
        /* 0x8  */ public uint DataLength;
        /* 0xC  */ public uint Reserved;
        /* 0x10  */ public ulong DataOffset;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VhdDynamicDiskHeader
    {
        /* 0x0  */ public ulong Cookie;
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

        public void InitDefaults()
        {
            Cookie = 0x6573726170737863;
            DataOffset = 0xFFFFFFFFFFFFFFFF;
            TableOffset = ((ulong) 0x600).EndianSwap();
            HeaderVersion = 0x100;
            MaxTableEntries = 0x00200000;
            BlockSize = 0x800;
            Checksum = 0x6FF4FFFF;
            ParentUuid = new byte[0x10];
            ParentUnicodeName = new byte[0x200];
            ParentLocatorEntries = new VhdParentLocatorEntry[8];
            Reserved2 = new byte[0x100];
        }
        public void CalculateChecksum()
        {
            Checksum = 0;
            using (var ms = new MemoryStream())
            {
                var writer = new BinaryWriter(ms);
                var reader = new BinaryReader(ms);
                writer.WriteStruct(this);
                reader.BaseStream.Position = 0;
                while (reader.BaseStream.Length > reader.BaseStream.Position)
                    Checksum += reader.ReadByte();
            }
            Checksum = ~Checksum;
            Checksum = Checksum.EndianSwap();
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VhdFooter
    {
        /* 0x0  */ public ulong Cookie;
        /* 0x8  */ public uint Features;
        /* 0xC  */ public uint Version;
        /* 0x10 */ public ulong DataOffset;
        /* 0x18 */ public uint TimeStamp;
        /* 0x1C */ public uint CreatorApp;
        /* 0x20 */ public uint CreatorVer;
        /* 0x24 */ public uint CreatorOS;
        /* 0x28 */ public ulong OrigSize;
        /* 0x30 */ public ulong CurSize;
        /* 0x38 */ public ushort DiskGeometryCylinders;
        /* 0x3A */ public byte DiskGeometryHeads;
        /* 0x3B */ public byte DiskGeometrySectors;
        /* 0x3C */ public uint DiskType;
        /* 0x40 */ public uint Checksum;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        /* 0x44 */ public byte[] UniqueId;

        /* 0x54 */ public byte SavedState;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x1AB)]
        /* 0x55 */ public byte[] Reserved;

        public void InitDefaults()
        {
            Cookie = 0x78697463656e6f63; // conectix
            Features = ((uint)0x2).EndianSwap();
            Version = 0x100;
            DataOffset = 0xffffffffffffffff;
            TimeStamp = 0x0;
            CreatorApp = 0x206e6977;
            CreatorVer = 0x03000600;
            CreatorOS = 0x6b326957;
            OrigSize = 0x40000000;
            CurSize = 0x40000000;
            DiskGeometryCylinders = 0x2008;
            DiskGeometryHeads = 0x10;
            DiskGeometrySectors = 0x3f;
            DiskType = ((uint) 2).EndianSwap();
            Checksum = 0x0;
            UniqueId = new byte[0x10];
            SavedState = 0;
            Reserved = new byte[0x1AB];
        }

        public void CalculateChecksum()
        {
            Checksum = 0;
            using (var ms = new MemoryStream())
            {
                var writer = new BinaryWriter(ms);
                var reader = new BinaryReader(ms);
                writer.WriteStruct(this);
                reader.BaseStream.Position = 0;
                while (reader.BaseStream.Length > reader.BaseStream.Position)
                    Checksum += reader.ReadByte();
            }
            Checksum = ~Checksum;
            Checksum = Checksum.EndianSwap();
        }
    }
}
