using System;
using System.Runtime.InteropServices;

namespace LibXboxOne.Certificates
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct PspConsoleCert
    {
        public UInt16 StructID; // 0x4343 (ASCII: CC = ConsoleCert?)

        public UInt16 Size;

        public UInt16 IssuerKeyId;  // Key Version

        public UInt16 ProtocolVer;  // unknown

        public UInt32 IssueDate;   // POSIX time

        public UInt32 PspRevisionId; // PSP Version

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        public byte[] SocId;

        public UInt16 GenerationId;

        public byte ConsoleRegion;

        public byte ReservedByte; // 0

        public UInt32 ReservedDword; // 0

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x8)]
        public byte[] VendorId; // size of 8

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x100)]
        public byte[] AttestationPubKey; // Public key that is used by the Xbox One ChalResp system

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x100)]
        public byte[] ReservedPublicKey;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0xC)]
        public char[] ConsoleSerialNumber;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x8)]
        public byte[] ConsoleSku;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x20)]
        public byte[] ConsoleSettingsHash; // Hash of factory settings (SHA-256)

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0xC)]
        public char[] ConsolePartNumber;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        public byte[] SomeData; // unknown

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x180)]
        public byte[] RsaSignature;

        public string ConsoleSerialNumberString
        {
            get
            {
                return new string(ConsoleSerialNumber);
            }
        }

        public string ConsolePartNumberString
        {
            get
            {
                return new string(ConsolePartNumber);
            }
        }
    }
}