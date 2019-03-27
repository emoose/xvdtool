using System;

namespace LibXboxOne
{
    public static class XvdMath
    {
        public static bool PagesAligned(ulong page)
        {
            return (page & (XvdFile.PAGE_SIZE - 1)) == 0;
        }

        public static ulong PageAlign(ulong offset)
        {
            return offset & 0xFFFFFFFFFFFFF000;
        }

        public static ulong InBlockOffset(ulong offset)
        {
            return offset - ((offset / XvdFile.BLOCK_SIZE) * XvdFile.BLOCK_SIZE);
        }

        public static ulong InPageOffset(ulong offset)
        {
            return offset & (XvdFile.PAGE_SIZE - 1);
        }

        public static ulong BlockNumberToOffset(ulong blockNumber)
        {
            return blockNumber * XvdFile.BLOCK_SIZE;
        }

        public static ulong PageNumberToOffset(ulong pageNumber)
        {
            return pageNumber * XvdFile.PAGE_SIZE;
        }

        public static ulong BytesToBlocks(ulong bytes)
        {
            return (bytes + XvdFile.BLOCK_SIZE - 1) / XvdFile.BLOCK_SIZE;
        }

        public static ulong PagesToBlocks(ulong pages)
        {
            return (pages + XvdFile.PAGES_PER_BLOCK - 1) / XvdFile.PAGES_PER_BLOCK;
        }

        public static ulong BytesToPages(ulong bytes)
        {
            return (bytes + XvdFile.PAGE_SIZE - 1) / XvdFile.PAGE_SIZE;
        }

        public static ulong OffsetToBlockNumber(ulong offset)
        {
            return offset / XvdFile.BLOCK_SIZE;
        }

        public static ulong OffsetToPageNumber(ulong offset)
        {
            return offset / XvdFile.PAGE_SIZE;
        }

        public static ulong SectorsToBytes(ulong sectors)
        {
            return sectors * XvdFile.SECTOR_SIZE;
        }

        public static ulong LegacySectorsToBytes(ulong sectors)
        {
            return sectors * XvdFile.LEGACY_SECTOR_SIZE;
        }

        public static ulong ComputePagesSpanned(ulong startOffset, ulong lengthBytes)
        {
            return OffsetToPageNumber(startOffset + lengthBytes - 1) -
                   OffsetToPageNumber(lengthBytes) + 1;
        }

        public static ulong QueryFirstDynamicPage(ulong metaDataPagesCount)
        {
            return XvdFile.PAGES_PER_BLOCK * PagesToBlocks(metaDataPagesCount);
        }

        public static ulong ComputeDataBackingPageNumber(XvdType type, ulong numHashLevels, ulong hashPageCount, ulong dataPageNumber)
        {
            if (type > XvdType.Dynamic) // Invalid Xvd Type!
                return dataPageNumber;

            return dataPageNumber + hashPageCount;
        }

        public static ulong CalculateHashBlockNumForBlockNum(XvdType type, ulong hashTreeLevels, ulong numberOfHashedPages,
                                                                ulong blockNum, uint hashLevel, out ulong entryNumInBlock)
        {
            ulong HashBlockExponent(ulong blockCount)
            {
                return (ulong)Math.Pow(0xAA, blockCount);
            }
            
            ulong result = 0xFFFF;
            entryNumInBlock = 0;

            if ((uint)type > 1 || hashLevel > 3)
                return result; // Invalid data

            if (hashLevel == 0)
                entryNumInBlock = blockNum % 0xAA;
            else
                entryNumInBlock = blockNum / HashBlockExponent(hashLevel) % 0xAA;

            if (hashLevel == 3)
                return 0;

            result = blockNum / HashBlockExponent(hashLevel + 1);
            hashTreeLevels -= hashLevel + 1;

            if (hashLevel == 0 && hashTreeLevels > 0)
            {
                result += (numberOfHashedPages + HashBlockExponent(2) - 1) / HashBlockExponent(2);
                hashTreeLevels--;
            }

            if ((hashLevel == 0 || hashLevel == 1) && hashTreeLevels > 0)
            {
                result += (numberOfHashedPages + HashBlockExponent(3) - 1) / HashBlockExponent(3);
                hashTreeLevels--;
            }

            if (hashTreeLevels > 0)
                result += (numberOfHashedPages + HashBlockExponent(4) - 1) / HashBlockExponent(4);

            return result;
        }

