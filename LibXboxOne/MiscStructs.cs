using System.Runtime.InteropServices;

namespace LibXboxOne
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct XviHeader
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x240)]
        /* 0x0 */
        public byte[] Unknown1;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        /* 0x240 */
        public byte[] ContentId;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        /* 0x250 */
        public byte[] VDUID;
        /* 0x260 */
        public ulong Unknown4;
        /* 0x268 */
        public uint Unknown5; // should be 1
        /* 0x26C */
        public uint Unknown6; // should be r9d
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct EkbFile
    {   // EKBs also seem to follow the XvcLicenseBlock format, but with 2 byte block ids instead of 4 byte.
        // note: this is just here for reference as EKB files are written sequentially like this, but EKB files should be loaded with the XvcLicenseBlock class now since it's assumed that's how the xbox reads them

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x4)]
        /* 0x0 */
        public byte[] Magic; // EKB1

        /* 0x4 */
        public uint HeaderLength; // (length of data proceeding this)
        /* 0x8 */
        public ushort Unknown1; // 1?
        /* 0xA */
        public uint Unknown2; // 2?
        /* 0xE */
        public ushort Unknown3; // 5?
        /* 0x10 */
        public ushort Unknown4; // 2?
        /* 0x12 */
        public uint Unknown5; // 0x20?

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x20)]
        /* 0x16 */
        public byte[] KeyIdHexChars;

        /* 0x36 */
        public ushort Unknown7; // 3?
        /* 0x38 */
        public uint Unknown8; // 2?
        /* 0x3C */
        public ushort Unknown9; // 6? lots of calcs for this
        /* 0x3E */
        public ushort Unknown10; // 4?
        /* 0x40 */
        public uint Unknown11; // 1?
        /* 0x44 */
        public byte Unknown12; // 0x31? 
        /* 0x45 */
        public ushort Unknown13; // 5?
        /* 0x47 */
        public uint Unknown14; // 2?
        /* 0x4B */
        public ushort Unknown15; // 1?
        /* 0x4D */
        public ushort Unknown16; // 7?
        /* 0x4F */
        public uint EncryptedDataLength; // EncryptedDataLength

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x180)]
        /* 0x53 */
        public byte[] EncryptedData;
    }
}
