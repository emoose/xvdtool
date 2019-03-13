using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using LibXboxOne.Keys;

namespace LibXboxOne
{
    public class XvdFile : IDisposable
    {
        #region Constants

        public static readonly ulong HEADER_SIGNATURE_SIZE = 0x200;
        public static readonly ulong XVD_HEADER_SIZE = 0x2E00;
        public static readonly ulong XVD_HEADER_INCL_SIGNATURE_SIZE = HEADER_SIGNATURE_SIZE + XVD_HEADER_SIZE;

        public static readonly uint PAGE_SIZE = 0x1000;
        public static readonly uint BLOCK_SIZE = 0xAA000;
        public static readonly uint SECTOR_SIZE = 4096;
        public static readonly uint LEGACY_SECTOR_SIZE = 512;
        public static readonly uint INVALID_SECTOR = 0xFFFFFFFF;

        public static readonly uint HASH_ENTRY_LENGTH = 0x18;

        public static readonly uint HASH_ENTRIES_IN_PAGE = PAGE_SIZE / HASH_ENTRY_LENGTH; // 0xAA
        public static readonly uint PAGES_PER_BLOCK = BLOCK_SIZE / PAGE_SIZE; // 0xAA

        public static readonly uint DATA_BLOCKS_IN_LEVEL0_HASHTREE = HASH_ENTRIES_IN_PAGE; // 0xAA
        public static readonly uint DATA_BLOCKS_IN_LEVEL1_HASHTREE = HASH_ENTRIES_IN_PAGE * DATA_BLOCKS_IN_LEVEL0_HASHTREE; // 0x70E4
        public static readonly uint DATA_BLOCKS_IN_LEVEL2_HASHTREE = HASH_ENTRIES_IN_PAGE * DATA_BLOCKS_IN_LEVEL1_HASHTREE; // 0x4AF768
        public static readonly uint DATA_BLOCKS_IN_LEVEL3_HASHTREE = HASH_ENTRIES_IN_PAGE * DATA_BLOCKS_IN_LEVEL2_HASHTREE; // 0x31C84B10

        public static readonly uint VHD_BLOCK_SIZE = 2 * 1024 * 1024; // 2 MB
        #endregion

        public static bool DisableDataHashChecking = false;

        public XvdHeader Header;
        public uint[] DynamicHeader;
        public XvcInfo XvcInfo;

        public List<XvcRegionHeader> RegionHeaders;
        public List<XvcUpdateSegmentInfo> UpdateSegments;

        public bool HashTreeValid = false;
        public bool DataHashTreeValid = false;
        public bool XvcDataHashValid = false;

        public bool CikIsDecrypted = false;

        public static XvdContentType[] XvcContentTypes = 
        { // taken from bit test 0x07018042 (00000111000000011000000001000010)
          // idx of each set bit (from right to left) is an XVC-enabled content type
            XvdContentType.Title,
            XvdContentType.Application,
            XvdContentType.MteApp,
            XvdContentType.MteTitle,
            XvdContentType.AppDlc,
            XvdContentType.TitleDlc,
            XvdContentType.UniversalDlc
        };

        public OdkIndex OverrideOdk { get; set; }

        private readonly IO _io;
        private readonly string _filePath;

        public string FilePath
        {
            get { return _filePath; }
        }

        public DateTime TimeCreated
        {
            get { return DateTime.FromFileTime(Header.FileTimeCreated); }
        }

        public bool IsXvcFile
        {
            get { return XvcContentTypes.Contains(Header.ContentType); }
        }

        public bool IsEncrypted
        {
            get { return !Header.VolumeFlags.HasFlag(XvdVolumeFlags.EncryptionDisabled); }
        }

        public bool IsDataIntegrityEnabled
        {
            get { return !Header.VolumeFlags.HasFlag(XvdVolumeFlags.DataIntegrityDisabled); }
        }

        public bool IsResiliencyEnabled
        {
            get { return Header.VolumeFlags.HasFlag(XvdVolumeFlags.ResiliencyEnabled); }
        }

        public bool UsesLegacySectorSize
        {
            get { return Header.VolumeFlags.HasFlag(XvdVolumeFlags.LegacySectorSize); }
        }

        public ulong EmbeddedXvdOffset => XVD_HEADER_INCL_SIGNATURE_SIZE;

        public ulong MduOffset => PageNumberToOffset(Header.EmbeddedXvdPageCount) +
                                  EmbeddedXvdOffset;

        public ulong HashTreeOffset => PageNumberToOffset(Header.NumMDUPages) +
                                       MduOffset;

        public ulong HashTreePageCount {
            get
            {
                return CalculateNumberHashPages(out ulong HashTreeLevels,
                                                Header.NumberOfHashedPages,
                                                IsResiliencyEnabled);
            }
        }

        public ulong HashTreeLevels {
            get
            {
                CalculateNumberHashPages(out ulong tmpHashTreeLevels,
                                         Header.NumberOfHashedPages,
                                         IsResiliencyEnabled);
                return tmpHashTreeLevels;
            }
        }

        public ulong UserDataOffset {
            get
            {
                return (IsDataIntegrityEnabled ? PageNumberToOffset(HashTreePageCount) : 0) +
                       HashTreeOffset;
            }
        }

        public ulong XvcInfoOffset {
            get
            {
                return PageNumberToOffset(Header.UserDataPageCount) +
                       UserDataOffset;
            }
        }

        public ulong DynamicHeaderOffset {
            get
            {
                return PageNumberToOffset(Header.XvcInfoPageCount) +
                       XvcInfoOffset;
            }
        }

        public ulong DriveDataOffset {
            get
            {
                return PageNumberToOffset(Header.DynamicHeaderPageCount) +
                       DynamicHeaderOffset;
            }
        }

        public XvdFile(string path)
        {
            _filePath = path;
            _io = new IO(path);
            OverrideOdk = OdkIndex.Invalid;
        }

        public static bool PagesAligned(ulong page)
        {
            return (page & (PAGE_SIZE - 1)) == 0;
        }

        public static ulong PageAlign(ulong offset)
        {
            return offset & 0xFFFFFFFFFFFFF000;
        }

