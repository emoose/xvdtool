using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace LibXboxOne
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct XbfsEntry
    {
        /* 0x0 */ public uint LBA;
        /* 0x4 */ public uint Length;
        /* 0x8 */ public ulong Reserved;

        public override string ToString()
        {
            return ToString(false);
        }

        public string ToString(bool formatted)
        {
            string fmt = formatted ? "    " : "";

            var b = new StringBuilder();
            b.Append(String.Format("LBA: 0x{0:X} (0x{1:X}), ", LBA, LBA * 0x1000));
            b.Append(String.Format("Length: 0x{0:X} (0x{1:X}), ", Length, Length * 0x1000));
            b.Append(String.Format("Reserved: 0x{0:X}", Reserved));

            return b.ToString();
        }
    }

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

    // XBFS header, can be at 0x10000, 0x810000 or 0x820000
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct XbfsHeader
    {
        public static readonly int DataToHash = 0x3E0;
        public static readonly string XbfsMagic = "SFBX";

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        /* 0x0 */ public char[] Magic; // SFBX

        /* 0x4 */ public byte FormatVersion;
        /* 0x5 */ public byte SequenceNumber; // Indicates latest filesystem, wraps around: 0xFF -> 0x00
        /* 0x6 */ public ushort LayoutVersion; // 3
        /* 0x8 */ public ulong Reserved08; // 0
        /* 0x10 */ public ulong Reserved10; // 0
        /* 0x18 */ public ulong Reserved18; // 0

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x3A)]
        /* 0x20 */ public XbfsEntry[] Files;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        /* 0x3C0 */ public byte[] Reserved3C0;

        /* 0x3D0 */ public Guid SystemXVID; // GUID

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x20)]
        /* 0x3E0 */ public byte[] XbfsHash; // SHA256 hash of 0x0 - 0x3E0

        public string MagicString
        {
            get
            {
                return new string(Magic);
            }
        }

        public bool IsValid
        {
            get
            {
                return MagicString == XbfsMagic;
            }
        }

        public bool IsHashValid
        {
            get
            {
                return XbfsHash.IsEqualTo(CalculateHash());
            }
        }

        byte[] CalculateHash()
        {
            byte[] data = Shared.StructToBytes(this);
            return HashUtils.ComputeSha256(data, 0, DataToHash);
        }

        public void Rehash()
        {
            XbfsHash = CalculateHash();
        }

        public override string ToString()
        {
            return ToString(false);
        }

        public string ToString(bool formatted)
        {            
            string fmt = formatted ? "    " : "";

            var b = new StringBuilder();
            b.AppendLineSpace(fmt + "Magic: " + new string(Magic));
            b.AppendLineSpace(fmt + "Format Version: 0x" + FormatVersion.ToString("X"));
            b.AppendLineSpace(fmt + "Sequence Number: 0x" + SequenceNumber.ToString("X"));
            b.AppendLineSpace(fmt + "Layout Version: 0x" + LayoutVersion.ToString("X"));
            b.AppendLineSpace(fmt + "Reserved08: 0x" + Reserved08.ToString("X"));
            b.AppendLineSpace(fmt + "Reserved10: 0x" + Reserved10.ToString("X"));
            b.AppendLineSpace(fmt + "Reserved18: 0x" + Reserved18.ToString("X"));
            b.AppendLineSpace(fmt + "Reserved3C0: " + Reserved3C0.ToHexString());
            b.AppendLineSpace(fmt + "System XVID: " + SystemXVID);
            b.AppendLineSpace(fmt + "XBFS header hash: " + Environment.NewLine + fmt + XbfsHash.ToHexString());
            b.AppendLine();

            for(int i = 0; i < Files.Length; i++)
            {
                XbfsEntry entry = Files[i];
                if (entry.Length == 0)
                    continue;
                b.AppendLine($"File {i}: {XbfsFile.XbfsFilenames[i]} {entry.ToString(formatted)}");
            }

            return b.ToString();
        }
    }

    public class XbfsFile
    {
        public static readonly int BlockSize = 0x1000;
        public static readonly int[] XbfsOffsets = { 0x10000, 0x810000, 0x820000 };
        public static string[] XbfsFilenames =
        {
            "1smcbl_a.bin", // 0
            "header.bin", // 1
            "devkit.ini", // 2
            "mtedata.cfg", // 3
            "certkeys.bin", // 4
            "smcerr.log", // 5
            "system.xvd", // 6
            "$sosrst.xvd", // 7
            "download.xvd", // 8
            "smc_s.cfg", // 9
            "sp_s.cfg", // 10, keyvault? has serial/partnum/osig, handled by psp.sys (/Device/psp)
            "os_s.cfg", // 11
            "smc_d.cfg", // 12
            "sp_d.cfg", // 13
            "os_d.cfg", // 14
            "smcfw.bin", // 15
            "boot.bin", // 16
            "host.xvd", // 17
            "settings.xvd", // 18
            "1smcbl_b.bin", // 19
            "bootanim.bin", // 20, this entry and ones below it are only in retail 97xx and above?
            "sostmpl.xvd", // 21
            "update.cfg", // 22
            "sosinit.xvd", // 23
            "hwinit.cfg", // 24
            "qaslt.xvd", // 25
            "keyvault.bin", // 26, keyvault backup? has serial/partnum/osig
            "unknown2.bin", // 27
            "unknown3.bin", // 28
            "unknownBlank2.bin" // 29
        };

        private readonly IO _io;
        private readonly string _filePath;

        public List<XbfsHeader> XbfsHeaders;
        public PspConsoleCert ConsoleCert;

        public string FilePath
        {
            get { return _filePath; }
        }

        public XbfsFile(string path)
        {
            _filePath = path;
            _io = new IO(path);
        }

        public static long FromLBA(uint lba)
        {
            return lba * BlockSize;
        }

        public static uint ToLBA(long offset)
        {
            return (uint)(offset / BlockSize);
        }

        public bool Load()
        {
            // read each XBFS header
            XbfsHeaders = new List<XbfsHeader>();
            foreach (int offset in XbfsOffsets)
            {
                _io.Stream.Position = offset;
                var header = _io.Reader.ReadStruct<XbfsHeader>();
                XbfsHeaders.Add(header);
            }

            long spDataSize = SeekToFile("sp_s.cfg");
            if (spDataSize <= 0)
                return true;

            // SP_S.cfg: (secure processor secured config? there's also a blank sp_d.cfg which is probably secure processor decrypted config)
            // 0x0    - 0x200   - signature?
            // 0x200  - 0x5200  - encrypted data? maybe loaded and decrypted into PSP memory?
            // 0x5200 - 0x5400  - blank
            // 0x5400 - 0x5800  - console certificate
            // 0x5800 - 0x6000  - unknown data, looks like it has some hashes and the OSIG of the BR drive
            // 0x6000 - 0x6600  - encrypted data?
            // 0x6600 - 0x7400  - blank
            // 0x7400 - 0x7410  - unknown data, hash maybe
            // 0x7410 - 0x40000 - blank

            _io.Stream.Position += 0x5400; // seek to start of unencrypted data in sp_s (console certificate)
            ConsoleCert = _io.Reader.ReadStruct<PspConsoleCert>();

            return true;
        }

        // returns the size of the file if found
        public long SeekToFile(string fileName)
        {
            int idx = Array.IndexOf(XbfsFilenames, fileName);
            if (idx < 0)
                return 0;
            long size = 0;
            for (int i = 0; i < XbfsHeaders.Count; i++)
            {
                if (!XbfsHeaders[i].IsValid)
                    continue;
                if (idx >= XbfsHeaders[i].Files.Length)
                    continue;
                var ent = XbfsHeaders[i].Files[idx];
                if (ent.Length == 0)
                    continue;
                _io.Stream.Position = FromLBA(ent.LBA);
                size = FromLBA(ent.Length);
            }
            return size;
        }

        public string GetXbfsInfo()
        {
            var info = new Dictionary<long, string>();
            for (int i = 0; i < XbfsHeaders.Count; i++)
            {
                if (!XbfsHeaders[i].IsValid)
                    continue;
                for (int y = 0; y < XbfsHeaders[i].Files.Length; y++)
                {
                    var ent = XbfsHeaders[i].Files[y];
                    if (ent.Length == 0)
                        continue;
                    long start = FromLBA(ent.LBA);
                    long length = FromLBA(ent.Length);
                    long end = start + length;
                    string addInfo = String.Format("{0:X} {1}_{2}", end, i, y);
                    if (info.ContainsKey(start))
                        info[start] += " " + addInfo;
                    else
                        info.Add(start, addInfo);
                }
            }
            string infoStr = String.Empty;
            var keys = info.Keys.ToList();
            keys.Sort();
            foreach (var key in keys)
                infoStr += "" + key.ToString("X") + " - " + info[key] + Environment.NewLine;

            return infoStr;
        }

        public void ExtractXbfsData(string folderPath)
        {
            var doneAddrs = new List<long>();
            for (int i = 0; i < XbfsHeaders.Count; i++)
            {
                if (!XbfsHeaders[i].IsValid)
                    continue;
                for (int y = 0; y < XbfsHeaders[i].Files.Length; y++)
                {
                    var ent = XbfsHeaders[i].Files[y];
                    if (ent.Length == 0)
                        continue;

                    string fileName = FromLBA(ent.LBA).ToString("X") + "_" + FromLBA(ent.Length).ToString("X") + "_" + i + "_" + y + "_" + XbfsFilenames[y];

                    long read = 0;
                    long total = FromLBA(ent.Length);
                    _io.Stream.Position = FromLBA(ent.LBA);

                    bool writeFile = true;
                    if (doneAddrs.Contains(_io.Stream.Position))
                    {
                        writeFile = false;
                        fileName = "DUPE_" + fileName;
                    }
                    doneAddrs.Add(_io.Stream.Position);


                    if (_io.Stream.Position + total > _io.Stream.Length)
                        continue;

                    using (var fileIo = new IO(Path.Combine(folderPath, fileName), FileMode.Create))
                    {
                        if(writeFile)
                            while (read < total)
                            {
                                int toRead = 0x4000;
                                if (total - read < toRead)
                                    toRead = (int) (total - read);
                                byte[] data = _io.Reader.ReadBytes(toRead);
                                fileIo.Writer.Write(data);
                                read += toRead;
                            }
                    }
                }
            }
        }


        public override string ToString()
        {
            return ToString(false);
        }

        public string ToString(bool formatted)
        {
            var b = new StringBuilder();
            b.AppendLine("XbfsFile");
            b.AppendLine();
            for (int i = 0; i < XbfsHeaders.Count; i++)
            {
                if(!XbfsHeaders[i].IsValid)
                    continue;

                b.AppendLine(String.Format("XbfsHeader slot {0}: (0x{1:X})", i, XbfsOffsets[i]));
                b.Append(XbfsHeaders[i].ToString(formatted));
            }
            return b.ToString();
        }
    }
}
