using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

namespace LibXboxOne.Nand
{
    public enum XbfsFlavor {
        XboxOne,
        XboxSeries,
        Invalid,
    }

    public enum NandSize: ulong
    {
        EMMC_LOGICAL = 0x13B00_0000,
        EMMC_PHYSICAL = 0x13C00_0000,
        NVME_SERIES = 0x4000_0000,
    }

    public class XbfsFile
    {
        public static readonly int SeriesOffsetDiff = 0x6000;
        public static readonly int BlockSize = 0x1000;
        public static readonly int[] XbfsOffsetsXboxOne = { 0x1_0000, 0x81_0000, 0x82_0000 };
        public static readonly int[] XbfsOffsetsXboxSeries = { 0x1800_8000 };
        public static string[] XbfsFilenames =
        {
            "1smcbl_a.bin", // 0
            "header.bin", // 1
            "devkit.ini", // 2
            "mtedata.cfg", // 3
            "certkeys.bin", // 4
            "smcerr.log", // 5
            "system.xvd", // 6
            "$sospf.xvd", // 7, formerly $sosrst.xvd
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
            "bootanim.dat", // 20, this entry and ones below it are only in retail 97xx and above?
            "obsolete.001", // 21, formerly sostmpl.xvd
            "update.cfg", // 22
            "obsolete.002", // 23, formerly sosinit.xvd
            "hwinit.cfg", // 24
            "qaslt.xvd", // 25
            "sp_s.bak", // 26, keyvault backup? has serial/partnum/osig
            "update2.cfg", // 27
            "obsolete.003", // 28
            "dump.lng", // 29
            "os_d_dev.cfg", // 30
            "os_glob.cfg", // 31
            "sp_s.alt", // 32
            "sysauxf.xvd", // 33
        };

        private readonly IO _io;

        public XbfsFlavor Flavor = XbfsFlavor.Invalid;
        public int[] HeaderOffsets;
        public List<XbfsHeader> XbfsHeaders;

        public readonly string FilePath;
        public long FileSize => _io.Stream.Length;

        public XbfsFile(string path)
        {
            FilePath = path;
            _io = new IO(path);
        }

        public static XbfsFlavor FlavorFromSize(long size) {
            return size switch
            {
                (long)NandSize.EMMC_LOGICAL or (long)NandSize.EMMC_PHYSICAL => XbfsFlavor.XboxOne,
                (long)NandSize.NVME_SERIES => XbfsFlavor.XboxSeries,
                _ => XbfsFlavor.Invalid,
            };
        }

        /// <summary>
        /// Get Filename for XBFS filename from index
        /// </summary>
        /// <param name="index">Index of file</param>
        /// <returns></returns>
        public static string GetFilenameForIndex(int index)
        {
            if (index >= XbfsFilenames.Length) {
                return null;
            }

            return XbfsFilenames[index];
        }

        /// <summary>
        /// Get index for XBFS filename from filename
        /// </summary>
        /// <param name="name">Filename</param>
        /// <returns>Returns >= 0 if file is found, -1 otherwise</returns>
        public static int GetFileindexForName(string name)
        {
            return Array.IndexOf(XbfsFilenames, name);
        }

        public Certificates.PspConsoleCert? ReadPspConsoleCertificate()
        {
            if (XbfsHeaders == null || XbfsHeaders.Count == 0)
                return null;

            long spDataSize = SeekToFile("sp_s.cfg");
            if (spDataSize <= 0)
                return null;

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
            return _io.Reader.ReadStruct<Certificates.PspConsoleCert>();
        }

        public Certificates.BootCapabilityCert? ReadBootcapCertificate()
        {
            if (XbfsHeaders == null || XbfsHeaders.Count == 0)
                return null;

            long spDataSize = SeekToFile("certkeys.bin");
            if (spDataSize <= 0)
                return null;

            return _io.Reader.ReadStruct<Certificates.BootCapabilityCert>();
        }

        public bool Load()
        {
            Flavor = FlavorFromSize(FileSize);

            HeaderOffsets = Flavor switch {
                XbfsFlavor.XboxOne => XbfsOffsetsXboxOne,
                XbfsFlavor.XboxSeries => XbfsOffsetsXboxSeries,
                _ => throw new InvalidDataException($"Invalid xbfs filesize: {FileSize:X}"),
            };

            // read each XBFS header
            XbfsHeaders = new List<XbfsHeader>();
            foreach (int offset in HeaderOffsets)
            {
                _io.Stream.Position = offset;
                var header = _io.Reader.ReadStruct<XbfsHeader>();
                XbfsHeaders.Add(header);
            }
            return true;
        }

        // returns the size of the file if found
        public long SeekToFile(string fileName)
        {
            int idx = GetFileindexForName(fileName);
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
                if (ent.BlockCount == 0)
                    continue;
                _io.Stream.Position = ent.Offset(Flavor);
                size = ent.Length;
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
                    long start = ent.Offset(Flavor);
                    long length = ent.Length;
                    long end = start + length;
                    string addInfo = $"{end:X} {i}_{y}";
                    if (info.ContainsKey(start))
                        info[start] += $" {addInfo}";
                    else
                        info.Add(start, addInfo);
                }
            }
            string infoStr = String.Empty;
            var keys = info.Keys.ToList();
            keys.Sort();
            foreach (var key in keys)
                infoStr += $"{key:X} - {info[key]}{Environment.NewLine}";

            return infoStr;
        }

        public void ExtractXbfsData(string folderPath)
        {
            if (!Directory.Exists(folderPath)) {
                Console.WriteLine($"Creating output folder for xbfs extraction '{folderPath}'");
                Directory.CreateDirectory(folderPath);
            }

            var doneAddrs = new List<long>();
            for (int i = 0; i < XbfsHeaders.Count; i++)
            {
                if (!XbfsHeaders[i].IsValid)
                    continue;
                for (int y = 0; y < XbfsHeaders[i].Files.Length; y++)
                {
                    var ent = XbfsHeaders[i].Files[y];
                    if (ent.BlockCount == 0)
                        continue;

                    var xbfsFilename = GetFilenameForIndex(y);
                    string fileName = $"{ent.Offset(Flavor):X}_{ent.Length:X}_{i}_{y}_{xbfsFilename ?? "unknown"}";

                    long read = 0;
                    long total = ent.Length;
                    _io.Stream.Position = ent.Offset(Flavor);

                    bool writeFile = true;
                    if (doneAddrs.Contains(_io.Stream.Position))
                    {
                        writeFile = false;
                        fileName = $"DUPE_{fileName}";
                    }
                    doneAddrs.Add(_io.Stream.Position);


                    if (_io.Stream.Position + total > _io.Stream.Length)
                        continue;

                    using (var fileIo = new IO(Path.Combine(folderPath, fileName), FileMode.Create))
                    {
                        if (!writeFile) // create empty file for DUPE_* files
                            continue;

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
            b.AppendLine($"Flavor: {Flavor}");
            b.AppendLine($"Size: 0x{FileSize:X} ({FileSize / 1024 / 1024} MB)");
            b.AppendLine();
            for (int i = 0; i < XbfsHeaders.Count; i++)
            {
                if(!XbfsHeaders[i].IsValid)
                    continue;

                b.AppendLine($"XbfsHeader slot {i}: (0x{HeaderOffsets[i]:X})");
                b.Append(XbfsHeaders[i].ToString(formatted));
            }
            return b.ToString();
        }
    }
}