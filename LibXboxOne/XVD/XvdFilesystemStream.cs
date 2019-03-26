using System;
using System.IO;

namespace LibXboxOne
{
    public class XvdFilesystemStream : Stream
    {
        readonly XvdFile _xvdFile;

        // Absolute offset in Xvd of DriveData start
        readonly long DriveBaseOffset;
        // Absolute offset to use for calculation BAT target offset
        readonly long DynamicBaseOffset;
        // Length of static data for XvdType.Dynamic
        readonly long StaticDataLength;

        public override bool CanRead => true;
        public override bool CanSeek => true;
        // Disable writing for now
        public override bool CanWrite => false;

        public override long Length => (long)_xvdFile.Header.DriveSize;

        public override long Position { get; set; }

        public XvdFilesystemStream(XvdFile file)
        {
            _xvdFile = file;
            DriveBaseOffset = (long)_xvdFile.DriveDataOffset;
            DynamicBaseOffset = (long)(_xvdFile.UserDataOffset +
                                XvdMath.PageNumberToOffset(_xvdFile.Header.UserDataPageCount));

            StaticDataLength = (long)(XvdFile.BLOCK_SIZE - (DriveBaseOffset - DynamicBaseOffset));
            Position = 0;
        }

        public override void Flush()
        {
        }

        byte[] InternalRead(int count)
        {
            var data = _xvdFile.ReadBytes(DriveBaseOffset + Position, count);
            if (data.Length <= 0)
                throw new IOException("InternalRead got nothing...");

            Position += data.Length;
            return data;
        }

        byte[] InternalReadAbsolute(long offset, int count)
        {
            var data = _xvdFile.ReadBytes(offset, count);
            if (data.Length <= 0)
                throw new IOException("InternalReadAbsolute got nothing...");

            Position += data.Length;
            return data;
        }

        byte[] ReadDynamic(int count)
        {
            int positionInBuffer = 0;
            int bytesRemaining = count;
            byte[] destBuffer = new byte[count];

            while (positionInBuffer < count)
            {
                byte[] data = new byte[0];
                if (Position < StaticDataLength)
                {
                    // Read a chunk from non-dynamic area, next iteration will read dynamic data
                    int maxReadLength = (int)(StaticDataLength - Position);
                    int length = bytesRemaining > maxReadLength ? maxReadLength : bytesRemaining;
                    data = InternalRead(length);
                }
                else
                {
                    // Lookup block allocation table for real data offset
                    var targetVirtualOffset = (ulong)(Position - StaticDataLength);
                    ulong blockNumber = XvdMath.OffsetToBlockNumber(targetVirtualOffset);
                    long inBlockOffset = (long)XvdMath.InBlockOffset(targetVirtualOffset);
                    int maxReadLength = (int)(XvdFile.BLOCK_SIZE - inBlockOffset);
                    int length = bytesRemaining > maxReadLength ? maxReadLength : bytesRemaining;

                    var targetPage = _xvdFile.ReadBat(blockNumber);
                    if (targetPage == XvdFile.INVALID_SECTOR)
                    {
                        data = new byte[length];
                        // Advance stream position cause we are not actually reading data
                        Position += length;
                    }
                    else
                    {
                        long targetPhysicalOffset = DynamicBaseOffset +
                                                    (long)XvdMath.PageNumberToOffset(targetPage) +
                                                    inBlockOffset;

                        data = InternalReadAbsolute(targetPhysicalOffset, length);
                    }
                }

                Array.Copy(data, 0, destBuffer, positionInBuffer, data.Length);
                positionInBuffer += data.Length;
                bytesRemaining -= data.Length;
            }

            return destBuffer;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            byte[] dataRead;

            if (Position + count > Length)
                throw new IOException("Desired range out-of-bounds for stream");
            else if (offset + count > buffer.Length)
                throw new IOException("Target buffer to small to hold read data");

            else if (_xvdFile.Header.Type == XvdType.Fixed)
                dataRead = InternalRead(count);
            else if (_xvdFile.Header.Type == XvdType.Dynamic)
                dataRead = ReadDynamic(count);

            else
                throw new IOException($"Unsupported XvdType: {_xvdFile.Header.Type}");

            Array.Copy(dataRead, 0, buffer, offset, count);
            return dataRead.Length;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = Length + offset;
                    break;
            }
            return Position;
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}