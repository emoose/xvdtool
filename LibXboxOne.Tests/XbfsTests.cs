using System.IO;
using Xunit;
using LibXboxOne.Nand;

namespace LibXboxOne.Tests
{
    public class XbfsTests
    {
        public static XbfsHeader GetHeader()
        {
            var data = ResourcesProvider.GetBytes("xbfs_header.bin", ResourceType.DataBlobs);
            return Shared.BytesToStruct<XbfsHeader>(data);
        }

        [Fact]
        public void TestXbfsHeaderParsing()
        {
            XbfsHeader header = GetHeader();

            Assert.True(header.IsValid);
            Assert.True(header.IsHashValid);
            Assert.Equal(1, header.FormatVersion);
            Assert.Equal(1, header.SequenceNumber);
            Assert.Equal(9, header.LayoutVersion);
            Assert.Equal((ulong)0, header.Reserved08);
            Assert.Equal((ulong)0, header.Reserved10);
            Assert.Equal((ulong)0, header.Reserved18);
        }

        [Fact]

        public void TestXbfsHeaderRehash()
        {
            XbfsHeader header = GetHeader();
            header.Files[0].Length = 123;

            Assert.False(header.IsHashValid);
            header.Rehash();
            Assert.True(header.IsHashValid);
        }
    }
}