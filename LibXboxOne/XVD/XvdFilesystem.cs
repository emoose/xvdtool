using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;
using DiscUtils;
using DiscUtils.Ntfs;
using DiscUtils.Partitions;
using DiscUtils.Streams;

namespace LibXboxOne
{
    public class XvdFilesystem
    {
        XvdFile _xvdFile { get; }
        XvdFilesystemStream _fs { get; }

        XvdType XvdFsType => _xvdFile.Header.Type;
        uint SectorSize => (uint)_xvdFile.Header.SectorSize;
        ulong FilesystemSize => _xvdFile.Header.DriveSize;
        Geometry DiskGeometry =>
            Geometry.FromCapacity((long)FilesystemSize, (int)SectorSize);
        

        public XvdFilesystem(XvdFile file)
        {
            _xvdFile = file;
            _fs = new XvdFilesystemStream(_xvdFile);
        }

        bool GetGptPartitionTable(out GuidPartitionTable partitionTable)
        {
            try
            {
                partitionTable = new GuidPartitionTable(_fs, DiskGeometry);
            }
            catch (Exception)
            {
                partitionTable = null;
            }

            // CHECKME: Count refers to the number of PARTITION TABLES, not partitions?
            if (partitionTable == null || partitionTable.Count <= 0)
            {
                Debug.WriteLine("No GPT partition table detected");
                return false;
            }

            return true;
        }

        bool GetMbrPartitionTable(out BiosPartitionTable partitionTable)
        {
            try
            {
                partitionTable = new BiosPartitionTable(_fs, DiskGeometry);
            }
            catch (Exception)
            {
                partitionTable = null;
            }

            // CHECKME: Count refers to the number of PARTITION TABLES, not partitions?
            if (partitionTable == null || partitionTable.Count <= 0)
            {
                Debug.WriteLine("No MBR partition table detected");
                return false;
            }

            return true;
        }

        PartitionTable OpenDisk()
        {
            // Gathering partitions manually so Geometry can be provided
            // explicitly. This ensures that 4k sectors are properly set.
            PartitionTable partitionTable;
            if (GetGptPartitionTable(out GuidPartitionTable gptTable))
            {
                partitionTable = (PartitionTable)gptTable;
            }
            else if (GetMbrPartitionTable(out BiosPartitionTable mbrTable))
            {
                partitionTable = (PartitionTable)mbrTable;
            }
            else
            {
                Debug.WriteLine("No valid partition table detected");
                return null;
            }

            if (partitionTable.Partitions == null || partitionTable.Partitions.Count <= 0)
            {
                Debug.WriteLine("Partition table holds no partitions");
                return null;
            }

            return partitionTable;
        }

        Stream GetPatchedNtfsPartitionStream(Stream inStream)
        {
            /* Workaround:
            * Wrap the NTFS filesystem stream  in a SnapshotStream to override
            * BiosParamaterBlock's SignatureByte in memory for DiscUtils to accept
            * the stream.
            *
            * See: https://github.com/DiscUtils/DiscUtils/issues/146
            */
            var snapshotStream = new DiscUtils.Streams.SnapshotStream(inStream, Ownership.None);

            snapshotStream.Snapshot();
            snapshotStream.Seek(0x26, SeekOrigin.Begin);
            snapshotStream.WriteByte(0x80);
            /* Workaround end */

            return snapshotStream;
        }

