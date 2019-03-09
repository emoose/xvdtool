using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Xunit;

namespace LibXboxOne.Tests
{
    public class XvdHashBlockTests
    {
        [Theory]
        [InlineData(0x4000, 1, 0x1)]
        [InlineData(0x5427, 1, 0x1)]
        [InlineData(0x20001, 1, 0x5)]
        [InlineData(0x20001, 2, 0x1)]
        [InlineData(0xA43E3, 1, 0x18)]
        [InlineData(0xA43E3, 2, 0x1)]
        [InlineData(0xC2BFF, 1, 0x1C)]
        [InlineData(0xC2BFF, 2, 0x1)]
        public void TestNumHashBlockCalculation(ulong size, ulong index, ulong expected)
        {
            ulong actual = XvdFile.CalculateNumHashBlocksInLevel(size, index, false);

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData((XvdType)0, 0x2, 0x4000, 0xE02, 0x0, 0x10, 0x16)]
        [InlineData((XvdType)1, 0x3, 0xC001, 0x150A, 0x0, 0x74, 0x22)]
        [InlineData((XvdType)1, 0x3, 0x20001, 0x3772, 0x0, 0x54, 0x59)]
        [InlineData((XvdType)1, 0x3, 0x5F653, 0x5F604, 0x0, 0x0, 0x909)]
        [InlineData((XvdType)1, 0x3, 0x5F653, 0x5F604, 0x1, 0x58, 0xE)]
        [InlineData((XvdType)1, 0x3, 0x5F653, 0x5BB94, 0x1, 0x0, 0xE)]
        [InlineData((XvdType)1, 0x3, 0x5F653, 0x5BB94, 0x2, 0xD, 0x0)]
        [InlineData((XvdType)1, 0x3, 0x5F653, 0xAB2, 0x0, 0x12, 0x1F)]
        [InlineData((XvdType)1, 0x3, 0x5F653, 0xF20B, 0x0, 0x53, 0x17B)]
        [InlineData((XvdType)1, 0x3, 0x5F653, 0xF60A, 0x0, 0x56, 0x181)]
        public void TestCalculateHashBlockNumForBlockNum(XvdType xvdType, ulong hashTreeLevels, ulong xvdDataBlockCount,
                                                         ulong blockNum, uint index,
                                                         ulong expectedEntryNum, ulong expectedResult)
        {
            ulong result = XvdFile.CalculateHashBlockNumForBlockNum(xvdType, hashTreeLevels, xvdDataBlockCount,
                                                                    blockNum, index, out ulong entryNumInBlock);
            
            Assert.Equal(expectedEntryNum, entryNumInBlock);
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData(0x6401, 0x97)]
        [InlineData(0x5427, 0x7F)]
        [InlineData(0x20001, 0x304)]
        [InlineData(0x5F653, 0x8FB)]
        public void TestCalculateHashTreeBlockCount(ulong xvdDataBlockCount, ulong expected)
        {
            ulong result = XvdFile.PagesToBlocks(xvdDataBlockCount);

            Assert.Equal(expected, result);
        }
    }
}