        public static ulong CalculateNumHashBlocksInLevel(ulong size, ulong hashLevel, bool resilient)
        {
            ulong hashBlocks = 0;

            switch (hashLevel)
            {
                case 0:
                    hashBlocks = (size + XvdFile.DATA_BLOCKS_IN_LEVEL0_HASHTREE - 1) / XvdFile.DATA_BLOCKS_IN_LEVEL0_HASHTREE;
                    break;
                case 1:
                    hashBlocks = (size + XvdFile.DATA_BLOCKS_IN_LEVEL1_HASHTREE - 1) / XvdFile.DATA_BLOCKS_IN_LEVEL1_HASHTREE;
                    break;
                case 2:
                    hashBlocks = (size + XvdFile.DATA_BLOCKS_IN_LEVEL2_HASHTREE - 1) / XvdFile.DATA_BLOCKS_IN_LEVEL2_HASHTREE;
                    break;
                case 3:
                    hashBlocks = (size + XvdFile.DATA_BLOCKS_IN_LEVEL3_HASHTREE - 1) / XvdFile.DATA_BLOCKS_IN_LEVEL3_HASHTREE;
                    break;
            }

            if (resilient)
                hashBlocks *= 2;

            return hashBlocks;
        }

        public static ulong CalculateNumberHashPages(out ulong hashTreeLevels, ulong hashedPagesCount, bool resilient)
        {
            ulong hashTreePageCount = (hashedPagesCount + XvdFile.HASH_ENTRIES_IN_PAGE - 1) / XvdFile.HASH_ENTRIES_IN_PAGE;
            hashTreeLevels = 1;

            if (hashTreePageCount > 1)
            {
                ulong result = 2;
                while (result > 1)
                {
                    result = CalculateNumHashBlocksInLevel(hashedPagesCount, hashTreeLevels, false);
                    hashTreeLevels += 1;
                    hashTreePageCount += result;
                }
            }

            if (resilient)
                hashTreePageCount *= 2;

            return hashTreePageCount;
        }

        // Source: https://github.com/ctatoiu/PS-Azure/blob/master/src/ServiceManagement/Compute/VhdManagement/Model/DiskGeometry.cs
        public static XvdDiskGeometry CalculateDiskGeometry(ulong driveSize, uint sectorSize)
        {
            long totalSectors = (long)(driveSize / sectorSize);
            if (totalSectors > 65535 * 16 * 255)
            {
                totalSectors = 65535 * 16 * 255;
            }

            int sectorsPerTrack;
            int heads;
            long cylinderTimesHeads;
            if (totalSectors >= 65535 * 16 * 63)
            {
                sectorsPerTrack = 255;
                heads = 16;
                cylinderTimesHeads = totalSectors / sectorsPerTrack;
            }
            else
            {
                sectorsPerTrack = 17;
                cylinderTimesHeads = totalSectors / sectorsPerTrack;

                heads = (int)((cylinderTimesHeads + 1023) / 1024);

                if (heads < 4)
                {
                    heads = 4;
                }
                if (cylinderTimesHeads >= heads * 1024 || heads > 16)
                {
                    sectorsPerTrack = 31;
                    heads = 16;
                    cylinderTimesHeads = totalSectors / sectorsPerTrack;
                }
                if (cylinderTimesHeads >= heads * 1024)
                {
                    sectorsPerTrack = 63;
                    heads = 16;
                    cylinderTimesHeads = totalSectors / sectorsPerTrack;
                }
            }
            long cylinders = cylinderTimesHeads / heads;

            return new XvdDiskGeometry()
            {
                Cylinder = (ushort)cylinders,
                Heads = (byte)heads,
                SectorsPerCylinder = (byte)sectorsPerTrack
            };
        }
    }
}