        public static ulong InBlockOffset(ulong offset)
        {
            return offset & (BLOCK_SIZE - 1);
        }

        public static ulong InPageOffset(ulong offset)
        {
            return offset & (PAGE_SIZE - 1);
        }

        public static ulong BlockNumberToOffset(ulong blockNumber)
        {
            return blockNumber * BLOCK_SIZE;
        }

        public static ulong PageNumberToOffset(ulong pageNumber)
        {
            return pageNumber * PAGE_SIZE;
        }

        public static ulong BytesToBlocks(ulong bytes)
        {
            return (bytes + BLOCK_SIZE - 1) / BLOCK_SIZE;
        }

        public static ulong PagesToBlocks(ulong pages)
        {
            return (pages + PAGES_PER_BLOCK - 1) / PAGES_PER_BLOCK;
        }

        public static ulong BytesToPages(ulong bytes)
        {
            return (bytes + PAGE_SIZE - 1) / PAGE_SIZE;
        }

        public static ulong OffsetToBlockNumber(ulong offset)
        {
            return offset / BLOCK_SIZE;
        }

        public static ulong OffsetToPageNumber(ulong offset)
        {
            return offset / PAGE_SIZE;
        }

        public static ulong SectorsToBytes(ulong sectors)
        {
            return sectors * SECTOR_SIZE;
        }

        public static ulong LegacySectorsToBytes(ulong sectors)
        {
            return sectors * LEGACY_SECTOR_SIZE;
        }

        public static ulong ComputePagesSpanned(ulong startOffset, ulong lengthBytes)
        {
            return OffsetToPageNumber(startOffset + lengthBytes - 1) -
                   OffsetToPageNumber(lengthBytes) + 1;
        }

        static ulong QueryFirstDynamicPage(ulong metaDataPagesCount)
        {
            return (ulong)PAGES_PER_BLOCK * PagesToBlocks(metaDataPagesCount);
        }

        ulong ComputeDataBackingPageNumber(XvdType type, ulong numHashLevels, ulong hashPageCount, ulong dataPageNumber)
        {
            if (type > XvdType.Dynamic) // Invalid Xvd Type!
                return dataPageNumber;

            return dataPageNumber + hashPageCount;    
        }

        ulong ReadBat(ulong requestedBlock)
        {
            if ((int)requestedBlock > DynamicHeader.Length)
            {
                throw new InvalidDataException(
                    $"Out-of-bounds block 0x{requestedBlock:X} requested, Max: 0x{DynamicHeader.Length:X}");
            }

            return DynamicHeader[requestedBlock];
        }

        public bool VirtualToLogicalDriveOffset(ulong virtualOffset, out ulong logicalOffset)
        {
            logicalOffset = 0;

            if (virtualOffset >= Header.DriveSize)
                throw new InvalidOperationException(
                    $"Virtual offset 0x{virtualOffset:X} is outside drivedata length 0x{Header.DriveSize:X}");
            else if (Header.Type > XvdType.Dynamic)
                throw new NotSupportedException($"Xvd type {Header.Type} is unhandled");


            if (Header.Type == XvdType.Dynamic)
            {
                var dataStartOffset = virtualOffset + PageNumberToOffset(Header.NumberOfMetadataPages);
                var pageNumber = OffsetToPageNumber(dataStartOffset);
                var inBlockOffset = InBlockOffset(dataStartOffset);
                var firstDynamicPage = QueryFirstDynamicPage(Header.NumberOfMetadataPages);

                if (pageNumber >= firstDynamicPage)
                {
                    var firstDynamicPageBytes = PageNumberToOffset(firstDynamicPage);
                    var blockNumber = OffsetToBlockNumber(dataStartOffset - firstDynamicPageBytes);
                    ulong allocatedBlock = ReadBat(blockNumber);
                    if (allocatedBlock == INVALID_SECTOR)
                        return false;

                    dataStartOffset = PageNumberToOffset(allocatedBlock) + inBlockOffset;
                    pageNumber = OffsetToPageNumber(dataStartOffset);
                }

                var dataBackingBlockNum = ComputeDataBackingPageNumber(Header.Type,
                                                                        HashTreeLevels,
                                                                        HashTreePageCount,
                                                                        pageNumber);
                logicalOffset = PageNumberToOffset(dataBackingBlockNum);
                logicalOffset += InPageOffset(dataStartOffset);
                logicalOffset += PageNumberToOffset(Header.EmbeddedXvdPageCount);
                logicalOffset += PageNumberToOffset(Header.NumMDUPages);
                logicalOffset += XVD_HEADER_INCL_SIGNATURE_SIZE;
                logicalOffset += PAGE_SIZE;
            }
            else
            { // Xvd type fixed
                logicalOffset = virtualOffset;
                logicalOffset += PageNumberToOffset(Header.EmbeddedXvdPageCount);
                logicalOffset += PageNumberToOffset(Header.NumMDUPages);
                logicalOffset += PageNumberToOffset(Header.NumberOfMetadataPages);
                logicalOffset += XVD_HEADER_INCL_SIGNATURE_SIZE;
                logicalOffset += PAGE_SIZE;
            }

            return true;
        }

        private void CryptHeaderCik(bool encrypt)
        {
            if ((!encrypt && CikIsDecrypted) || IsXvcFile)
            {
                CikIsDecrypted = true;
                return;
            }

            var odkToUse = OverrideOdk == OdkIndex.Invalid ? Header.ODKKeyslotID : OverrideOdk;

            var odkKey = Keys.DurangoKeys.GetOdkById(odkToUse);
            if (odkKey == null)
            {
                throw new InvalidOperationException(
                    $"ODK with Id \'{Header.ODKKeyslotID}\' not found! Cannot crypt CIK in header");
            }
            else if (!odkKey.HasKeyData)
            {
                throw new InvalidOperationException(
                    $"ODK with Id \'{Header.ODKKeyslotID}\' is known but not loaded! Cannot crypt CIK in header");
            }

            byte[] nullIv = new byte[16];

            var cipher = Aes.Create();
            cipher.Mode = CipherMode.ECB;
            cipher.Padding = PaddingMode.None;

            ICryptoTransform transform = encrypt ? cipher.CreateEncryptor(odkKey.KeyData, nullIv) :
                                                   cipher.CreateDecryptor(odkKey.KeyData, nullIv);

            transform.TransformBlock(Header.KeyMaterial, 0, Header.KeyMaterial.Length, Header.KeyMaterial, 0);

            CikIsDecrypted = !encrypt;
        }

