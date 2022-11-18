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

        [Fact]
        public void TestXbfsOutOfBounds()
        {
            XbfsHeader header = GetHeader();

            // Write a file entry past the known filenames array
            header.Files[XbfsFile.XbfsFilenames.Length + 2] = new XbfsEntry(){
                LBA=0, Length=1, Reserved=0
            };

            // Call to ToString will print filenames and should
            // encouter a file that we don't know the filename of
            header.ToString();
        }
    }
}