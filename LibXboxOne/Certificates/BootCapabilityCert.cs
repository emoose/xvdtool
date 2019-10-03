using System;
using System.Runtime.InteropServices;
using System.Text;

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

                public override string ToString()
        {
            return ToString(false);
        }

        public string ToString(bool formatted)
        {
            string fmt = formatted ? "    " : "";

            var b = new StringBuilder();
            b.AppendLineSpace("BootCapabilityCert:");

            if (StructId == 0x00)
            {
                b.AppendLineSpace(fmt + "! No or invalid certificate (StructId: 0)\n");
                return b.ToString();
            }

            b.AppendLineSpace(fmt + $"StructId: 0x{StructId:X}");
            b.AppendLineSpace(fmt + $"Size: {Size} (0x{Size:X})");
            b.AppendLineSpace(fmt + $"IssuerKeyId: {IssuerKeyId} (0x{IssuerKeyId:X})");
            b.AppendLineSpace(fmt + $"IssueDate: {IssueDateTime}");
            b.AppendLineSpace(fmt + $"SocId: {Environment.NewLine}{fmt}{SocId.ToHexString()}");
            b.AppendLineSpace(fmt + $"GenerationId: {GenerationId} (0x{GenerationId:X})");
            b.AppendLineSpace(fmt + $"AllowedStates: {AllowedStates} (0x{AllowedStates:X})");
            b.AppendLineSpace(fmt + $"LastCapability: {LastCapability} (0x{LastCapability:X})");
            b.AppendLineSpace(fmt + $"Flags: {Flags} (0x{Flags:X})");

            var expiry = $"{ExpireCentury}{ExpireYear:00}-{ExpireMonth:00}-{ExpireDayOfMonth:00} {ExpireHour:00}:{ExpireMinute:00}:{ExpireSecond:00}";
            if (ExpireCentury == 255)
                expiry = "never!";
            b.AppendLineSpace(fmt + $"Expiration date: {expiry}");

            b.AppendLineSpace(fmt + $"MinimumSpVersion: {MinimumSpVersion} (0x{MinimumSpVersion:X})");
            b.AppendLineSpace(fmt + $"Minimum2blVersion: {Minimum2blVersion} (0x{Minimum2blVersion:X})");

            b.AppendLineSpace(fmt + $"Nonce: {Environment.NewLine}{fmt}{Nonce.ToHexString()}");
            b.AppendLineSpace(fmt + $"Reserved: {Environment.NewLine}{fmt}{Reserved.ToHexString()}");
            b.AppendLineSpace(fmt + $"RsaSignature:  {Environment.NewLine}{fmt}{RsaSignature.ToHexString()}");

            b.AppendLineSpace(fmt + "Boot Capabilities:");
            for(int i = 0; i < Capabilities.Length; i++)
            {
                BootCapability cap = Capabilities[i];

                if (cap == BootCapability.CERT_CAP_NONE
                 || cap == BootCapability.CERT_CAP_DELETED)
                {
                    continue;
                }
                b.AppendLine($"{fmt}-{cap}");
            }

            b.AppendLine();
            return b.ToString();
        }
    }
}