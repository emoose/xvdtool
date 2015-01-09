using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace LibXboxOne
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SfbxEntry
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
            b.AppendLine("SfbxEntry:");

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

    // might be the new ConsoleSecurityCertificate?
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct SpKeyVault
    {
        /* 0x0 */ public ushort Magic; // ?? these next 4 fields look like they follow same format as 360 bootloaders? probably not
        /* 0x2 */ public ushort Build; // ??
        /* 0x4 */ public ushort Qfe; // ??
        /* 0x6 */ public ushort Flags; // ??

        /* 0x8 */ public uint UniqueData1;
        /* 0xC */ public uint Unknown1; // 01 0A 22 10
        /* 0x10 */ public byte Unknown2; // 0x1

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0xF)]
        /* 0x11 */ public byte[] UniqueData2;
        /* 0x20 */ public uint Unknown3; // 0x1
        /* 0x24 */ public uint Unknown4;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x200)]
        /* 0x28 */ public byte[] PossibleSignature;
        /* 0x228 */ public ulong UniqueData3;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x14)]
        /* 0x230 */ public char[] ConsoleSerialNumber;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x20)]
        /* 0x244 */ public byte[] UniqueData4;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x1C)]
        /* 0x264 */ public char[] ConsolePartNumber;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x180)]
        /* 0x280 */ public byte[] UniqueData5;
    }

    // SFBX header, can be at 0x10000, 0x810000 or 0x820000
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct SfbxHeader
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
        /* 0x20 */ public SfbxEntry[] Entries;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        /* 0x3C0 */ public byte[] Unknown6;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        /* 0x3D0 */ public byte[] Unknown7;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x20)]
        /* 0x3E0 */ public byte[] SfbxHash; // SHA256 hash of 0x0 - 0x3E0

        public override string ToString()
        {
            return ToString(false);
        }

        public string ToString(bool formatted)
        {
            var b = new StringBuilder();
            b.AppendLine("SfbxHeader:");

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
            b.AppendLineSpace(fmt + "SFBX header hash: " + Environment.NewLine + fmt + SfbxHash.ToHexString());

            for(int i = 0; i < Entries.Length; i++)
            {
                SfbxEntry entry = Entries[i];
                //if (entry.Length == 0)
                //    break;
                b.AppendLine("Entry " + i);
                b.Append(entry.ToString(formatted));
            }

            return b.ToString();
        }
    }

    public class XbfsFile
    {
        static int[] sfbxOffsets = { 0x10000, 0x810000, 0x820000 };
        public static string[] SfbxFilenames =
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

        public List<SfbxHeader> SfbxHeaders;
        public SpKeyVault KeyVault;

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
            // read each SFBX header
            SfbxHeaders = new List<SfbxHeader>();
            foreach (int offset in sfbxOffsets)
            {
                _io.Stream.Position = offset;
                var header = _io.Reader.ReadStruct<SfbxHeader>();
                SfbxHeaders.Add(header);
            }

            long spDataSize = SeekToFile("sp_s.cfg");
            if (spDataSize <= 0)
                return true;

            //SP_S.cfg: (secure processor secured config? there's also a blank sp_d.cfg which is probably secure processor decrypted config)
            // 0 - 0x200 - signature?
            // 0x200 - 0x5200 - loaded and decrypted into PSP memory?
            // 0x5200 - 0x5400 - blank
            // 0x5400 - 0x5800 - console data, not encrypted, format unknown

            _io.Stream.Position += 0x5400; // seek to start of unencrypted data in sp_s (keyvault?)
            KeyVault = _io.Reader.ReadStruct<SpKeyVault>();

            return true;
        }

        // returns the size of the file if found
        public long SeekToFile(string fileName)
        {
            int idx = Array.IndexOf(SfbxFilenames, fileName);
            if (idx < 0)
                return 0;
            long size = 0;
            for (int i = 0; i < SfbxHeaders.Count; i++)
            {
                if (SfbxHeaders[i].Version != 1)
                    continue;
                if (idx >= SfbxHeaders[i].Entries.Length)
                    continue;
                SfbxEntry ent = SfbxHeaders[i].Entries[idx];
                if (ent.Length == 0)
                    continue;
                _io.Stream.Position = ent.LBA*0x1000;
                size = ent.Length*0x1000;
            }
            return size;
        }

        public string GetSfbxInfo()
        {
            var info = new Dictionary<long, string>();
            for (int i = 0; i < SfbxHeaders.Count; i++)
            {
                if (SfbxHeaders[i].Version != 1)
                    continue;
                for (int y = 0; y < SfbxHeaders[i].Entries.Length; y++)
                {
                    SfbxEntry ent = SfbxHeaders[i].Entries[y];
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

        public void ExtractSfbxData(string folderPath)
        {
            var doneAddrs = new List<long>();
            for (int i = 0; i < SfbxHeaders.Count; i++)
            {
                if (SfbxHeaders[i].Version != 1)
                    continue;
                for(int y = 0; y < SfbxHeaders[i].Entries.Length; y++)
                {
                    SfbxEntry ent = SfbxHeaders[i].Entries[y];
                    if (ent.Length == 0)
                        continue;

                    string fileName = (ent.LBA * 0x1000).ToString("X") + "_" + (ent.Length * 0x1000).ToString("X") + "_" + i + "_" + y + "_" + SfbxFilenames[y];

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
            for (int i = 0; i < SfbxHeaders.Count; i++)
            {
                b.AppendLine("SfbxHeader slot " + i + " (0x" + sfbxOffsets[i].ToString("X") + ")");
                b.Append(SfbxHeaders[i].ToString(formatted));
            }
            return b.ToString();
        }
    }
}
