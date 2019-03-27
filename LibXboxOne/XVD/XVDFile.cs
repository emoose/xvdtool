using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using LibXboxOne.Keys;
using LibXboxOne.ThirdParty;

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
        public static readonly uint HASH_ENTRY_LENGTH_ENCRYPTED = 0x14;

        public static readonly uint HASH_ENTRIES_IN_PAGE = PAGE_SIZE / HASH_ENTRY_LENGTH; // 0xAA
        public static readonly uint PAGES_PER_BLOCK = BLOCK_SIZE / PAGE_SIZE; // 0xAA

        public static readonly uint DATA_BLOCKS_IN_LEVEL0_HASHTREE = HASH_ENTRIES_IN_PAGE; // 0xAA
        public static readonly uint DATA_BLOCKS_IN_LEVEL1_HASHTREE = HASH_ENTRIES_IN_PAGE * DATA_BLOCKS_IN_LEVEL0_HASHTREE; // 0x70E4
        public static readonly uint DATA_BLOCKS_IN_LEVEL2_HASHTREE = HASH_ENTRIES_IN_PAGE * DATA_BLOCKS_IN_LEVEL1_HASHTREE; // 0x4AF768
        public static readonly uint DATA_BLOCKS_IN_LEVEL3_HASHTREE = HASH_ENTRIES_IN_PAGE * DATA_BLOCKS_IN_LEVEL2_HASHTREE; // 0x31C84B10
        #endregion

        public static bool DisableDataHashChecking = false;

        public XvdHeader Header;
        public XvcInfo XvcInfo;

        public List<XvcRegionHeader> RegionHeaders;
        public List<XvcUpdateSegment> UpdateSegments;
        public List<XvcRegionSpecifier> RegionSpecifiers;
        public List<XvcRegionPresenceInfo> RegionPresenceInfo;

        public bool HashTreeValid;
        public bool DataHashTreeValid;
        public bool XvcDataHashValid;

        public bool CikIsDecrypted;

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
        public XvdFilesystem Filesystem { get; private set; }

        private readonly IO _io;

        public readonly string FilePath;

        public DateTime TimeCreated => DateTime.FromFileTime(Header.FileTimeCreated);

        public bool IsXvcFile => XvcContentTypes.Contains(Header.ContentType);

        public bool IsEncrypted => !Header.VolumeFlags.HasFlag(XvdVolumeFlags.EncryptionDisabled);

        public bool IsDataIntegrityEnabled => !Header.VolumeFlags.HasFlag(XvdVolumeFlags.DataIntegrityDisabled);

        public bool IsResiliencyEnabled => Header.VolumeFlags.HasFlag(XvdVolumeFlags.ResiliencyEnabled);

        public bool UsesLegacySectorSize => Header.VolumeFlags.HasFlag(XvdVolumeFlags.LegacySectorSize);

        public ulong EmbeddedXvdOffset => XVD_HEADER_INCL_SIGNATURE_SIZE;

        public ulong MduOffset => XvdMath.PageNumberToOffset(Header.EmbeddedXvdPageCount) +
                                  EmbeddedXvdOffset;

        public ulong HashTreeOffset => Header.MutableDataLength + MduOffset;

        public ulong HashTreePageCount =>
            XvdMath.CalculateNumberHashPages(out _,
                Header.NumberOfHashedPages,
                IsResiliencyEnabled);

        public ulong HashTreeLevels {
            get
            {
                XvdMath.CalculateNumberHashPages(out ulong tmpHashTreeLevels,
                                         Header.NumberOfHashedPages,
                                         IsResiliencyEnabled);
                return tmpHashTreeLevels;
            }
        }

        public ulong UserDataOffset =>
            (IsDataIntegrityEnabled ? XvdMath.PageNumberToOffset(HashTreePageCount) : 0) +
            HashTreeOffset;

        public ulong XvcInfoOffset =>
            XvdMath.PageNumberToOffset(Header.UserDataPageCount) +
            UserDataOffset;

        public ulong DynamicHeaderOffset =>
            XvdMath.PageNumberToOffset(Header.XvcInfoPageCount) +
            XvcInfoOffset;

        public ulong DriveDataOffset =>
            XvdMath.PageNumberToOffset(Header.DynamicHeaderPageCount) +
            DynamicHeaderOffset;

        public uint DataHashEntryLength => IsEncrypted ? HASH_ENTRY_LENGTH_ENCRYPTED : HASH_ENTRY_LENGTH;

        // Used to calculate absolute offset for dynamic data
        public ulong DynamicBaseOffset => XvcInfoOffset;

        public ulong StaticDataLength { get; private set; }

        public XvdFile(string path)
        {
            FilePath = path;
            _io = new IO(path);
            OverrideOdk = OdkIndex.Invalid;
        }

        public uint ReadBat(ulong requestedBlock)
        {
            ulong absoluteAddress = DynamicHeaderOffset + (requestedBlock * sizeof(uint));
            if (absoluteAddress > (DynamicHeaderOffset + Header.DynamicHeaderLength - sizeof(uint)))
            {
                throw new InvalidDataException(
                    $"Out-of-bounds block 0x{requestedBlock:X} requested, addr: 0x{absoluteAddress:X}. " +
                    $"Dynamic header range: 0x{DynamicHeaderOffset:X}-0x{DynamicHeaderOffset+Header.DynamicHeaderLength:X}");
            }

            _io.Stream.Position = (long)absoluteAddress;
            return _io.Reader.ReadUInt32();
        }

        public byte[] ReadBytes(long offset, int length)
        {
            _io.Stream.Seek(offset, SeekOrigin.Begin);
            return _io.Reader.ReadBytes(length);
        }

        public bool VirtualToLogicalDriveOffset(ulong virtualOffset, out ulong logicalOffset)
        {
            logicalOffset = 0;

            if (virtualOffset >= Header.DriveSize)
                throw new InvalidOperationException(
                    $"Virtual offset 0x{virtualOffset:X} is outside drivedata length 0x{Header.DriveSize:X}");
            if (Header.Type > XvdType.Dynamic)
                throw new NotSupportedException($"Xvd type {Header.Type} is unhandled");


            if (Header.Type == XvdType.Dynamic)
            {
                var dataStartOffset = virtualOffset + XvdMath.PageNumberToOffset(Header.NumberOfMetadataPages);
                var pageNumber = XvdMath.OffsetToPageNumber(dataStartOffset);
                var inBlockOffset = XvdMath.InBlockOffset(dataStartOffset);
                var firstDynamicPage = XvdMath.QueryFirstDynamicPage(Header.NumberOfMetadataPages);

                if (pageNumber >= firstDynamicPage)
                {
                    var firstDynamicPageBytes = XvdMath.PageNumberToOffset(firstDynamicPage);
                    var blockNumber = XvdMath.OffsetToBlockNumber(dataStartOffset - firstDynamicPageBytes);
                    ulong allocatedBlock = ReadBat(blockNumber);
                    if (allocatedBlock == INVALID_SECTOR)
                        return false;

                    dataStartOffset = XvdMath.PageNumberToOffset(allocatedBlock) + inBlockOffset;
                    pageNumber = XvdMath.OffsetToPageNumber(dataStartOffset);
                }

                var dataBackingBlockNum = XvdMath.ComputeDataBackingPageNumber(Header.Type,
                                                                        HashTreeLevels,
                                                                        HashTreePageCount,
                                                                        pageNumber);
                logicalOffset = XvdMath.PageNumberToOffset(dataBackingBlockNum);
                logicalOffset += XvdMath.InPageOffset(dataStartOffset);
                logicalOffset += XvdMath.PageNumberToOffset(Header.EmbeddedXvdPageCount);
                logicalOffset += Header.MutableDataLength;
                logicalOffset += XVD_HEADER_INCL_SIGNATURE_SIZE;
                logicalOffset += PAGE_SIZE;
            }
            else
            { // Xvd type fixed
                logicalOffset = virtualOffset;
                logicalOffset += XvdMath.PageNumberToOffset(Header.EmbeddedXvdPageCount);
                logicalOffset += Header.MutableDataLength;
                logicalOffset += XvdMath.PageNumberToOffset(Header.NumberOfMetadataPages);
                logicalOffset += XVD_HEADER_INCL_SIGNATURE_SIZE;
                logicalOffset += PAGE_SIZE;
            }

            return true;
        }

        uint[] GetAllBATEntries()
        {
            var batEntryCount = XvdMath.BytesToBlocks(Header.DriveSize);
            uint[] BatEntries = new uint[batEntryCount];

            for (ulong i=0; i < batEntryCount; i++)
                BatEntries[i] = ReadBat(i);

            return BatEntries;
        }

        ulong CalculateStaticDataLength()
        {
            if (Header.Type == XvdType.Dynamic)
            {
                var smallestPage = GetAllBATEntries().Min();
                return XvdMath.PageNumberToOffset(smallestPage)
                       - XvdMath.PageNumberToOffset(Header.DynamicHeaderPageCount)
                       - XvdMath.PageNumberToOffset(Header.XvcInfoPageCount);
            }
            else if (Header.Type == XvdType.Fixed)
                return 0;
            else
                throw new InvalidProgramException("Unsupported XvdType");
        }

        private void CryptHeaderCik(bool encrypt)
        {
            if ((!encrypt && CikIsDecrypted) || IsXvcFile)
            {
                CikIsDecrypted = true;
                return;
            }

            var odkToUse = OverrideOdk == OdkIndex.Invalid ? Header.ODKKeyslotID : OverrideOdk;

            var odkKey = DurangoKeys.GetOdkById(odkToUse);
            if (odkKey == null)
                throw new InvalidOperationException(
                    $"ODK with Id \'{Header.ODKKeyslotID}\' not found! Cannot crypt CIK in header");

            if (!odkKey.HasKeyData)
                throw new InvalidOperationException(
                    $"ODK with Id \'{Header.ODKKeyslotID}\' is known but not loaded! Cannot crypt CIK in header");

            byte[] nullIv = new byte[16];

            var cipher = Aes.Create();
            cipher.Mode = CipherMode.ECB;
            cipher.Padding = PaddingMode.None;

            var transform = encrypt ? cipher.CreateEncryptor(odkKey.KeyData, nullIv) :
                                                   cipher.CreateDecryptor(odkKey.KeyData, nullIv);

            transform.TransformBlock(Header.KeyMaterial, 0, Header.KeyMaterial.Length, Header.KeyMaterial, 0);

            CikIsDecrypted = !encrypt;
        }

        private bool CryptXvcRegion(int regionIdx, bool encrypt)
        {
            if (XvcInfo.ContentID == null || !XvcInfo.IsAnyKeySet || regionIdx > XvcInfo.RegionCount || RegionHeaders == null || regionIdx >= RegionHeaders.Count)
                return false;

            XvcRegionHeader header = RegionHeaders[regionIdx];
            if (encrypt && header.KeyId == XvcConstants.XVC_KEY_NONE)
                header.KeyId = 0;

            if (header.Length <= 0 || header.Offset <= 0 || header.KeyId == XvcConstants.XVC_KEY_NONE || header.KeyId + 1 > XvcInfo.KeyCount)
                return false;

            GetXvcKey(header.KeyId, out var key);

            if (key == null)
                return false;

            return CryptSectionXts(encrypt, key, (uint)header.Id, header.Offset, header.Length);
        }

        internal bool CryptSectionXts(bool encrypt, byte[] key, uint headerId, ulong offset, ulong length)
        {
            var startPage = XvdMath.OffsetToPageNumber(offset - UserDataOffset);
            ulong numPages = XvdMath.BytesToPages(length);

            // Pre-read data unit numbers to minimize needing to seek around the file
            List<uint> dataUnits = null;
            if (IsDataIntegrityEnabled)
            {
                dataUnits = new List<uint>();
                for (uint page = 0; page < numPages; page++)
                {
                    // fetch dataUnit from hash table entry for this page
                    // TODO: seems we'll have to insert dataUnit when re-adding hashtables...

                    // last 4 bytes of hash entry = dataUnit
                    _io.Stream.Position = (long)CalculateHashEntryOffsetForBlock(startPage + page, 0) + 0x14;
                    dataUnits.Add(_io.Reader.ReadUInt32());
                }
            }

            var tweakAesKey = new byte[0x10];
            var dataAesKey = new byte[0x10];

            var tweak = new byte[0x10];

            // Split tweak- / Data AES key
            Array.Copy(key, tweakAesKey, 0x10);
            Array.Copy(key, 0x10, dataAesKey, 0, 0x10);

            // Copy VDUID and header Id as tweak
            var headerIdBytes = BitConverter.GetBytes(headerId);
            Array.Copy(Header.VDUID, 0, tweak, 0x8, 0x8);
            Array.Copy(headerIdBytes, 0, tweak, 0x4, 0x4);

            var cipher = new AesXtsTransform(tweak, dataAesKey, tweakAesKey, encrypt);

            // Perform crypto!
            _io.Stream.Position = (long)offset;
            for (uint page = 0; page < numPages; page++)
            { 
                var transformedData = new byte[PAGE_SIZE];

                var pageOffset = _io.Stream.Position;
                var origData = _io.Reader.ReadBytes((int)PAGE_SIZE);

                cipher.TransformDataUnit(origData, 0, origData.Length, transformedData, 0, dataUnits?[(int)page] ?? page);

                _io.Stream.Position = pageOffset;
                _io.Writer.Write(transformedData);
            }

            return true;
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
            return _io.Reader.ReadBytes((int)Header.UserDataLength);
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
                    if (header.Length <= 0 || header.Offset <= 0 || header.KeyId == XvcConstants.XVC_KEY_NONE || header.KeyId + 1 > XvcInfo.KeyCount)
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
                if (cikKeyId != Guid.Empty) // if cikKeyId is set, set the XvcInfo key accordingly
                {
                    var key = DurangoKeys.GetCikByGuid(cikKeyId);
                    if (key == null)
                    {
                        throw new InvalidOperationException($"Desired CIK with GUID {cikKeyId} is unknown");
                    }

                    if (!key.HasKeyData)
                    {
                        throw new InvalidOperationException($"Desired CIK with GUID {cikKeyId} is known but not loaded");
                    }

                    XvcInfo.EncryptionKeyIds[0].KeyId = cikKeyId.ToByteArray();
                }

                for (int i = 0; i < RegionHeaders.Count; i++)
                {
                    var header = RegionHeaders[i];
                    if (header.Length <= 0 || header.Offset <= 0 || header.KeyId + 1 > XvcInfo.KeyCount && header.KeyId != XvcConstants.XVC_KEY_NONE)
                        continue;

                    if (header.Id == XvcRegionId.Header ||
                        header.Id == XvcRegionId.EmbeddedXvd ||
                        header.Id == XvcRegionId.MetadataXvc)
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
                if (RegionSpecifiers != null)
                    XvcInfo.RegionSpecifierCount = (uint)RegionSpecifiers.Count;

                _io.Writer.WriteStruct(XvcInfo);

                for (int i = 0; i < XvcInfo.RegionCount; i++)
                    _io.Writer.WriteStruct(RegionHeaders[i]);

                for (int i = 0; i < XvcInfo.UpdateSegmentCount; i++)
                    _io.Writer.WriteStruct(UpdateSegments[i]);

                if (RegionSpecifiers != null)
                    for (int i = 0; i < XvcInfo.RegionSpecifierCount; i++)
                        _io.Writer.WriteStruct(RegionSpecifiers[i]);
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

            if (Header.XvcDataLength > 0 && IsXvcFile)
            {
                _io.Stream.Position = (long)XvcInfoOffset;

                XvcInfo = _io.Reader.ReadStruct<XvcInfo>();

                if (XvcInfo.Version >= 1)
                {
                    RegionHeaders = new List<XvcRegionHeader>();
                    for (int i = 0; i < XvcInfo.RegionCount; i++)
                        RegionHeaders.Add(_io.Reader.ReadStruct<XvcRegionHeader>());

                    UpdateSegments = new List<XvcUpdateSegment>();
                    for (int i = 0; i < XvcInfo.UpdateSegmentCount; i++)
                        UpdateSegments.Add(_io.Reader.ReadStruct<XvcUpdateSegment>());

                    if(XvcInfo.Version >= 2) // RegionSpecifiers / RegionPresenseInfo only seems to be used on XvcInfo v2
                    {
                        RegionSpecifiers = new List<XvcRegionSpecifier>();
                        for (int i = 0; i < XvcInfo.RegionSpecifierCount; i++)
                            RegionSpecifiers.Add(_io.Reader.ReadStruct<XvcRegionSpecifier>());

                        if(Header.MutableDataPageCount > 0)
                        {
                            RegionPresenceInfo = new List<XvcRegionPresenceInfo>();
                            _io.Stream.Position = (long)MduOffset;
                            for (int i = 0; i < XvcInfo.RegionCount; i++)
                                RegionPresenceInfo.Add((XvcRegionPresenceInfo)_io.Reader.ReadByte());
                        }
                    }
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
            StaticDataLength = CalculateStaticDataLength();
            Filesystem = new XvdFilesystem(this);
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

            // fix region headers to match pre-hashtable
            for (int i = 0; i < XvcInfo.RegionCount; i++)
            {
                var region = RegionHeaders[i];
                region.Hash = 0;

                if (IsDataIntegrityEnabled)
                {
                    if (HashTreeOffset >= region.Offset && region.Offset + region.Length > HashTreeOffset)
                        region.Length -= hashTreeSize;
                    else if (region.Offset > HashTreeOffset)
                        region.Offset -= hashTreeSize;
                }

                msIo.Writer.WriteStruct(region);
            }

            for (int i = 0; i < XvcInfo.UpdateSegmentCount; i++)
            {
                var segment = UpdateSegments[i];

                var hashTreeEnd = XvdMath.BytesToPages(HashTreeOffset) + HashTreePageCount;
                if (segment.PageNum >= hashTreeEnd)
                    segment.PageNum -= (uint)HashTreePageCount;

                segment.Hash = 0;

                msIo.Writer.WriteStruct(segment);
            }

            if (RegionSpecifiers != null)
                for (int i = 0; i < XvcInfo.RegionSpecifierCount; i++)
                    msIo.Writer.WriteStruct(RegionSpecifiers[i]);

            if(Header.XvcDataLength > msIo.Stream.Length)
                msIo.Stream.SetLength(Header.XvcDataLength);

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

            if (!AddData(HashTreeOffset, (uint)HashTreePageCount))
                return false;

            Header.VolumeFlags ^= XvdVolumeFlags.DataIntegrityDisabled;

            // todo: calculate hash tree

            return Save();
        }

        internal bool AddData(ulong offset, ulong numPages)
        {
            var page = XvdMath.OffsetToPageNumber(offset);
            var length = numPages * PAGE_SIZE;

            _io.Stream.Position = (long)offset;
            if (!_io.AddBytes((long)length))
                return false;

            if (!IsXvcFile)
                return true;

            if (XvcInfo.InitialPlayOffset > offset)
                XvcInfo.InitialPlayOffset += length;

            if (XvcInfo.PreviewOffset > offset)
                XvcInfo.PreviewOffset += length;

            for (int i = 0; i < RegionHeaders.Count; i++)
            {
                var region = RegionHeaders[i];
                region.Hash = 0; // ???

                if (offset >= region.Offset && region.Offset + region.Length > offset)
                    region.Length += length; // offset is part of region, add to length
                else if (region.Offset > offset)
                    region.Offset += length; // offset is before region, add to offset

                RegionHeaders[i] = region;
            }

            for (int i = 0; i < UpdateSegments.Count; i++)
            {
                var segment = UpdateSegments[i];
                if (segment.PageNum < page)
                    continue;

                segment.PageNum += (uint)numPages;
                UpdateSegments[i] = segment;
            }

            return true;
        }

        internal bool RemoveData(ulong offset, ulong numPages)
        {
            var page = XvdMath.OffsetToPageNumber(offset);
            var length = numPages * PAGE_SIZE;

            _io.Stream.Position = (long)offset;
            if (!_io.DeleteBytes((long)length))
                return false;

            if (!IsXvcFile)
                return true;

            if (XvcInfo.InitialPlayOffset > offset)
                XvcInfo.InitialPlayOffset -= length;

            if (XvcInfo.PreviewOffset > offset)
                XvcInfo.PreviewOffset -= length;

            for (int i = 0; i < RegionHeaders.Count; i++)
            {
                var region = RegionHeaders[i];
                region.Hash = 0; // ???

                if (offset >= region.Offset && region.Offset + region.Length > offset)
                    region.Length -= length; // offset is part of region, reduce length
                else if (region.Offset > offset)
                    region.Offset -= length; // offset is before region, reduce offset

                RegionHeaders[i] = region; // region is a copy instead of a reference due to it being a struct, so we have to replace the original data ourselves
            }

            for(int i = 0; i < UpdateSegments.Count; i++)
            {
                var segment = UpdateSegments[i];
                if (segment.PageNum < page)
                    continue;

                segment.PageNum -= (uint)numPages;
                UpdateSegments[i] = segment;
            }

            return true;
        }

        public bool RemoveMutableData()
        {
            if (Header.MutableDataPageCount <= 0)
                return true;

            if (!RemoveData(MduOffset, Header.MutableDataPageCount))
                return false;

            Header.MutableDataPageCount = 0;

            return Save();
        }

        public bool RemoveHashTree()
        {
            if (!IsDataIntegrityEnabled)
                return true;

            if (!RemoveData(HashTreeOffset, HashTreePageCount))
                return false;

            Header.VolumeFlags ^= XvdVolumeFlags.DataIntegrityDisabled;

            for (int i = 0; i < Header.TopHashBlockHash.Length; i++)
                Header.TopHashBlockHash[i] = 0;

            return Save();
        }

        public ulong CalculateHashEntryOffsetForBlock(ulong blockNum, uint hashLevel)
        {
            var hashBlock = XvdMath.CalculateHashBlockNumForBlockNum(Header.Type, HashTreeLevels, Header.NumberOfHashedPages, blockNum, hashLevel, out var entryNum);
            return HashTreeOffset + XvdMath.PageNumberToOffset(hashBlock) + (entryNum * HASH_ENTRY_LENGTH);
        }

        public ulong[] VerifyDataHashTree(bool rehash = false)
        {
            ulong dataBlockCount = XvdMath.OffsetToPageNumber((ulong)_io.Stream.Length - UserDataOffset);
            var invalidBlocks = new List<ulong>();

            for (ulong i = 0; i < dataBlockCount; i++)
            {
                var hashEntryOffset = CalculateHashEntryOffsetForBlock(i, 0);
                _io.Stream.Position = (long)hashEntryOffset;

                byte[] oldhash = _io.Reader.ReadBytes((int)DataHashEntryLength);

                var dataToHashOffset = XvdMath.PageNumberToOffset(i) + UserDataOffset;
                _io.Stream.Position = (long)dataToHashOffset;

                byte[] data = _io.Reader.ReadBytes((int)PAGE_SIZE);
                byte[] hash = HashUtils.ComputeSha256(data);
                Array.Resize(ref hash, (int)DataHashEntryLength);

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
                        _io.Stream.Position = (long)CalculateHashEntryOffsetForBlock(dataBlockNum, hashTreeLevel - 1);
                        byte[] blockHash = HashUtils.ComputeSha256(_io.Reader.ReadBytes((int)PAGE_SIZE));
                        Array.Resize(ref blockHash, (int)HASH_ENTRY_LENGTH);

                        _io.Stream.Position = (long)CalculateHashEntryOffsetForBlock(dataBlockNum, hashTreeLevel);

                        byte[] oldHash = _io.Reader.ReadBytes((int)HASH_ENTRY_LENGTH);
                        if (!blockHash.IsEqualTo(oldHash))
                        {
                            _io.Stream.Position -= (int)HASH_ENTRY_LENGTH; // todo: maybe return a list of blocks that needed rehashing
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
                        _io.Stream.Position = (long)CalculateHashEntryOffsetForBlock(dataBlockNum, hashTreeLevel - 1);
                        byte[] blockHash = HashUtils.ComputeSha256(_io.Reader.ReadBytes((int)PAGE_SIZE));
                        Array.Resize(ref blockHash, (int)HASH_ENTRY_LENGTH);

                        var upperHashBlockOffset = CalculateHashEntryOffsetForBlock(dataBlockNum, hashTreeLevel);
                        topHashTreeBlock = XvdMath.OffsetToPageNumber(upperHashBlockOffset - HashTreeOffset);
                        _io.Stream.Position = (long)upperHashBlockOffset;

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
                if (new Guid(xvcKey.KeyId) == keyGuid)
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
            b.AppendLineSpace(fmt + $"Page Count: 0x{Header.NumberOfHashedPages:X}");
            b.AppendLineSpace(fmt + $"Embedded XVD Offset: 0x{EmbeddedXvdOffset:X}");
            b.AppendLineSpace(fmt + $"MDU Offset: 0x{MduOffset:X}");
            b.AppendLineSpace(fmt + $"HashTree Offset: 0x{HashTreeOffset:X}");
            b.AppendLineSpace(fmt + $"User Data Offset: 0x{UserDataOffset:X}");
            b.AppendLineSpace(fmt + $"XVC Data Offset: 0x{XvcInfoOffset:X}");
            b.AppendLineSpace(fmt + $"Dynamic Header Offset: 0x{DynamicHeaderOffset:X}");
            b.AppendLineSpace(fmt + $"Drive Data Offset: 0x{DriveDataOffset:X}");

            if (IsDataIntegrityEnabled)
            {
                b.AppendLineSpace(fmt + $"Hash Tree Page Count: 0x{HashTreePageCount:X}");
                b.AppendLineSpace(fmt + $"Hash Tree Levels: 0x{HashTreeLevels:X}");
                b.AppendLineSpace(fmt + $"Hash Tree Valid: {HashTreeValid}");

                if (!DisableDataHashChecking)
                    b.AppendLineSpace(fmt + $"Data Hash Tree Valid: {DataHashTreeValid}");
            }

            if(IsXvcFile)
                b.AppendLineSpace(fmt + $"XVC Data Hash Valid: {XvcDataHashValid}");

            b.AppendLine();
            b.Append(Header.ToString(formatted));

            if (IsXvcFile && XvcInfo.ContentID != null)
            {
                b.AppendLine();
                bool xvcKeyFound = GetXvcKey(0, out var decryptKey);
                if (xvcKeyFound)
                {
                    b.AppendLine($"Decrypt key for xvc keyslot 0: {decryptKey.ToHexString()}");
                    b.AppendLine("(key is wrong though until the obfuscation/encryption on it is figured out)");
                    b.AppendLine();
                }

                b.AppendLine(XvcInfo.ToString(formatted));
            }

            if(RegionHeaders != null)
                for (int i = 0; i < RegionHeaders.Count; i++)
                {
                    b.AppendLine();
                    string presenceInfo = "";
                    if (RegionPresenceInfo != null && RegionPresenceInfo.Count > i)
                    {
                        var presenceFlags = RegionPresenceInfo[i];
                        presenceInfo = " (";
                        presenceInfo += (presenceFlags.HasFlag(XvcRegionPresenceInfo.IsPresent) ? "present" : "not present") + ", ";
                        presenceInfo += presenceFlags.HasFlag(XvcRegionPresenceInfo.IsAvailable) ? "available" : "unavailable";
                        if (((int)presenceFlags & 0xF0) != 0)
                        {
                            presenceInfo += $", on disc {(int)presenceFlags >> 4}";
                        }
                        presenceInfo += ")";
                    }
                    b.AppendLine($"Region {i}{presenceInfo}");
                    b.Append(RegionHeaders[i].ToString(formatted));
                }

            if (UpdateSegments != null && UpdateSegments.Count > 0)
            {
                // have to add segments to a seperate List so we can store the index of them...
                var segments = new List<Tuple<int, XvcUpdateSegment>>();
                for (int i = 0; i < UpdateSegments.Count; i++)
                {
                    if (UpdateSegments[i].Hash == 0)
                        break;
                    segments.Add(Tuple.Create(i, UpdateSegments[i]));
                }

                b.AppendLine();
                b.AppendLine("Update Segments:");
                b.AppendLine();
                b.AppendLine(segments.ToStringTable(
                      new[] { "Id", "PageNum (Offset)", "Hash" },
                      a => a.Item1, a => $"0x{a.Item2.PageNum:X} (0x{XvdMath.PageNumberToOffset(a.Item2.PageNum):X})", a => $"0x{a.Item2.Hash:X}"));
            }


            if (RegionSpecifiers != null && RegionSpecifiers.Count > 0)
            {
                // have to add specifiers to a seperate List so we can store the index of them...
                var specs = new List<Tuple<int, XvcRegionSpecifier>>();
                for (int i = 0; i < RegionSpecifiers.Count; i++)
                {
                    specs.Add(Tuple.Create(i, RegionSpecifiers[i]));
                }

                b.AppendLine();
                b.AppendLine("Region Specifiers:");
                b.AppendLine();
                b.AppendLine(specs.ToStringTable(
                      new[] { "Id", "RegionId", "Key", "Value" },
                      a => a.Item1, a => $"0x{a.Item2.RegionId:X}", a => a.Item2.Key, a => a.Item2.Value));
            }

            if (!IsEncrypted)
            {
                b.AppendLine();
                try
                {
                    b.Append(Filesystem.ToString(formatted));
                }
                catch (Exception e)
                {
                    b.AppendLine($"Failed to get XvdFilesystem info, error: {e}");
                }
            }
            else
                b.AppendLine($"Cannot get XvdFilesystem from encrypted package");

            return b.ToString();
        }
        #endregion

        public void Dispose()
        {
            _io.Dispose();
        }
    }
}