        private bool CryptXvcRegion(int regionIdx, bool encrypt)
        {
            if (XvcInfo.ContentID == null || !XvcInfo.IsAnyKeySet || regionIdx > XvcInfo.RegionCount || RegionHeaders == null || regionIdx >= RegionHeaders.Count)
                return false;

            XvcRegionHeader header = RegionHeaders[regionIdx];
            if (encrypt && header.KeyId == 0xFFFF)
                header.KeyId = 0;

            if (header.Length <= 0 || header.Offset <= 0 || header.KeyId == 0xFFFF || (header.KeyId+1) > XvcInfo.KeyCount)
                return false;

            byte[] key;
            GetXvcKey(header.KeyId, out key);

            if (key == null)
                return false;

            return CryptSectionXts(encrypt, key, header.Id, header.Offset, header.Length);
        }

        internal bool CryptSectionXts(bool encrypt, byte[] key, uint headerId, ulong offset, ulong length)
        {
            ulong numDataUnits = BytesToPages(length);
            var headerIdBytes = BitConverter.GetBytes(headerId);

            var tweakAesKey = new byte[0x10];
            var dataAesKey = new byte[0x10];

            var tweak = new byte[0x10];

            // Split tweak- / Data AES key
            Array.Copy(key, tweakAesKey, 0x10);
            Array.Copy(key, 0x10, dataAesKey, 0, 0x10);

            // Copy VDUID and header Id as tweak
            Array.Copy(Header.VDUID, 0, tweak, 0x8, 0x8);
            Array.Copy(headerIdBytes, 0, tweak, 0x4, 0x4);

            var cipher = new AesXtsTransform(tweak, dataAesKey, tweakAesKey, encrypt);

            _io.Stream.Position = (long)offset;
            for (ulong dataUnit = 0; dataUnit < numDataUnits; dataUnit++)
            {
                var transformedData = new byte[PAGE_SIZE];
                var origData = _io.Reader.ReadBytes((int)PAGE_SIZE);
                _io.Stream.Position -= PAGE_SIZE;

                cipher.TransformDataUnit(origData, 0, origData.Length, transformedData, 0, dataUnit);

                _io.Writer.Write(transformedData);
            }
            return true;
        }

        internal static ulong CalculateHashBlockNumForBlockNum(XvdType type, ulong hashTreeLevels, ulong numberOfHashedPages,
                                                                ulong blockNum, uint index, out ulong entryNumInBlock)
        {
            ulong HashBlockExponent(ulong blockCount)
            {
                return (ulong)Math.Pow(0xAA, blockCount);
            }

            long _hashTreeLevels = (long)hashTreeLevels;
            ulong result = 0xFFFF;
            entryNumInBlock = 0;

            if ((uint)type > 1 || index > 3)
                return result; // Invalid data

            if (index == 0)
                entryNumInBlock = blockNum % 0xAA;
            else
                entryNumInBlock = blockNum / HashBlockExponent(index) % 0xAA;

            if (index == 3)
                return 0;

            result = blockNum / HashBlockExponent(index + 1);
            hashTreeLevels -= (index + 1);

            if (index == 0 && hashTreeLevels > 0)
            {
                result += (numberOfHashedPages + HashBlockExponent(2) - 1) / HashBlockExponent(2);
                hashTreeLevels--;
            }
            
            if ((index == 0 || index == 1) && hashTreeLevels > 0)
            {
                result += (numberOfHashedPages + HashBlockExponent(3) - 1) / HashBlockExponent(3);
                hashTreeLevels--;
            }

            if (hashTreeLevels > 0 )
                result += (numberOfHashedPages + HashBlockExponent(4) - 1) / HashBlockExponent(4);

            return result;
        }

        internal static ulong CalculateNumHashBlocksInLevel(ulong size, ulong idx, bool resilient)
        {
            ulong hashBlocks = 0;

            switch(idx)
            {
                case 0:
                    hashBlocks = (size + DATA_BLOCKS_IN_LEVEL0_HASHTREE - 1) / DATA_BLOCKS_IN_LEVEL0_HASHTREE;
                    break;
                case 1:
                    hashBlocks = (size + DATA_BLOCKS_IN_LEVEL1_HASHTREE - 1) / DATA_BLOCKS_IN_LEVEL1_HASHTREE;
                    break;
                case 2:
                    hashBlocks = (size + DATA_BLOCKS_IN_LEVEL2_HASHTREE - 1) / DATA_BLOCKS_IN_LEVEL2_HASHTREE;
                    break;
                case 3:
                    hashBlocks = (size + DATA_BLOCKS_IN_LEVEL3_HASHTREE - 1) / DATA_BLOCKS_IN_LEVEL3_HASHTREE;
                    break;
            }

            if (resilient)
                hashBlocks *= 2;

            return hashBlocks;
        }

        public byte[] ExtractEmbeddedXvd()
        {
            if (Header.EmbeddedXVDLength == 0)
                return null;
            _io.Stream.Position = (long)EmbeddedXvdOffset;
            return _io.Reader.ReadBytes((int)Header.EmbeddedXVDLength);
        }

        public byte[] ExtractUserData()
        {
            if (Header.UserDataLength == 0)
                return null;
            _io.Stream.Position = (long)UserDataOffset;
            return _io.Reader.ReadBytes((int) Header.UserDataLength);
        }