        IEnumerable<DiscUtils.DiscFileInfo> IterateFilesystem(int partitionNumber)
        {
            IEnumerable<DiscUtils.DiscFileInfo> IterateSubdir(DiscUtils.DiscDirectoryInfo subdir)
            {
                foreach(var dir in subdir.GetDirectories())
                {
                    foreach(var subfile in IterateSubdir(dir))
                    {
                        yield return subfile;
                    }
                }

                var files = subdir.GetFiles();
                foreach(var f in files)
                {
                    yield return f;
                }
            }

            PartitionTable disk = OpenDisk();
            if (disk == null)
            {
                Debug.WriteLine("IterateFilesystem: Failed to open disk");
                yield break;
            }
            else if (disk.Partitions.Count - 1 < partitionNumber)
            {
                Debug.WriteLine($"IterateFilesystem: Partition {partitionNumber} does not exist");
                yield break;
            }

            using (var fsStream = disk.Partitions[partitionNumber].Open())
            {
                var partitionStream = GetPatchedNtfsPartitionStream(fsStream);
                NtfsFileSystem fs = new DiscUtils.Ntfs.NtfsFileSystem(partitionStream);

                foreach(var file in IterateSubdir(fs.Root))
                {
                    yield return file;
                }

                fs.Dispose();
                partitionStream.Dispose();
            }
        }

        public bool ExtractFilesystem(string outputDirectory, int partitionNumber=0)
        {
            foreach (var file in IterateFilesystem(partitionNumber))
            {
                Console.WriteLine(file.DirectoryName + file.Name);
                /* Assemble destination path and create directory */
                var destDir = outputDirectory;
                var parentDirs = file.DirectoryName.Split('\\');
                foreach(var pd in parentDirs)
                {
                    destDir = Path.Combine(destDir, pd);
                }
                Directory.CreateDirectory(destDir);
                
                /* Write out file */
                var destFile = Path.Combine(destDir, file.Name);
                using(var srcFile = file.OpenRead())
                {
                    using(var dstFs = File.Create(destFile))
                    {
                        var data = new byte[srcFile.Length];

                        if (srcFile.Read(data, 0, data.Length) != data.Length)
                            throw new InvalidOperationException(
                                $"Failed to read {srcFile} from raw image");

                        dstFs.Write(data, 0, data.Length);
                    }
                }
            }

            return true;
        }

        public bool ExtractFilesystemImage(string targetFile, bool createVhd)
        {
            using (var destFs = File.Open(targetFile, FileMode.Create))
            {
                byte[] buffer = new byte[XvdFile.PAGE_SIZE];

                for (long offset = 0; offset < _fs.Length; offset += XvdFile.PAGE_SIZE)
                {
                    _fs.Read(buffer, 0, buffer.Length);
                    destFs.Write(buffer, 0, buffer.Length);
                }
            }
            return true;
        }

        void InitializeVhdManually(DiscUtils.Vhd.Disk vhdDisk)
        {
            BiosPartitionTable.Initialize(vhdDisk, WellKnownPartitionType.WindowsNtfs);
            // GuidPartitionTable.Initialize(vhdDisk,  WellKnownPartitionType.WindowsNtfs);

            var volMgr = new VolumeManager(vhdDisk);
            var logicalVolume = volMgr.GetLogicalVolumes()[0];

            var label = $"XVDTool conversion";

            using (var destNtfs = NtfsFileSystem.Format(logicalVolume, label, new NtfsFormatOptions()))
            {
                destNtfs.NtfsOptions.ShortNameCreation = ShortFileNameOption.Disabled;

                // NOTE: For VHD creation we just assume a single partition
                foreach (var file in IterateFilesystem(partitionNumber: 0))
                {
                    var fh = file.OpenRead();

                    if (!destNtfs.Exists(file.DirectoryName))
                    {
                        destNtfs.CreateDirectory(file.DirectoryName);
                    }

                    using (Stream dest = destNtfs.OpenFile(file.FullName, FileMode.Create,
                        FileAccess.ReadWrite))
                    {
                        fh.CopyTo(dest);
                        dest.Flush();
                    }

                    fh.Close();
                }
            }
        }

        void InitializeVhdViaPump(DiscUtils.Vhd.Disk vhdDisk)
        {
            if (SectorSize != XvdFile.LEGACY_SECTOR_SIZE)
            {
                throw new InvalidOperationException(
                    "Initializing VHD via pump is only supported for 512 byte sectors");
            }

            var pump = new StreamPump(_fs, vhdDisk.Content, (int)XvdFile.LEGACY_SECTOR_SIZE);
            pump.Run();
        }

