using System;
using System.Runtime.InteropServices;

namespace LibXboxOne.Certificates
{
    //[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    //public struct ConsoleEndorsementCert
    //{
    //    /* 0x0 */ public uint Magic; // 0x43430004
    //    /* 0x4 */ public uint Version; // 0x00010002

    //    /* 0x8 */ public uint CertCreationTimestamp; // UNIX timestamp
    //    /* 0xC */ public uint PspRevisionId; // 01 0A 22 10 = rev B0, 00 0A 22 10 = rev A0
    //    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
    //    /* 0x10 */ public byte[] SocId; // unique console ID, probably burned into the jaguar SoC during mfg

    //    /* 0x20 */ public ushort IsPrivate; // 0x1
    //    /* 0x22 */ public ushort Unknown3;
    //    /* 0x24 */ public uint Unknown4;
    //    /* 0x28 */ public ulong Unknown5; // might be console ID

    //    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x100)]
    //    /* 0x30 */ public byte[] UniqueKey1; // some sort of key, might be console private key

    //    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x100)]
    //    /* 0x130 */ public byte[] UniqueKey2; // another key

    //    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x14)]
    //    /* 0x230 */ public char[] ConsoleSerialNumber;

    //    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x20)]
    //    /* 0x244 */ public byte[] UnknownHash; // hash of something in the cert, 0x10 - 0x244 maybe, or 0x10 - 0x20, hash might be keyed in some way

    //    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x1C)]
    //    /* 0x264 */ public char[] ConsolePartNumber;

    //    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x180)]
    //    /* 0x280 */ public byte[] CertificateSignature;
    //}

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
        public byte[] ConsoleSerialNumber;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x8)]
        public byte[] ConsoleSku;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x20)]
        public byte[] ConsoleSettingsHash; // Hash of factory settings (SHA-256)

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0xC)]
        public byte[] ConsolePartNumber;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        public byte[] SomeData; // unknown

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x180)]
        public byte[] RsaSignature;
    }
}