        static ulong CalculateNumberHashPages(out ulong hashTreeLevels, ulong hashedPagesCount, bool resilient)
        {
            
            ulong hashTreePageCount = (hashedPagesCount + HASH_ENTRIES_IN_PAGE - 1) / HASH_ENTRIES_IN_PAGE;
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

        public bool Decrypt()
        {
            if (!IsEncrypted)
                return true;

            CryptHeaderCik(false);
            if (!CikIsDecrypted)
                return false;

            bool success;

            if (IsXvcFile)
            {
                for (int i = 0; i < RegionHeaders.Count; i++)
                {
                    XvcRegionHeader header = RegionHeaders[i];
                    if (header.Length <= 0 || header.Offset <= 0 || header.KeyId == 0xFFFF || (header.KeyId + 1) > XvcInfo.KeyCount)
                        continue;
                    if (!CryptXvcRegion(i, false))
                        return false;
                }
                success = true;
            }
            else
            {
                // todo: check with more non-xvc xvds and see if they use any other headerId besides 0x1
                success = CryptSectionXts(false, Header.KeyMaterial, 0x1, UserDataOffset,
                    (ulong)_io.Stream.Length - UserDataOffset);
            }

            if (!success)
                return false;

            Header.VolumeFlags ^= XvdVolumeFlags.EncryptionDisabled;
            Save();

            return true;
        }

        public bool Encrypt(Guid cikKeyId)
        {
            if (IsEncrypted)
                return true;

            bool success;

            if (!IsXvcFile)
            {
                if (Header.KeyMaterial.IsArrayEmpty())
                {
                    // generate a new CIK if there's none specified
                    var rng = new Random();
                    Header.KeyMaterial = new byte[0x20];
                    rng.NextBytes(Header.KeyMaterial);
                }

                // todo: check with more non-xvc xvds and see if they use any other headerId besides 0x1
                success = CryptSectionXts(true, Header.KeyMaterial, 0x1, UserDataOffset,
                    (ulong)_io.Stream.Length - UserDataOffset);
            }
            else
            {
                if (cikKeyId != null && cikKeyId != Guid.Empty) // if cikKeyId is set, set the XvcInfo key accordingly
                {
                    var key = DurangoKeys.GetCikByGuid(cikKeyId);
                    if (key == null)
                    {
                        throw new InvalidOperationException($"Desired CIK with GUID {cikKeyId} is unknown");
                    }
                    else if (!key.HasKeyData)
                    {
                        throw new InvalidOperationException($"Desired CIK with GUID {cikKeyId} is known but not loaded");
                    }

                    XvcInfo.EncryptionKeyIds[0].KeyId = cikKeyId.ToByteArray();
                }

                for (int i = 0; i < RegionHeaders.Count; i++)
                {
                    XvcRegionHeader header = RegionHeaders[i];
                    if (header.Length <= 0 || header.Offset <= 0 || ((header.KeyId + 1) > XvcInfo.KeyCount && header.KeyId != 0xFFFF))
                        continue;

                    if (header.Id == 0x40000005 || header.Id == 0x40000004 || header.Id == 0x40000001)
                        continue; // skip XVD header / EXVD / XVC info

                    if (!CryptXvcRegion(i, true))
                        return false;
                }
                success = true;
            }

            if (!success)
                return false;

            CryptHeaderCik(true);

            Header.VolumeFlags ^= XvdVolumeFlags.EncryptionDisabled;

            // seems the readonly flag gets set when encrypting
            if (!Header.VolumeFlags.HasFlag(XvdVolumeFlags.ReadOnly))
            {
                Header.VolumeFlags ^= XvdVolumeFlags.ReadOnly;
            }

            Save();

            return true;
        }

        public bool Save()
        {
            if (Header.XvcDataLength > 0 && IsXvcFile)
            {
                _io.Stream.Position = (long)XvcInfoOffset;

                XvcInfo.RegionCount = (uint)RegionHeaders.Count;
                XvcInfo.UpdateSegmentCount = (uint)UpdateSegments.Count;

                _io.Writer.WriteStruct(XvcInfo);

                for (int i = 0; i < XvcInfo.RegionCount; i++)
                    _io.Writer.WriteStruct(RegionHeaders[i]);

                for (int i = 0; i < XvcInfo.UpdateSegmentCount; i++)
                    _io.Writer.WriteStruct(UpdateSegments[i]);
            }

            if (IsDataIntegrityEnabled)
            {
// ReSharper disable once UnusedVariable
                ulong[] invalidBlocks = VerifyDataHashTree(true);
// ReSharper disable once UnusedVariable
                bool hashTreeValid = CalculateHashTree();
            }

            _io.Stream.Position = 0;
            _io.Writer.WriteStruct(Header);

            return true;
        }

        public bool Load()
        {
            _io.Stream.Position = 0;
            Header = _io.Reader.ReadStruct<XvdHeader>();

            CikIsDecrypted = !IsEncrypted;

            if (DriveDataOffset >= (ulong)_io.Stream.Length)
                return false;

            if (Header.Type == XvdType.Dynamic)
            {
                _io.Stream.Position = (long)DynamicHeaderOffset;
                DynamicHeader = new uint[Header.DynamicHeaderLength / sizeof(uint)];
                for (int entry = 0; entry < DynamicHeader.Length; entry++)
                {
                    DynamicHeader[entry] = _io.Reader.ReadUInt32();
                }
            }

            if (Header.XvcDataLength > 0 && IsXvcFile)
            {
                _io.Stream.Position = (long)XvcInfoOffset;

                XvcInfo = _io.Reader.ReadStruct<XvcInfo>();

                if (XvcInfo.Version >= 1)
                {
                    RegionHeaders = new List<XvcRegionHeader>();
                    for (int i = 0; i < XvcInfo.RegionCount; i++)
                        RegionHeaders.Add(_io.Reader.ReadStruct<XvcRegionHeader>());

                    UpdateSegments = new List<XvcUpdateSegmentInfo>();
                    for (int i = 0; i < XvcInfo.UpdateSegmentCount; i++)
                        UpdateSegments.Add(_io.Reader.ReadStruct<XvcUpdateSegmentInfo>());
                }
            }

            DataHashTreeValid = true;
            HashTreeValid = true;

            if (IsDataIntegrityEnabled)
            {
                if (!DisableDataHashChecking)
                {
                    ulong[] invalidBlocks = VerifyDataHashTree();
                    DataHashTreeValid = invalidBlocks.Length <= 0;
                }
                HashTreeValid = VerifyHashTree();
            }

            XvcDataHashValid = VerifyXvcHash();
            return true;
        }

        public bool VerifyXvcHash(bool rehash = false)
        {
            if (!IsXvcFile)
                return true;

            ulong hashTreeSize = HashTreePageCount * PAGE_SIZE;

            var ms = new MemoryStream();
            var msIo = new IO(ms);
            msIo.Writer.WriteStruct(XvcInfo);
            for (int i = 0; i < XvcInfo.RegionCount; i++)
                msIo.Writer.WriteStruct(RegionHeaders[i]);

            for (int i = 0; i < XvcInfo.UpdateSegmentCount; i++)
                msIo.Writer.WriteStruct(UpdateSegments[i]);

            msIo.Stream.SetLength(Header.XvcDataLength);

            // fix region headers to match pre-hashtable
            msIo.Stream.Position = 0xDA8 + 0x50;
            for (int i = 0; i < XvcInfo.RegionCount; i++)
            {
                ulong length = RegionHeaders[i].Length;
                ulong offset = RegionHeaders[i].Offset;

                if (IsDataIntegrityEnabled)
                {
                    if ((offset == HashTreeOffset || offset < HashTreeOffset) && HashTreeOffset < (offset + length))
                        length -= hashTreeSize;
                    else if (offset > HashTreeOffset)
                        offset -= hashTreeSize;
                }

                msIo.Writer.Write(offset); // write fixed offset
                msIo.Writer.Write(length); // write fixed length
                msIo.Writer.Write((ulong)0); // null out PDUID

                msIo.Stream.Position += (0x80 - 24);
            }


            if (IsDataIntegrityEnabled)
            {
                // remove hash table offset from the special regions
                if (XvcInfo.InitialPlayOffset > HashTreeOffset)
                {
                    msIo.Stream.Position = 0xD28;
                    msIo.Writer.Write(XvcInfo.InitialPlayOffset - hashTreeSize);
                }

                if (XvcInfo.PreviewOffset > HashTreeOffset)
                {
                    msIo.Stream.Position = 0xD40;
                    msIo.Writer.Write(XvcInfo.PreviewOffset - hashTreeSize);
                }
            }

            byte[] xvcData = ms.ToArray();
            msIo.Dispose();
            byte[] hash = HashUtils.ComputeSha256(xvcData);
            bool isValid = Header.OriginalXvcDataHash.IsEqualTo(hash);

            if (rehash)
                Header.OriginalXvcDataHash = hash;

            return isValid; //todo: investigate why this gets the correct hash for dev XVCs but fails for retail ones, might be to do with retail XVC data having a content ID that doesn't match with VDUID/UDUID
        }

        public bool AddHashTree()
        {
            if (IsDataIntegrityEnabled)
                return true;

            var hashTreeSize = (long) (HashTreePageCount * PAGE_SIZE);

            _io.Stream.Position = (long)HashTreeOffset;
            if (!_io.AddBytes(hashTreeSize))
                return false;

            Header.VolumeFlags ^= XvdVolumeFlags.DataIntegrityDisabled;

            if (IsXvcFile)
            {
                if (XvcInfo.InitialPlayOffset > 0)
                    XvcInfo.InitialPlayOffset += (ulong) hashTreeSize;

                if (XvcInfo.PreviewOffset > 0)
                    XvcInfo.PreviewOffset += (ulong) hashTreeSize;

                foreach (XvcRegionHeader t in RegionHeaders)
                {
                    XvcRegionHeader header = t; // have to make a new object pointer otherwise c# complains

                    ulong regionEnd = header.Offset + header.Length;

                    if ((header.Offset == HashTreeOffset || header.Offset < HashTreeOffset) &&
                        HashTreeOffset < regionEnd)
                        header.Length += (ulong) hashTreeSize;
                    else if (header.Offset > HashTreeOffset)
                        header.Offset += (ulong) hashTreeSize;

                    header.RegionPDUID = 0;
                }
            }

            return Save();

            // todo: figure out update segments and fix them

            //VerifyDataHashTree(true);
            //return CalculateHashTree();
        }

        public bool RemoveHashTree()
        {
            if (!IsDataIntegrityEnabled)
                return true;

            var hashTreeSize = (long)(HashTreePageCount * PAGE_SIZE);

            _io.Stream.Position = (long)HashTreeOffset;
            if (!_io.DeleteBytes(hashTreeSize))
                return false;

            Header.VolumeFlags ^= XvdVolumeFlags.DataIntegrityDisabled;
            
            for (int i = 0; i < Header.TopHashBlockHash.Length; i++)
                Header.TopHashBlockHash[i] = 0;

            if (IsXvcFile)
            {
                if (XvcInfo.InitialPlayOffset > HashTreeOffset)
                    XvcInfo.InitialPlayOffset -= (ulong) hashTreeSize;

                if (XvcInfo.PreviewOffset > HashTreeOffset)
                    XvcInfo.PreviewOffset -= (ulong) hashTreeSize;

                for(int i = 0; i < RegionHeaders.Count; i++)
                {
                    var newHdr = new XvcRegionHeader(RegionHeaders[i]);

                    ulong regionEnd = newHdr.Offset + newHdr.Length;

                    if ((newHdr.Offset == HashTreeOffset || newHdr.Offset < HashTreeOffset) && HashTreeOffset < regionEnd)
                        newHdr.Length -= (ulong)hashTreeSize;
                    else if (newHdr.Offset > HashTreeOffset)
                        newHdr.Offset -= (ulong)hashTreeSize;

                    newHdr.RegionPDUID = 0;

                    RegionHeaders[i] = newHdr;
                }
            }

            // todo: figure out update segments and fix them

            return true;
        }

        public ulong[] VerifyDataHashTree(bool rehash = false)
        {
            ulong dataBlockCount = ((ulong)_io.Stream.Length - UserDataOffset) / PAGE_SIZE;
            var invalidBlocks = new List<ulong>();

            for (ulong i = 0; i < dataBlockCount; i++)
            {
                ulong stackNum;
                var blockNum = CalculateHashBlockNumForBlockNum(Header.Type,
                                                                HashTreeLevels, Header.NumberOfHashedPages,
                                                                (ulong)i, 0, out stackNum);

                var hashEntryOffset = PageNumberToOffset(blockNum) + HashTreeOffset;
                hashEntryOffset += stackNum * HASH_ENTRY_LENGTH;

                _io.Stream.Position = (long)hashEntryOffset;
                byte[] oldhash = _io.Reader.ReadBytes((int)HASH_ENTRY_LENGTH);

                var dataToHashOffset = (PageNumberToOffset(i) + UserDataOffset);

                _io.Stream.Position = (long)dataToHashOffset;
                byte[] data = _io.Reader.ReadBytes((int)PAGE_SIZE);
                byte[] hash = HashUtils.ComputeSha256(data);
                Array.Resize(ref hash, (int)HASH_ENTRY_LENGTH);

                bool writeIdx = false; // encrypted data uses 0x14 hashes with a block IDX added to the end to make the HASH_ENTRY_LENGTH hash
                uint idxToWrite = (uint)i;
                if (IsEncrypted)
                {
                    if (IsXvcFile)
                    {
                        var hdr = new XvcRegionHeader();
                        foreach (var region in RegionHeaders)
                        {
                            if (region.KeyId == 0xFFFF)
                                continue; // skip unencrypted regions

                            if (dataToHashOffset >= region.Offset && dataToHashOffset < (region.Offset + region.Length))
                            {
                                writeIdx = true;
                                hdr = region;
                                break;
                            }
                        }
                        if (hdr.Id != 0)
                        {
                            var regionOffset = dataToHashOffset - hdr.Offset;
                            var regionBlockNo = BytesToPages(regionOffset);
                            idxToWrite = (uint) regionBlockNo;
                        }
                    }
                    else
                    {
                        writeIdx = true;
                        idxToWrite = (uint) i;
                    }
                }

                if (writeIdx)
                {
                    byte[] idxBytes = BitConverter.GetBytes(idxToWrite);
                    Array.Copy(idxBytes, 0, hash, 0x14, 4);
                }

                if (hash.IsEqualTo(oldhash))
                    continue;

                invalidBlocks.Add(i);
                if (!rehash)
                    continue;
                _io.Stream.Position = (long)hashEntryOffset;
                _io.Writer.Write(hash);
            }

            return invalidBlocks.ToArray();
        }

        public bool CalculateHashTree()
        {
            uint blocksPerLevel = 0xAA;
            uint hashTreeLevel = 1;
            while (hashTreeLevel < HashTreeLevels)
            {
                uint dataBlockNum = 0;
                if (Header.NumberOfHashedPages != 0)
                {
                    while (dataBlockNum < Header.NumberOfHashedPages)
                    {
                        ulong entryNum;
                        var blockNum = CalculateHashBlockNumForBlockNum(Header.Type,
                                                                        HashTreeLevels, Header.NumberOfHashedPages,
                                                                        dataBlockNum, hashTreeLevel - 1, out entryNum);
                        _io.Stream.Position = (long)(HashTreeOffset + PageNumberToOffset(blockNum));
                        byte[] blockHash = HashUtils.ComputeSha256(_io.Reader.ReadBytes((int)PAGE_SIZE));
                        Array.Resize(ref blockHash, (int)HASH_ENTRY_LENGTH);

                        ulong entryNum2;
                        var secondBlockNum = CalculateHashBlockNumForBlockNum(Header.Type,
                                                                                HashTreeLevels, Header.NumberOfHashedPages,
                                                                                dataBlockNum, hashTreeLevel, out entryNum2);
                        
                        var hashEntryOffset = HashTreeOffset + PageNumberToOffset(secondBlockNum);
                        hashEntryOffset += (entryNum2 + (entryNum2 * 2)) << 3;
                        _io.Stream.Position = (long)hashEntryOffset;

                        byte[] oldHash = _io.Reader.ReadBytes((int)HASH_ENTRY_LENGTH);
                        if (!blockHash.IsEqualTo(oldHash))
                        {
                            _io.Stream.Position = (long)hashEntryOffset; // todo: maybe return a list of blocks that needed rehashing
                            _io.Writer.Write(blockHash);
                        }

                        dataBlockNum += blocksPerLevel;
                    }
                }
                hashTreeLevel++;
                blocksPerLevel = blocksPerLevel * 0xAA;
            }
            _io.Stream.Position = (long)HashTreeOffset;
            byte[] hash = HashUtils.ComputeSha256(_io.Reader.ReadBytes((int)PAGE_SIZE));
            Header.TopHashBlockHash = hash;

            return true;
        }

        public bool VerifyHashTree()
        {
            if (!IsDataIntegrityEnabled)
                return true;

            _io.Stream.Position = (long)HashTreeOffset;
            byte[] hash = HashUtils.ComputeSha256(_io.Reader.ReadBytes((int)PAGE_SIZE));
            if (!Header.TopHashBlockHash.IsEqualTo(hash))
                return false;

            if (HashTreeLevels == 1)
                return true;

            var blocksPerLevel = 0xAA;
            ulong topHashTreeBlock = 0;
            uint hashTreeLevel = 1;
            while (hashTreeLevel < HashTreeLevels)
            {
                uint dataBlockNum = 0;
                if (Header.NumberOfHashedPages != 0)
                {
                    while (dataBlockNum < Header.NumberOfHashedPages)
                    {
                        ulong entryNum;
                        var blockNum = CalculateHashBlockNumForBlockNum(Header.Type,
                                                                        HashTreeLevels, Header.NumberOfHashedPages,
                                                                        dataBlockNum, hashTreeLevel - 1, out entryNum);

                        _io.Stream.Position = (long) (HashTreeOffset + PageNumberToOffset(blockNum));
                        byte[] blockHash = HashUtils.ComputeSha256(_io.Reader.ReadBytes((int)PAGE_SIZE));
                        Array.Resize(ref blockHash, (int)HASH_ENTRY_LENGTH);

                        ulong entryNum2;
                        var secondBlockNum = CalculateHashBlockNumForBlockNum(Header.Type,
                                                                              HashTreeLevels, Header.NumberOfHashedPages,
                                                                              dataBlockNum, hashTreeLevel, out entryNum2);
                        topHashTreeBlock = secondBlockNum;

                        var hashEntryOffset = HashTreeOffset + PageNumberToOffset(secondBlockNum);
                        hashEntryOffset += (entryNum2 + (entryNum2 * 2)) << 3;
                        _io.Stream.Position = (long)hashEntryOffset;

                        byte[] expectedHash = _io.Reader.ReadBytes((int)HASH_ENTRY_LENGTH);
                        if (!expectedHash.IsEqualTo(blockHash))
                        {
                            // wrong hash
                            return false;
                        }
                        dataBlockNum += (uint)blocksPerLevel;
                    }
                }
                hashTreeLevel++;
                blocksPerLevel = blocksPerLevel * 0xAA;
            }
            if (topHashTreeBlock != 0)
            {
                Console.WriteLine(@"Top level hash page calculated to be at {0}, should be 0!", topHashTreeBlock);
            }
            return true;
        }


        public byte[] CreateVhdFooter()
        {
            var footer = new Vhd.VhdFooter()
            {
                Cookie = Vhd.VhdFooter.GetHeaderCookie(), // conectix
                Features = ((uint)Vhd.VhdDiskFeatures.None).EndianSwap(),
                FileFormatVersion = 0x00010000,
                DataOffset = 0xffffffffffffffff, // Fixed disk: 0xffffffffffffffff, Others: Real value
                TimeStamp = Vhd.VhdFile.GetTimestamp(DateTime.UtcNow),
                CreatorApp = Vhd.VhdCreatorApplication.WindowsDiskMngmt,
                CreatorVersion = 0x03000600,
                CreatorHostOS = Vhd.VhdCreatorHostOs.Windows,
                OriginalSize = ((ulong)Header.DriveSize).EndianSwap(),
                CurrentSize = ((ulong)Header.DriveSize).EndianSwap(),
                DiskGeometry = new Vhd.VhdDiskGeometry()
                {
                    Cylinder = 0,
                    Heads = 0,
                    SectorsPerCylinder = 0
                },
                DiskType = ((uint)Vhd.VhdDiskType.Fixed).EndianSwap(),
                Checksum = 0x0,
                UniqueId = Header.VDUID,
                SavedState = 0,
                Reserved = new byte[0x1AB]
            };

            footer.FixChecksum();
            return Shared.StructToBytes<Vhd.VhdFooter>(footer);
        }

        public bool ExtractFilesystem(string targetFile, bool createVhd)
        {
            using (var fs = File.Open(targetFile, FileMode.Create))
            {
                if (Header.Type == XvdType.Fixed)
                {
                    for (ulong offset = DriveDataOffset; offset < Header.DriveSize; offset += PAGE_SIZE)
                    {
                        _io.Stream.Seek((int)offset, SeekOrigin.Begin);
                        var pageBytes = _io.Reader.ReadBytes((int)PAGE_SIZE);
                        fs.Write(pageBytes, 0, pageBytes.Length);
                    }
                }
                else if (Header.Type == XvdType.Dynamic)
                {
                    var chunkSize = BLOCK_SIZE;
                    byte[] emptyChunk = new byte[chunkSize];

                    ulong batBaseAddress = UserDataOffset + PageNumberToOffset(Header.UserDataPageCount);
                    ulong diffInitialWrite = DriveDataOffset - batBaseAddress;

                    _io.Stream.Seek((int)DriveDataOffset, SeekOrigin.Begin);
                    var ptBytes = _io.Reader.ReadBytes((int)(chunkSize - diffInitialWrite));
                    fs.Write(ptBytes, 0, ptBytes.Length);

                    foreach (ulong batEntry in DynamicHeader)
                    {
                        if (batEntry != INVALID_SECTOR)
                        {
                            var targetOffset = PageNumberToOffset(batEntry);
                            _io.Stream.Seek((long)(batBaseAddress + targetOffset), SeekOrigin.Begin);
                            var data = _io.Reader.ReadBytes((int)chunkSize);
                            fs.Write(data, 0, data.Length);
                        }
                        else
                        {
                            fs.Write(emptyChunk, 0, emptyChunk.Length);
                        }
                    }
                }
                else
                    throw new NotSupportedException($"Invalid xvd type: {Header.Type}");

                if (createVhd)
                {
                    // Align to 2MB blocks
                    ulong alignmentBytes = ((ulong)fs.Length % VHD_BLOCK_SIZE);
                    if (alignmentBytes > 0)
                    {
                        ulong alignmentPadding = VHD_BLOCK_SIZE - alignmentBytes;
                        fs.Write(new byte[alignmentPadding], 0, (int)alignmentPadding);
                    }

                    var footer = CreateVhdFooter();
                    fs.Write(footer, 0, footer.Length);
                }
            }
            return true;
        }

        public bool GetXvcKey(ushort keyIndex, out byte[] keyOutput)
        {
            keyOutput = null;
            if (XvcInfo.EncryptionKeyIds == null || XvcInfo.EncryptionKeyIds.Length < 1 || XvcInfo.KeyCount == 0)
                return false;
            
            XvcEncryptionKeyId xvcKeyEntry = XvcInfo.EncryptionKeyIds[keyIndex];
            if (xvcKeyEntry.IsKeyNulled)
                return false;
            
            return GetXvcKeyByGuid(new Guid(xvcKeyEntry.KeyId), out keyOutput);
        }

        public bool GetXvcKeyByGuid(Guid keyGuid, out byte[] keyOutput)
        {
            keyOutput = null;

            if (XvcInfo.EncryptionKeyIds == null || XvcInfo.EncryptionKeyIds.Length < 1 || XvcInfo.KeyCount == 0)
                return false;
            
            bool keyFound = false;
            foreach(var xvcKey in XvcInfo.EncryptionKeyIds)
            {
                if ((new Guid(xvcKey.KeyId) == keyGuid))
                {
                    keyFound = true;
                }
            }

            if (!keyFound)
            {
                Console.WriteLine($"Key {keyGuid} is not used by this XVC");
                return false;
            }

            if(DurangoKeys.IsCikLoaded(keyGuid))
            {
                keyOutput = DurangoKeys.GetCikByGuid(keyGuid).KeyData;
                return true;
            }

            Console.WriteLine($"Did not find CIK {keyGuid} loaded in Keystorage");
            Console.WriteLine("Checking for XML licenses...");

            string licenseFolder = Path.GetDirectoryName(FilePath);
            if (Path.GetFileName(licenseFolder) == "MSXC")
                licenseFolder = Path.GetDirectoryName(licenseFolder);

            if (String.IsNullOrEmpty(licenseFolder))
                return false;

            licenseFolder = Path.Combine(licenseFolder, "Licenses");

            if (!Directory.Exists(licenseFolder))
                return false;

            foreach (string file in Directory.GetFiles(licenseFolder, "*.xml"))
            {
                var xml = new XmlDocument();
                xml.Load(file);

                var xmlns = new XmlNamespaceManager(xml.NameTable);
                xmlns.AddNamespace("resp", "http://schemas.microsoft.com/xboxlive/security/clas/LicResp/v1");

                XmlNode licenseNode = xml.SelectSingleNode("//resp:SignedLicense", xmlns);
                if (licenseNode == null)
                    continue;

                string signedLicense = licenseNode.InnerText;
                byte[] signedLicenseBytes = Convert.FromBase64String(signedLicense);

                var licenseXml = new XmlDocument();
                licenseXml.LoadXml(Encoding.ASCII.GetString(signedLicenseBytes));

                var xmlns2 = new XmlNamespaceManager(licenseXml.NameTable);
                xmlns2.AddNamespace("resp", "http://schemas.microsoft.com/xboxlive/security/clas/LicResp/v1");

                XmlNode keyIdNode = licenseXml.SelectSingleNode("//resp:KeyId", xmlns2);
                if (keyIdNode == null)
                    continue;

                if (keyGuid != new Guid(keyIdNode.InnerText))
                    continue;

                XmlNode licenseBlockNode = licenseXml.SelectSingleNode("//resp:SPLicenseBlock", xmlns2);
                if (licenseBlockNode == null)
                    continue;

                string licenseBlock = licenseBlockNode.InnerText;
                byte[] licenseBlockBytes = Convert.FromBase64String(licenseBlock);

                var block = new XvcLicenseBlock(licenseBlockBytes);
                var keyIdBlock = block.GetBlockWithId(XvcLicenseBlockId.KeyId);
                if (keyIdBlock == null)
                    continue;

                if (!(new Guid(keyIdBlock.BlockData) == keyGuid))
                    continue;

                var decryptKeyBlock = block.GetBlockWithId(XvcLicenseBlockId.EncryptedCik);
                if (decryptKeyBlock == null)
                    continue;

                keyOutput = decryptKeyBlock.BlockData;
                Console.WriteLine($"Xvd CIK key found in {file}");
                // todo: decrypt/deobfuscate the key

                return true;
            }
            return false;
        }

        public byte[] Read(long offset, int count)
        {
            _io.Stream.Position = offset;
            return _io.Reader.ReadBytes(count);
        }

        #region ToString
        public override string ToString()
        {
            return ToString(false);
        }

        public string ToString(bool formatted)
        {
            var b = new StringBuilder();

            string fmt = formatted ? "    " : "";

            b.AppendLine("XvdMiscInfo:");
            b.AppendLineSpace(fmt + "Page Count: 0x" + Header.NumberOfHashedPages.ToString("X"));
            b.AppendLineSpace(fmt + "Embedded XVD Offset: 0x" + EmbeddedXvdOffset.ToString("X"));
            b.AppendLineSpace(fmt + "MDU Offset: 0x" + MduOffset.ToString("X"));
            b.AppendLineSpace(fmt + "HashTree Offset: 0x" + HashTreeOffset.ToString("X"));
            b.AppendLineSpace(fmt + "User Data Offset: 0x" + UserDataOffset.ToString("X"));
            b.AppendLineSpace(fmt + "XVC Data Offset: 0x" + XvcInfoOffset.ToString("X"));
            b.AppendLineSpace(fmt + "Dynamic Header Offset: 0x" + DynamicHeaderOffset.ToString("X"));
            b.AppendLineSpace(fmt + "Drive Data Offset: 0x" + DriveDataOffset.ToString("X"));

            if (IsDataIntegrityEnabled)
            {
                b.AppendLineSpace(fmt + "Hash Tree Page Count: 0x" + HashTreePageCount.ToString("X"));
                b.AppendLineSpace(fmt + "Hash Tree Levels: 0x" + HashTreeLevels.ToString("X"));
                b.AppendLineSpace(fmt + "Hash Tree Valid: " + (HashTreeValid ? "true" : "false"));

                if (!DisableDataHashChecking)
                    b.AppendLineSpace(fmt + "Data Hash Tree Valid: " + (DataHashTreeValid ? "true" : "false"));
            }

            if(IsXvcFile)
                b.AppendLineSpace(fmt + "XVC Data Hash Valid: " + (XvcDataHashValid ? "true" : "false"));

            b.AppendLine();
            b.Append(Header.ToString(formatted));

            if (XvcInfo.ContentID == null)
                return b.ToString();

            b.AppendLine();
            if (formatted)
            {
                byte[] decryptKey;
                bool xvcKeyFound = GetXvcKey(0, out decryptKey);
                if (xvcKeyFound)
                {
                    b.AppendLine($"Decrypt key for xvc keyslot 0:" + decryptKey.ToHexString());
                    b.AppendLine("(key is wrong though until the obfuscation/encryption on it is figured out)");
                    b.AppendLine();
                }
            } 
            b.AppendLine(XvcInfo.ToString(formatted));

            if(RegionHeaders != null)
                for (int i = 0; i < RegionHeaders.Count; i++)
                {
                    b.AppendLine();
                    b.AppendLine("Region " + i);
                    b.Append(RegionHeaders[i].ToString(formatted));
                }

            if (UpdateSegments != null)
                for (int i = 0; i < UpdateSegments.Count; i++)
                {
                    if (UpdateSegments[i].Unknown1 == 0)
                        break;
                    b.AppendLine();
                    b.AppendLine("Update Segment " + i);
                    b.Append(UpdateSegments[i].ToString(formatted));
                }

            return b.ToString();
        }
        #endregion

        public void Dispose()
        {
            _io.Dispose();
        }
    }
}