        // Source: https://github.com/DiscUtils/DiscUtils/issues/137
        public bool ConvertToVhd(string outputFile)
        {
            using (FileStream destVhdFs = File.Open(outputFile, FileMode.Create, FileAccess.ReadWrite))
            {
                DiscUtils.Vhd.Disk vhdDisk;
                if (XvdFsType == XvdType.Fixed)
                {
                    Console.WriteLine("Initializing fixed VHD...");
                    vhdDisk = DiscUtils.Vhd.Disk.InitializeFixed(destVhdFs,
                                                                     Ownership.None,
                                                                     (long)FilesystemSize
                                                                     + (long)(FilesystemSize / 10));
                }
                else if (XvdFsType == XvdType.Dynamic)
                {
                    Console.WriteLine("Initializing dynamic VHD...");
                    vhdDisk = DiscUtils.Vhd.Disk.InitializeDynamic(destVhdFs,
                                                                       Ownership.None,
                                                                       (long)FilesystemSize
                                                                       + (long)(FilesystemSize / 10));
                }
                else
                    throw new InvalidOperationException();

                if (SectorSize == XvdFile.LEGACY_SECTOR_SIZE)
                {
                    Console.WriteLine("Pumping data as-is to vhd (legacy sector size)");
                    InitializeVhdViaPump(vhdDisk);
                }
                else
                {
                    Console.WriteLine("Creating vhd manually (4k sectors)");
                    InitializeVhdManually(vhdDisk);
                }
            }
            return true;
        }

        public string FileInfoToString(DiscFileInfo info)
        {
            return $"{info.FullName} ({info.Length} bytes)";
        }

        public override string ToString()
        {
            return ToString(true);
        }

        public string ToString(bool formatted)
        {
            var b = new StringBuilder();
            b.AppendLine("XvdFilesystem:");

            string fmt = formatted ? "    " : "";

            if (_xvdFile.IsEncrypted)
            {
                b.AppendLineSpace(fmt + "Cannot get XvdFilesystem info from encrypted package");
                return b.ToString();
            }

            var disk = OpenDisk();
            if (disk == null)
            {
                b.AppendLineSpace(fmt + "No partition table found on disk!");
                return b.ToString();
            }

            b.AppendLineSpace(fmt + "Partitions:");
            var partitions = disk.Partitions;
            for (int i = 0; i < partitions.Count; i++)
            {
                var part = partitions[i];
                b.AppendLineSpace(fmt + fmt + $"- Partition {i}:");

                b.AppendLineSpace(fmt + fmt + fmt + $"  BIOS-type: {part.TypeAsString} ({part.BiosType} / 0x{part.BiosType:X})");
                b.AppendLineSpace(fmt + fmt + fmt + $"  GUID-type: {part.GuidType}");
                b.AppendLineSpace(fmt + fmt + fmt + $"  First sector: {part.FirstSector} (0x{part.FirstSector:X})");
                b.AppendLineSpace(fmt + fmt + fmt + $"  Last sector: {part.LastSector} (0x{part.LastSector:X})");
                b.AppendLineSpace(fmt + fmt + fmt + $"  Sector count: {part.SectorCount} (0x{part.SectorCount:X})");
                b.AppendLine();
            }

            b.AppendLineSpace(fmt + "Filesystem content:");
            try
            {
                for (int partitionNumber = 0; partitionNumber < partitions.Count; partitionNumber++)
                {
                    b.AppendLineSpace(fmt + fmt + $":: Partition {partitionNumber}:");
                    foreach (var file in IterateFilesystem(partitionNumber))
                    {
                        b.AppendLineSpace(fmt + fmt + FileInfoToString(file));
                    }
                }
            }
            catch (Exception e)
            {
                b.AppendLineSpace(fmt + fmt + $"Failed to list filesystem content. Error: {e}");
            }

            return b.ToString();
        }
    }
}