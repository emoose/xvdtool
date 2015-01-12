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
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        /* 0x8 */ public byte[] Padding;

        public override string ToString()
        {
            return ToString(false);
        }

        public string ToString(bool formatted)
        {
            var b = new StringBuilder();
            b.AppendLine("XbfsEntry:");

            string fmt = formatted ? "    " : "";

            if (!Padding.IsArrayEmpty())
                b.AppendLineSpace(fmt + "Padding != null");

            b.AppendLine();

            b.AppendLineSpace(fmt + "LBA: 0x" + LBA.ToString("X") + " (0x" + (LBA * 0x1000).ToString("X") + ")");
            b.AppendLineSpace(fmt + "Length: 0x" + Length.ToString("X") + " (0x" + (Length * 0x1000).ToString("X") + ")");
            b.AppendLineSpace(fmt + "Padding: " + Padding.ToHexString());

            return b.ToString();
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct ConsoleEndorsementCert
    {
        /* 0x0 */ public uint Magic; // 0x43430004
        /* 0x4 */ public uint Version; // 0x00010002

        /* 0x8 */ public uint CertCreationTimestamp; // UNIX timestamp
        /* 0xC */ public uint PspRevisionId; // 01 0A 22 10 = rev B0, 00 0A 22 10 = rev A0
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        /* 0x10 */ public byte[] SocId; // unique console ID, probably burned into the jaguar SoC during mfg

        /* 0x20 */ public ushort IsPrivate; // 0x1
        /* 0x22 */ public ushort Unknown3;
        /* 0x24 */ public uint Unknown4;
        /* 0x28 */ public ulong Unknown5; // might be console ID

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x100)]
        /* 0x30 */ public byte[] UniqueKey1; // some sort of key, might be console private key
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x100)]
        /* 0x130 */ public byte[] UniqueKey2; // another key

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x14)]
        /* 0x230 */ public char[] ConsoleSerialNumber;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x20)]
        /* 0x244 */ public byte[] UnknownHash; // hash of something in the cert, 0x10 - 0x244 maybe, or 0x10 - 0x20, hash might be keyed in some way

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x1C)]
        /* 0x264 */ public char[] ConsolePartNumber;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x180)]
        /* 0x280 */ public byte[] CertificateSignature;
    }

    // XBFS header, can be at 0x10000, 0x810000 or 0x820000
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct XbfsHeader
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        /* 0x0 */ public char[] Magic; // SFBX

        /* 0x4 */ public byte Version; // 1
        /* 0x5 */ public byte BootSlot; // 1 or 2, has to match with boot slot location (1 = 0x10000, 2 = 0x810000, not sure about the value for 0x820000 but i guess its 3)
        /* 0x6 */ public byte Unknown1; // 3
        /* 0x7 */ public byte Unknown2; // 0
        /* 0x8 */ public int Unknown3; // 0
        /* 0xC */ public int Unknown4; // 0

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        /* 0x10 */ public byte[] Unknown5;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x3A)]
        /* 0x20 */ public XbfsEntry[] Entries;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        /* 0x3C0 */ public byte[] Unknown6;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        /* 0x3D0 */ public byte[] Unknown7;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x20)]
        /* 0x3E0 */ public byte[] XbfsHash; // SHA256 hash of 0x0 - 0x3E0

        public override string ToString()
        {
            return ToString(false);
        }

        public string ToString(bool formatted)
        {
            var b = new StringBuilder();
            b.AppendLine("XbfsHeader:");

            string fmt = formatted ? "    " : "";

            if (Version != 1)
                b.AppendLineSpace(fmt + "Version != 1");
            if (Unknown1 != 3)
                b.AppendLineSpace(fmt + "Unknown1 != 3");
            if (Unknown2 != 0)
                b.AppendLineSpace(fmt + "Unknown2 != 0");
            if (Unknown3 != 0)
                b.AppendLineSpace(fmt + "Unknown3 != 0");
            if (Unknown4 != 0)
                b.AppendLineSpace(fmt + "Unknown4 != 0");
            if (!Unknown5.IsArrayEmpty())
                b.AppendLineSpace(fmt + "Unknown5 != null");
            if (!Unknown6.IsArrayEmpty())
                b.AppendLineSpace(fmt + "Unknown6 != null");
            if (!Unknown7.IsArrayEmpty())
                b.AppendLineSpace(fmt + "Unknown7 != null");

            b.AppendLine();
            b.AppendLineSpace(fmt + "Magic: " + new string(Magic));
            b.AppendLineSpace(fmt + "Version: 0x" + Version.ToString("X"));
            b.AppendLineSpace(fmt + "Boot Slot: 0x" + BootSlot.ToString("X"));
            b.AppendLineSpace(fmt + "Unknown1: 0x" + Unknown1.ToString("X"));
            b.AppendLineSpace(fmt + "Unknown2: 0x" + Unknown2.ToString("X"));
            b.AppendLineSpace(fmt + "Unknown3: 0x" + Unknown3.ToString("X"));
            b.AppendLineSpace(fmt + "Unknown4: 0x" + Unknown4.ToString("X"));
            b.AppendLineSpace(fmt + "Unknown5: 0x" + Unknown5.ToHexString());
            b.AppendLineSpace(fmt + "Unknown6: 0x" + Unknown6.ToHexString());
            b.AppendLineSpace(fmt + "Unknown7: 0x" + Unknown7.ToHexString());
            b.AppendLineSpace(fmt + "XBFS header hash: " + Environment.NewLine + fmt + XbfsHash.ToHexString());

            for(int i = 0; i < Entries.Length; i++)
            {
                XbfsEntry entry = Entries[i];
                b.AppendLine("Entry " + i);
                b.Append(entry.ToString(formatted));
            }

            return b.ToString();
        }
    }

    public class XbfsFile
    {
        public static int[] XbfsOffsets = { 0x10000, 0x810000, 0x820000 };
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
        public ConsoleEndorsementCert ConsoleCert;

        public string FilePath
        {
            get { return _filePath; }
        }

        public XbfsFile(string path)
        {
            _filePath = path;
            _io = new IO(path);
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
            ConsoleCert = _io.Reader.ReadStruct<ConsoleEndorsementCert>();

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
                if (XbfsHeaders[i].Version != 1)
                    continue;
                if (idx >= XbfsHeaders[i].Entries.Length)
                    continue;
                var ent = XbfsHeaders[i].Entries[idx];
                if (ent.Length == 0)
                    continue;
                _io.Stream.Position = ent.LBA*0x1000;
                size = ent.Length*0x1000;
            }
            return size;
        }

        public string GetXbfsInfo()
        {
            var info = new Dictionary<long, string>();
            for (int i = 0; i < XbfsHeaders.Count; i++)
            {
                if (XbfsHeaders[i].Version != 1)
                    continue;
                for (int y = 0; y < XbfsHeaders[i].Entries.Length; y++)
                {
                    var ent = XbfsHeaders[i].Entries[y];
                    if (ent.Length == 0)
                        continue;
                    long start = (long)ent.LBA * 0x1000;
                    long length = (long)ent.Length * 0x1000;
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
                if (XbfsHeaders[i].Version != 1)
                    continue;
                for (int y = 0; y < XbfsHeaders[i].Entries.Length; y++)
                {
                    var ent = XbfsHeaders[i].Entries[y];
                    if (ent.Length == 0)
                        continue;

                    string fileName = (ent.LBA * 0x1000).ToString("X") + "_" + (ent.Length * 0x1000).ToString("X") + "_" + i + "_" + y + "_" + XbfsFilenames[y];

                    long read = 0;
                    long total = (long)ent.Length*0x1000;
                    _io.Stream.Position = (long)ent.LBA * 0x1000;

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
                b.AppendLine("XbfsHeader slot " + i + " (0x" + XbfsOffsets[i].ToString("X") + ")");
                b.Append(XbfsHeaders[i].ToString(formatted));
            }
            return b.ToString();
        }
    }
}
