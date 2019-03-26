using System;
using System.IO;
using DiscUtils.Streams;

namespace LibXboxOne
{
    public class XvdFilesystem
    {
        XvdFile _xvdFile { get; }
        Vhd.VhdDiskGeometry _geometry { get; }
        XvdFilesystemStream _fs { get; }
        uint SectorSize => (uint)_xvdFile.Header.SectorSize;
        ulong FilesystemSize => _xvdFile.Header.DriveSize;
        

        public XvdFilesystem(XvdFile file)
        {
            _xvdFile = file;
            _geometry = Vhd.VhdUtils.CalculateDiskGeometry(FilesystemSize, SectorSize);
            _fs = new XvdFilesystemStream(_xvdFile);
        }

        void ExtractFiles(DiscUtils.DiscDirectoryInfo dirInfo, string extractDir)
        {
            var dirs = dirInfo.GetDirectories();
            foreach(var d in dirs)
            {
                ExtractFiles(d, extractDir);
            }

            var files = dirInfo.GetFiles();
            foreach(var f in files)
            {
                /* Assemble destination path and create directory */
                var destDir = extractDir;
                var parentDirs = f.DirectoryName.Split('\\');
                foreach(var pd in parentDirs)
                {
                    destDir = Path.Combine(destDir, pd);
                }
                Directory.CreateDirectory(destDir);
                
                /* Write out file */
                var destFile = Path.Combine(destDir, f.Name);
                using(var srcFile = f.OpenRead())
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
        }

        public bool ExtractFilesystem(string outputDirectory)
        {
            var geometry = new DiscUtils.Geometry(_geometry.Cylinder,
                                                  _geometry.Heads,
                                                  _geometry.SectorsPerCylinder,
                                                  (int)SectorSize);

            var disk = new DiscUtils.Raw.Disk(_fs,
                                              DiscUtils.Streams.Ownership.None,
                                              geometry);

            using (var fsStream = disk.Partitions[0].Open())
            {
                /* Workaround:
                 * Wrap the NTFS filesystem stream  in a SnapshotStream to override
                 * BiosParamaterBlock's SignatureByte in memory for DiscUtils to accept
                 * the stream.
                 *
                 * See: https://github.com/DiscUtils/DiscUtils/issues/146
                 */
                var snapshotStream = new DiscUtils.Streams.SnapshotStream(fsStream, Ownership.None);

                snapshotStream.Snapshot();
                snapshotStream.Seek(0x26, SeekOrigin.Begin);
                snapshotStream.WriteByte(0x80);
                /* Workaround end */

                var fs = new DiscUtils.Ntfs.NtfsFileSystem(snapshotStream);

                ExtractFiles(fs.Root, outputDirectory);
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

                if (createVhd)
                {
                    var footer = Vhd.VhdFooter.CreateForFixedDisk((ulong)destFs.Length,
                                                                  _xvdFile.Header.VDUID);
                    var footerBytes = Shared.StructToBytes<Vhd.VhdFooter>(footer);
                    destFs.Write(footerBytes, 0, footerBytes.Length);
                }
            }
            return true;
        }
    }
}