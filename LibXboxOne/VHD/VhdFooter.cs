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
        /* 0x8  */ public uint Features;
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
        /* 0x38 */ public VhdDiskGeometry DiskGeometry;
        /* 0x3C */ public uint DiskType;
        /* 0x40 */ public uint Checksum;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        /* 0x44 */ public byte[] UniqueId;

        /* 0x54 */ public byte SavedState;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x1AB)]
        /* 0x55 */ public byte[] Reserved;

    	static uint VHD_FILEFORMAT_VERSION => 0x00010000;
        static uint VHD_CREATOR_VERSION => 0x000A0000;

        public static char[] GetHeaderCookie()
        {
            return new char[]{'c','o','n','e','c','t','i','x'};
        }

        static VhdFooter CreateWithDefaultValues(ulong driveSize, byte[] uniqueId)
        {
            return new VhdFooter()
            {
                Cookie = GetHeaderCookie(), // conectix
                Features = ((uint)VhdDiskFeatures.Reserved).EndianSwap(),
                FileFormatVersion = VHD_FILEFORMAT_VERSION.EndianSwap(),
                TimeStamp = VhdUtils.GetTimestamp(DateTime.UtcNow).EndianSwap(),
                CreatorApp = VhdCreatorApplication.WindowsDiskMngmt,
                CreatorVersion = VHD_CREATOR_VERSION.EndianSwap(),
                CreatorHostOS = VhdCreatorHostOs.Windows,
                OriginalSize = driveSize.EndianSwap(),
                CurrentSize = driveSize.EndianSwap(),
                DiskGeometry = VhdUtils.CalculateDiskGeometry(driveSize),
                Checksum = 0x0,
                UniqueId = uniqueId,
                SavedState = 0,
                Reserved = new byte[0x1AB]
            };
        }

        public static VhdFooter CreateForFixedDisk(ulong driveSize, byte[] uniqueId)
        {
            var footer = CreateWithDefaultValues(driveSize, uniqueId);
            footer.DiskType = ((uint)VhdDiskType.Fixed).EndianSwap();
            footer.DataOffset = 0xffffffffffffffff;

            footer.FixChecksum();
            return footer;
        }

        public static VhdFooter CreateForDynamicDisk(ulong driveSize, byte[] uniqueId)
        {
            var footer = CreateWithDefaultValues(driveSize, uniqueId);
            footer.DiskType = ((uint)VhdDiskType.Dynamic).EndianSwap();
            footer.DataOffset = ((ulong)0x200).EndianSwap(); // Offset of dynamic disk header (size of footer)

            footer.FixChecksum();
            return footer;
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