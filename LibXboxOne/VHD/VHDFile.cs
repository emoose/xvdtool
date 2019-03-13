using System;
using System.IO;
using System.Runtime.InteropServices;

namespace LibXboxOne.Vhd
{
    // note: this code doesn't make a working dynamic vhd
    // or maybe it does and i just had compression enabled on the file and that's why it wouldn't mount

    class VhdFile : IDisposable
    {
        private static readonly DateTime VhdEpoch = new DateTime(2000, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        public VhdFooter Header;
        public VhdDynamicDiskHeader DynamicDiskHeader;
        public uint[] BlockAllocationTable;

        private readonly IO _io;

        public static uint GetTimestamp(DateTime dateTime)
        {
            return (uint)dateTime.Subtract(VhdEpoch).TotalSeconds;
        }

        public VhdFile(string fileName)
        {
            _io = new IO(fileName, FileMode.Create);
        }

        public void Create(VhdDiskType diskType, ulong driveSize)
        {
            Header = new VhdFooter();
            // Header.InitDefaults();
            Header.OriginalSize = driveSize.EndianSwap();
            Header.CurrentSize = driveSize.EndianSwap();

            DynamicDiskHeader = new VhdDynamicDiskHeader();
            DynamicDiskHeader.InitDefaults();

            var batEntryCount = (uint)(driveSize/DynamicDiskHeader.BlockSize.EndianSwap());
            BlockAllocationTable = new uint[(int) batEntryCount];
            for (uint i = 0; i < batEntryCount; i++)
                BlockAllocationTable[i] = 0xFFFFFFFF;

            DynamicDiskHeader.MaxTableEntries = batEntryCount.EndianSwap();

            Header.FixChecksum();
            DynamicDiskHeader.FixChecksum();
        }

        public void Save()
        {
            _io.Stream.Position = 0;
            _io.Writer.WriteStruct(Header);
            _io.Stream.Position = 0x200;
            _io.Writer.WriteStruct(DynamicDiskHeader);

            _io.Stream.Position = 0x600;
            foreach (uint batEntry in BlockAllocationTable)
                _io.Writer.Write(batEntry);

            const uint unknownStuff = 0xffffffff;
            for (int i = 0; i < 0x20; i++)
                _io.Writer.Write(unknownStuff);

            _io.Stream.Position += 0x180;

            // reserve space for the drive
            _io.Stream.Position += (long)Header.OriginalSize.EndianSwap();

            // write vhd footer
            _io.Writer.WriteStruct(Header);
        }

        public void Write(long offset, byte[] data)
        {
            long position = 0x600 + (BlockAllocationTable.Length*4) + 0x200 + offset; // seek to offset
            _io.Stream.Position = position;
            _io.Writer.Write(data);
        }

        public void Dispose()
        {
            _io.Dispose();
        }
    }
}
