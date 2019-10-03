using System;
using System.Runtime.InteropServices;

namespace LibXboxOne.Certificates
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct BootCapabilityCert
    {
        public ushort StructId;
        public ushort Size;
        public ushort ProtocolVersion;
        public ushort IssuerKeyId;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] IssueDate;

        public DateTime IssueDateTime
        {
            get
            {
                var filetime = BitConverter.ToInt64(IssueDate, 0);
                return DateTime.FromFileTime(filetime);
            }
        }

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        public byte[] SocId;

        public ushort GenerationId;
        public byte AllowedStates;
        public byte LastCapability;
        public uint Flags;
        public byte ExpireCentury;
        public byte ExpireYear;
        public byte ExpireMonth;
        public byte ExpireDayOfMonth;
        public byte ExpireHour;
        public byte ExpireMinute;
        public byte ExpireSecond;
        public byte MinimumSpVersion;
        public ulong Minimum2blVersion;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        public byte[] Nonce;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x38)]
        public byte[] Reserved;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x100)]
        public BootCapability[] Capabilities;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x180)]
        public byte[] RsaSignature;
    }
}