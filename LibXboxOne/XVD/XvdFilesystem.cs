using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DiscUtils;
using DiscUtils.Ntfs;
using DiscUtils.Partitions;
using DiscUtils.Streams;

namespace LibXboxOne
{
    public class XvdFilesystem
    {
        XvdFile _xvdFile { get; }
        XvdDiskGeometry _geometry { get; }
        XvdFilesystemStream _fs { get; }

        XvdType XvdFsType => _xvdFile.Header.Type;
        uint SectorSize => (uint)_xvdFile.Header.SectorSize;
        ulong FilesystemSize => _xvdFile.Header.DriveSize;
        

        public XvdFilesystem(XvdFile file)
        {
            _xvdFile = file;
            _geometry = XvdMath.CalculateDiskGeometry(FilesystemSize, SectorSize);
            _fs = new XvdFilesystemStream(_xvdFile);
        }

        DiscUtils.Raw.Disk OpenDisk()
        {
            var geometry = new DiscUtils.Geometry(_geometry.Cylinder,
                                                  _geometry.Heads,
                                                  _geometry.SectorsPerCylinder,
                                                  (int)SectorSize);

            return new DiscUtils.Raw.Disk(_fs,
                                          DiscUtils.Streams.Ownership.None,
                                          geometry);
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

        IEnumerable<DiscUtils.DiscFileInfo> IterateFilesystem()
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

            var disk = OpenDisk();

            if (disk.Partitions == null || disk.Partitions.Count <= 0)
                throw new InvalidDataException("No filesystem partitions detected");
            else if (disk.Partitions.Count > 1)
                throw new NotSupportedException("More than one filesystem partition detected");

            using (var fsStream = disk.Partitions[0].Open())
            {
                var partitionStream = GetPatchedNtfsPartitionStream(fsStream);
                var fs = new DiscUtils.Ntfs.NtfsFileSystem(partitionStream);

                foreach(var file in IterateSubdir(fs.Root))
                {
                    yield return file;
                }

                fs.Dispose();
                partitionStream.Dispose();
            }

            disk.Dispose();
        }

        public bool ExtractFilesystem(string outputDirectory)
        {
            foreach (var file in IterateFilesystem())
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

                foreach (var file in IterateFilesystem())
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
                                                                     (long)FilesystemSize);
                }
                else if (XvdFsType == XvdType.Dynamic)
                {
                    Console.WriteLine("Initializing dynamic VHD...");
                    vhdDisk = DiscUtils.Vhd.Disk.InitializeDynamic(destVhdFs,
                                                                       Ownership.None,
                                                                       (long)FilesystemSize);
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
            b.AppendLineSpace(fmt + "General Disk info:");
            b.AppendLineSpace(fmt + fmt + $"Capacity: {disk.Capacity} (0x{disk.Capacity:X})");
            b.AppendLineSpace(fmt + fmt + $"Partition table present: {disk.IsPartitioned}");
            b.AppendLine();

            if (disk.Partitions == null || disk.Partitions.Count == 0)
            {
                b.AppendLineSpace(fmt + "No partition table found on disk!");
                disk.Dispose();
                return b.ToString();
            }

            var pTable = disk.Partitions;
            b.AppendLineSpace(fmt + "Partition table info:");
            b.AppendLineSpace(fmt + fmt + $"Disk GUID: {pTable.DiskGuid}");
            b.AppendLineSpace(fmt + fmt + $"Partition count: {pTable.Count}");
            b.AppendLine();

            b.AppendLineSpace(fmt + fmt + "Partitions:");

            for (int i = 0; i < pTable.Count; i++)
            {
                var part = pTable[i];
                b.AppendLineSpace(fmt + fmt + $"- Partition {i}:");

                b.AppendLineSpace(fmt + fmt + fmt + $"  BIOS-type: {part.TypeAsString} ({part.BiosType} / 0x{part.BiosType:X})");
                b.AppendLineSpace(fmt + fmt + fmt + $"  GUID-type: {part.GuidType}");
                b.AppendLineSpace(fmt + fmt + fmt + $"  First sector: {part.FirstSector} (0x{part.FirstSector:X})");
                b.AppendLineSpace(fmt + fmt + fmt + $"  Last sector: {part.LastSector} (0x{part.LastSector:X})");
                b.AppendLineSpace(fmt + fmt + fmt + $"  Sector count: {part.SectorCount} (0x{part.SectorCount:X})");
                b.AppendLine();
            }
            disk.Dispose();

            b.AppendLineSpace(fmt + "Filesystem content:");
            try
            {
                foreach (var file in IterateFilesystem())
                {
                    b.AppendLineSpace(fmt + fmt + FileInfoToString(file));
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