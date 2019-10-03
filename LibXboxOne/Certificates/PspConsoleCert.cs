using System;
using System.Runtime.InteropServices;
using System.Text;

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

        public DateTime IssueDateTime
        {
            get
            {
                return DateTime.UnixEpoch.AddSeconds(IssueDate);
            }
        }

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

                public override string ToString()
        {
            return ToString(false);
        }

        public string ToString(bool formatted)
        {
            string fmt = formatted ? "    " : "";

            var b = new StringBuilder();
            b.AppendLineSpace("PspConsoleCert:");
            b.AppendLineSpace(fmt + $"StructId: 0x{StructID:X}");
            b.AppendLineSpace(fmt + $"Size: {Size} (0x{Size:X})");
            b.AppendLineSpace(fmt + $"IssuerKeyId: {IssuerKeyId} (0x{IssuerKeyId:X})");
            b.AppendLineSpace(fmt + $"ProtocolVer: {ProtocolVer} (0x{ProtocolVer:X})");
            b.AppendLineSpace(fmt + $"IssueDateTime: {IssueDateTime} ({IssueDate})");
            b.AppendLineSpace(fmt + $"PspRevisionId: {PspRevisionId} (0x{PspRevisionId:X})");
            b.AppendLineSpace(fmt + $"SocId: {SocId.ToHexString()}");
            b.AppendLineSpace(fmt + $"GenerationId: {GenerationId} (0x{GenerationId:X})");
            b.AppendLineSpace(fmt + $"ConsoleRegion: {ConsoleRegion} (0x{ConsoleRegion:X})");
            b.AppendLineSpace(fmt + $"ReservedByte: {ReservedByte} (0x{ReservedByte:X})");
            b.AppendLineSpace(fmt + $"ReservedDword: {ReservedDword} (0x{ReservedDword:X})");
            b.AppendLineSpace(fmt + $"VendorId: {VendorId.ToHexString()}");
            b.AppendLineSpace(fmt + $"AttestationPubKey: {Environment.NewLine}{fmt}{AttestationPubKey.ToHexString()}");
            b.AppendLineSpace(fmt + $"ReservedPublicKey: {Environment.NewLine}{fmt}{ReservedPublicKey.ToHexString()}");
            b.AppendLineSpace(fmt + $"ConsoleSerialNumberString: {ConsoleSerialNumberString}");
            b.AppendLineSpace(fmt + $"ConsoleSku: {ConsoleSku.ToHexString()}");
            b.AppendLineSpace(fmt + $"ConsoleSettingsHash: {ConsoleSettingsHash.ToHexString(String.Empty)}");
            b.AppendLineSpace(fmt + $"ConsolePartNumberString: {ConsolePartNumberString}");
            b.AppendLineSpace(fmt + $"SomeData: {SomeData.ToHexString()}");
            b.AppendLineSpace(fmt + $"RsaSignature:  {Environment.NewLine}{fmt}{RsaSignature.ToHexString()}");

            b.AppendLine();
            return b.ToString();
        }
    }
}