using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using LibXboxOne.Keys;

namespace LibXboxOne
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct XvdHeader
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x200)]
        /* 0x0 */ public byte[] Signature; // RSA signature of the hash of 0x200-0xe00

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        /* 0x0 (from signature) */ public char[] Magic; // msft-xvd

        /* 0x8 */ public XvdVolumeFlags VolumeFlags;
        /* 0xC */ public uint FormatVersion; // 3 in latest xvds
        /* 0x10 */ public long FileTimeCreated;
        /* 0x18 */ public ulong DriveSize;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        /* 0x20 */ public byte[] VDUID; // DriveID / ContentID

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        /* 0x30 */ public byte[] UDUID; // UserID

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x20)]
        /* 0x40 */ public byte[] TopHashBlockHash;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x20)]
        /* 0x60 */ public byte[] OriginalXvcDataHash; // hash of XVC data pre-hashtables, with no PDUIDs

        /* 0x80 */ public XvdType Type;
        /* 0x84 */ public XvdContentType ContentType; // if above 0x1A = not an XVC
        /* 0x88 */ public uint EmbeddedXVDLength;
        /* 0x8C */ public uint UserDataLength; // aka Persistent Local Storage ?
        /* 0x90 */ public uint XvcDataLength;
        /* 0x94 */ public uint DynamicHeaderLength;
        /* 0x98 */ public uint BlockSize; // always 0x000AA000, value seems to be used (staticly built into the exe) during xvdsign en/decryption

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x4)]
        /* 0x9C */ public XvdExtEntry[] ExtEntries;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x8)]
        /* 0xFC */ public ushort[] Capabilities;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x20)]
        /* 0x10C */ public byte[] PECatalogHash;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        /* 0x12C */ public byte[] EmbeddedXVD_PDUID;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        /* 0x13C */ public byte[] Reserved13C;

        // encrypted CIK is only used in non-XVC XVDs, field is decrypted with ODK and then used as the CIK to decrypt data blocks
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x20)]
        /* 0x14C */ public byte[] KeyMaterial;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x20)]
        /* 0x16C */ public byte[] UserDataHash;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        /* 0x18C */ public char[] SandboxId;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        /* 0x19C */ public byte[] ProductId;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        /* 0x1AC */ public byte[] PDUID; // BuildID, changed with every XVD package creation

        /* 0x1BC */ public ushort PackageVersion1;
        /* 0x1BE */ public ushort PackageVersion2;
        /* 0x1C0 */ public ushort PackageVersion3;
        /* 0x1C2 */ public ushort PackageVersion4;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        /* 0x1C4 */ public ushort[] PECatalogCaps;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x80)]
        /* 0x1E4 */ public byte[] PECatalogs;

        /* 0x264 */ public uint WriteableExpirationDate;
        /* 0x268 */ public uint WriteablePolicyFlags;
        /* 0x26C */ public uint PersistentLocalStorageSize;
        
        // NEW FIELDS: only seen in SoDTest windows-XVC!
        /* 0x270 */ public byte MutableDataPageCount;
        /* 0x271 */ public byte Unknown271;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        /* 0x272 */ public byte[] Unknown272;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0xA)]
        /* 0x282 */ public byte[] Reserved282;
        /* 0x28C */ public long SequenceNumber;
        /* 0x294 */ public ushort RequiredSystemVersion1;
        /* 0x296 */ public ushort RequiredSystemVersion2;
        /* 0x298 */ public ushort RequiredSystemVersion3;
        /* 0x29A */ public ushort RequiredSystemVersion4;

        /* 0x29C */ public Keys.OdkIndex ODKKeyslotID; // 0x2 for test ODK, 0x0 for retail ODK? (makepkg doesn't set this for test ODK crypted packages?)

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0xB60)]
        /* 0x2A0 */ public byte[] Reserved2A0;

        /* 0xE00 = END */

        public ulong MutableDataLength => XvdMath.PageNumberToOffset(MutableDataPageCount);
        public ulong UserDataPageCount => XvdMath.BytesToPages(UserDataLength);
        public ulong XvcInfoPageCount => XvdMath.BytesToPages(XvcDataLength);
        public ulong EmbeddedXvdPageCount => XvdMath.BytesToPages(EmbeddedXVDLength);
        public ulong DynamicHeaderPageCount => XvdMath.BytesToPages(DynamicHeaderLength);
        public ulong DrivePageCount => XvdMath.BytesToPages(DriveSize);
        public ulong NumberOfHashedPages => (DrivePageCount + UserDataPageCount + XvcInfoPageCount + DynamicHeaderPageCount);
        public ulong NumberOfMetadataPages => (UserDataPageCount + XvcInfoPageCount + DynamicHeaderPageCount);
        public ulong SectorSize => VolumeFlags.HasFlag(XvdVolumeFlags.LegacySectorSize) ?
                                        XvdFile.LEGACY_SECTOR_SIZE :
                                        XvdFile.SECTOR_SIZE;

        public bool IsSigned => !Signature.IsArrayEmpty();

        byte[] GetHeaderWithoutSignature()
        {
            var rawHeader = Shared.StructToBytes(this);
            byte[] headerData = new byte[rawHeader.Length - Signature.Length];
            // Copy headerdata, skipping signature
            Array.Copy(rawHeader, Signature.Length, headerData, 0, headerData.Length);
            return headerData;
        }

        public string SignedBy
        {
            get
            {
                if (!IsSigned)
                    return "<UNSIGNED>";

                var headerData = GetHeaderWithoutSignature();
                foreach(var signKey in DurangoKeys.GetAllXvdSigningKeys())
                {
                    if(signKey.Value.KeyData != null && HashUtils.VerifySignature(signKey.Value.KeyData, Signature, headerData))
                        return signKey.Key;
                }
                return "<UNKNOWN>";
            }
        }

        public bool Resign(byte[] key, string keyType)
        {
            var headerData = GetHeaderWithoutSignature();
            return HashUtils.SignData(key, keyType, headerData, out Signature);
        }

        public bool ResignWithRedKey()
        {
            DurangoKeyEntry key = (DurangoKeyEntry)DurangoKeys.GetSignkeyByName("RedXvdPrivateKey");
            if (key == null || !key.HasKeyData)
                throw new InvalidOperationException("Private Xvd Red key is not loaded, cannot resign xvd header");

            return Resign(key.KeyData, "RSAFULLPRIVATEBLOB");
        }

        public override string ToString()
        {
            return ToString(false);
        }

        public string ToString(bool formatted)
        {
            var b = new StringBuilder();
            b.AppendLine("XvdHeader:");

            string fmt = formatted ? "    " : "";

            if (!Enum.IsDefined(typeof(XvdContentType), ContentType))
                b.AppendLineSpace(fmt + "Unknown content type 0x" + ContentType.ToString("X"));

            b.AppendLineSpace(fmt + $"Signed by: {SignedBy}");

            b.AppendLineSpace(fmt + $"Using ODK keyslot: {ODKKeyslotID}");

            b.AppendLineSpace(fmt + "Read-only flag " + (VolumeFlags.HasFlag(XvdVolumeFlags.ReadOnly) ? "set" : "not set"));

            b.AppendLineSpace(fmt + (VolumeFlags.HasFlag(XvdVolumeFlags.EncryptionDisabled)
                ? "Decrypted"
                : "Encrypted"));

            b.AppendLineSpace(fmt + (VolumeFlags.HasFlag(XvdVolumeFlags.DataIntegrityDisabled)
                ? "Data integrity disabled (doesn't use hash tree)"
                : "Data integrity enabled (uses hash tree)"));
            
            b.AppendLineSpace(fmt + (VolumeFlags.HasFlag(XvdVolumeFlags.LegacySectorSize)
                ? "Legacy Sector Size (512 bytes)"
                : "Sector Size (4096 bytes)"));
            
            b.AppendLineSpace(fmt + "ResiliencyEnabled " + (VolumeFlags.HasFlag(XvdVolumeFlags.ResiliencyEnabled) ? "set" : "not set"));
            b.AppendLineSpace(fmt + "SraReadOnly " + (VolumeFlags.HasFlag(XvdVolumeFlags.SraReadOnly) ? "set" : "not set"));
            
            b.AppendLineSpace(fmt + "RegionIdInXts " + (VolumeFlags.HasFlag(XvdVolumeFlags.RegionIdInXts) ? "set" : "not set"));
            b.AppendLineSpace(fmt + "EraSpecific " + (VolumeFlags.HasFlag(XvdVolumeFlags.EraSpecific) ? "set" : "not set"));

            b.AppendLine();

            b.AppendLineSpace(fmt + "Magic: " + new string(Magic));
            b.AppendLineSpace(fmt + "Volume Flags: 0x" + VolumeFlags.ToString("X"));
            b.AppendLineSpace(fmt + "Format Version: 0x" + FormatVersion.ToString("X"));

            string contentType = "0x" + ContentType.ToString("X") + " (" + ((XvdContentType)ContentType) + ")";

            b.AppendLineSpace(fmt + "File Time Created: " + DateTime.FromFileTime(FileTimeCreated));
            b.AppendLineSpace(fmt + "Drive Size: 0x" + DriveSize.ToString("X"));

            b.AppendLineSpace(fmt + String.Format("VDUID / Drive Id: {0}", new Guid(VDUID)));
            b.AppendLineSpace(fmt + String.Format("UDUID / User Id: {0}", new Guid(UDUID)));

            b.AppendLineSpace(fmt + "Top Hash Block Hash:" + Environment.NewLine + fmt + TopHashBlockHash.ToHexString());
            b.AppendLineSpace(fmt + "Original XVC Data Hash:" + Environment.NewLine + fmt + OriginalXvcDataHash.ToHexString());

            b.AppendLineSpace(fmt + "XvdType: " + Type);
            b.AppendLineSpace(fmt + "Content Type: " + contentType);
            b.AppendLineSpace(fmt + "Embedded XVD PDUID/Build Id: " + new Guid(EmbeddedXVD_PDUID));
            b.AppendLineSpace(fmt + "Embedded XVD Length: 0x" + EmbeddedXVDLength.ToString("X"));
            b.AppendLineSpace(fmt + "User Data Length: 0x" + UserDataLength.ToString("X"));
            b.AppendLineSpace(fmt + "XVC Data Length: 0x" + XvcDataLength.ToString("X"));
            b.AppendLineSpace(fmt + "Dynamic Header Length: 0x" + DynamicHeaderLength.ToString("X"));
            b.AppendLineSpace(fmt + "BlockSize: 0x" + BlockSize.ToString("X"));
            b.AppendLineSpace(fmt + "Ext Entries: " + ExtEntries.Where(e => !e.IsEmpty).Count());
            foreach(XvdExtEntry entry in ExtEntries.Where(e => !e.IsEmpty))
                b.AppendLineSpace(fmt + entry.ToString(true));

            b.AppendLineSpace(fmt + "Capabilities: " + Capabilities.ToHexString());

            b.AppendLineSpace(fmt + "PE Catalog Hash: " + PECatalogHash.ToHexString());
            b.AppendLineSpace(fmt + "Userdata hash: " + UserDataHash.ToHexString());

            b.AppendLineSpace(fmt + "PE Catalog Caps: " + PECatalogCaps.ToHexString());
            b.AppendLineSpace(fmt + "PE Catalogs: " + PECatalogs.ToHexString());

            b.AppendLineSpace(fmt + "Writeable Expiration Date: 0x" + WriteableExpirationDate.ToString("X"));
            b.AppendLineSpace(fmt + "Writeable Policy flags: 0x" + WriteablePolicyFlags.ToString("X"));
            b.AppendLineSpace(fmt + "Persistent Local storage length: 0x" + PersistentLocalStorageSize.ToString("X"));
            b.AppendLineSpace(fmt + $"Mutable data page count: 0x{MutableDataPageCount:X} (0x{MutableDataLength:X} bytes)");

            b.AppendLineSpace(fmt + "Sandbox Id: " + new string(SandboxId).Replace("\0", ""));
            b.AppendLineSpace(fmt + "Product Id: " + new Guid(ProductId));
            b.AppendLineSpace(fmt + "PDUID/Build Id: " + new Guid(PDUID));
            b.AppendLineSpace(fmt + "Sequence Number: " + SequenceNumber);
            b.AppendLineSpace(fmt + String.Format("Package Version: {3}.{2}.{1}.{0}", PackageVersion1, PackageVersion2, PackageVersion3, PackageVersion4));
            b.AppendLineSpace(fmt + String.Format("Required System Version: {3}.{2}.{1}.{0}", RequiredSystemVersion1, RequiredSystemVersion2, RequiredSystemVersion3, RequiredSystemVersion4));
            b.AppendLineSpace(fmt + "ODK Keyslot ID: " + ODKKeyslotID.ToString());
            b.AppendLineSpace(fmt + "KeyMaterial:" + Environment.NewLine + fmt + KeyMaterial.ToHexString());

            if (Unknown271 != 0)
                b.AppendLineSpace(fmt + "Unknown271: " + Unknown271.ToString("X"));

            if (!Unknown272.IsArrayEmpty())
                b.AppendLineSpace(fmt + "Unknown272: " + Unknown272.ToHexString());

            if (!Reserved13C.IsArrayEmpty())
                b.AppendLineSpace(fmt + "Reserved13C: " + Environment.NewLine + fmt + Reserved13C.ToHexString());
            if (!Reserved13C.IsArrayEmpty())
                b.AppendLineSpace(fmt + "Reserved13C: " + Environment.NewLine + fmt + Reserved13C.ToHexString());
            if (!Reserved2A0.IsArrayEmpty())
                b.AppendLineSpace(fmt + "Reserved2A0: " + Environment.NewLine + fmt + Reserved2A0.ToHexString());

            return b.ToString();
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
    public struct XvdExtEntry
    {
        /* 0x00 */ public uint Code;
        /* 0x04 */ public uint Length;
        /* 0x08 */ public ulong Offset;
        /* 0x10 */ public uint DataLength;
        /* 0x14 */ public uint Reserved;

        /* 0x18 = END */

        public bool IsEmpty => Code == 0 && Length == 0 && Offset == 0 && DataLength == 0 && Reserved == 0;

        public override string ToString()
        {
            return ToString(false);
        }

        public string ToString(bool formatted)
        {
            string fmt = formatted ? "    " : "";
            return fmt + $"XvdExtEntry: Code: {Code:X}, Length: {Length:X}, Offset: {Offset:X}, DataLength: {DataLength:X}";
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct XvcUpdateSegment
    {
        /* 0x0 */ public uint PageNum;
        /* 0x4 */ public ulong Hash;

        /* 0xC = END */

        public override string ToString()
        {
            return ToString(false);
        }
        public string ToString(bool formatted)
        {
            var b = new StringBuilder();
            b.AppendLine("XvcUpdateSegment");
            b.AppendLine();

            string fmt = formatted ? "    " : "";

            b.AppendLineSpace(fmt + $"PageNum: 0x{PageNum:X} (@ 0x{XvdMath.PageNumberToOffset(PageNum)})");
            b.AppendLineSpace(fmt + $"Hash: 0x{Hash:X}");

            return b.ToString();
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct XvcRegionSpecifier
    {
        /* 0x0 */ public XvcRegionId RegionId;
        /* 0x4 */ public uint Padding4;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x40)]
        /* 0x8 */ public string Key;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x80)]
        /* 0x88 */ public string Value;

        /* 0x188 = END */

        public override string ToString()
        {
            return ToString(false);
        }
        public string ToString(bool formatted)
        {
            var b = new StringBuilder();
            b.AppendLine("XvcRegionSpecifier");
            b.AppendLine();

            string fmt = formatted ? "    " : "";

            b.AppendLineSpace(fmt + $"Region ID: 0x{((uint)RegionId):X} {RegionId})");
            b.AppendLineSpace(fmt + $"Key: {Key}");
            b.AppendLineSpace(fmt + $"Value: {Key}");

            if (Padding4 != 0)
                b.AppendLineSpace(fmt + $"Padding4: 0x{Padding4:X}");

            return b.ToString();
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
    public struct XvcRegionHeader
    {
        /* 0x0 */ public XvcRegionId Id;
        /* 0x4 */ public ushort KeyId;
        /* 0x6 */ public ushort Padding6;
        /* 0x8 */ public XvcRegionFlags Flags;
        /* 0xC */ public uint FirstSegmentIndex;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
        /* 0x10 */ public string Description; // XVC-HD = Header, XVC-EXVD = Embedded XVD, XVC-MD = XVC metadata, FS-MD = FileSystem metadata

        /* 0x50 */ public ulong Offset;
        /* 0x58 */ public ulong Length;
        /* 0x60 */ public ulong Hash; // aka RegionPDUID

        /* 0x68 */ public ulong Unknown68;
        /* 0x70 */ public ulong Unknown70;
        /* 0x78 */ public ulong Unknown78;

        /* 0x80 = END */

        public override string ToString()
        {
            return ToString(false);
        }
        public string ToString(bool formatted)
        {
            var b = new StringBuilder();
            b.AppendLine($"XvcRegionHeader (ID/EncryptionIV: 0x{((uint)Id):X} {Id}):");

            string fmt = formatted ? "    " : "";

            if (Padding6 != 0)
                b.AppendLineSpace(fmt + "Padding6 != 0");
            if (Unknown68 != 0)
                b.AppendLineSpace(fmt + "Unknown68 != 0");
            if (Unknown70 != 0)
                b.AppendLineSpace(fmt + "Unknown70 != 0");
            if (Unknown78 != 0)
                b.AppendLineSpace(fmt + "Unknown78 != 0");

            string keyid = KeyId.ToString("X");
            if (KeyId == XvcConstants.XVC_KEY_NONE)
                keyid += " (not encrypted)";
            b.AppendLineSpace(fmt + "Description: " + Description.Replace("\0", ""));
            b.AppendLineSpace(fmt + "Key ID: 0x" + keyid);
            b.AppendLineSpace(fmt + "Flags: 0x" + ((uint)Flags).ToString("X"));
            if (Flags.HasFlag(XvcRegionFlags.Resident))
                b.AppendLineSpace(fmt + "    - Resident");
            if (Flags.HasFlag(XvcRegionFlags.InitialPlay))
                b.AppendLineSpace(fmt + "    - InitialPlay");
            if (Flags.HasFlag(XvcRegionFlags.Preview))
                b.AppendLineSpace(fmt + "    - Preview");
            if (Flags.HasFlag(XvcRegionFlags.FileSystemMetadata))
                b.AppendLineSpace(fmt + "    - FileSystemMetadata");
            if (Flags.HasFlag(XvcRegionFlags.Present))
                b.AppendLineSpace(fmt + "    - Present");
            if (Flags.HasFlag(XvcRegionFlags.OnDemand))
                b.AppendLineSpace(fmt + "    - OnDemand");
            if (Flags.HasFlag(XvcRegionFlags.Available))
                b.AppendLineSpace(fmt + "    - Available");
            
            b.AppendLineSpace(fmt + "Offset: 0x" + Offset.ToString("X"));
            b.AppendLineSpace(fmt + "Length: 0x" + Length.ToString("X"));
            b.AppendLineSpace(fmt + "Hash: 0x" + Hash.ToString("X"));
            b.AppendLineSpace(fmt + "First Segment Index: " + FirstSegmentIndex.ToString());
            b.AppendLine();

            if (Unknown68 != 0)
                b.AppendLineSpace(fmt + "Unknown68: 0x" + Unknown68.ToString("X"));
            if (Unknown70 != 0)
                b.AppendLineSpace(fmt + "Unknown70: 0x" + Unknown70.ToString("X"));
            if (Unknown78 != 0)
                b.AppendLineSpace(fmt + "Unknown78: 0x" + Unknown78.ToString("X"));
            if (Padding6 != 0)
                b.AppendLineSpace(fmt + "Padding6: 0x" + Padding6.ToString("X"));

            return b.ToString();
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct XvcEncryptionKeyId
    {
        public bool IsKeyNulled
        {
            get { return KeyId == null || KeyId.IsArrayEmpty(); }
        }

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        public byte[] KeyId;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct XvcInfo
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        /* 0x0 */ public byte[] ContentID;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0xC0)] 
        /* 0x10 */ public XvcEncryptionKeyId[] EncryptionKeyIds;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x100)] 
        /* 0xC10 */ public byte[] Description; // unicode?
        
        /* 0xD10 */ public uint Version;
        /* 0xD14 */ public uint RegionCount;
        /* 0xD18 */ public uint Flags;
        /* 0xD1C */ public ushort PaddingD1C;
        /* 0xD1E */ public ushort KeyCount;
        /* 0xD20 */ public uint UnknownD20;
        /* 0xD24 */ public uint InitialPlayRegionId;
        /* 0xD28 */ public ulong InitialPlayOffset;
        /* 0xD30 */ public long FileTimeCreated;
        /* 0xD38 */ public uint PreviewRegionId;
        /* 0xD3C */ public uint UpdateSegmentCount;
        /* 0xD40 */ public ulong PreviewOffset;
        /* 0xD48 */ public ulong UnusedSpace;
        /* 0xD50 */ public uint RegionSpecifierCount;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x54)]
        /* 0xD54 */ public byte[] ReservedD54;

        /* 0xDA8 = END (actually 0x2000 but rest is read in XVDFile class) */

        public bool IsUsingTestCik
        {
            get
            {
                Guid testCik = new Guid("33EC8436-5A0E-4F0D-B1CE-3F29C3955039");
                return testCik != null &&
                       EncryptionKeyIds != null &&
                       EncryptionKeyIds.Length > 0 &&
                       EncryptionKeyIds[0].KeyId.IsEqualTo(testCik.ToByteArray());
            }
        }

        public bool IsAnyKeySet
        {
            get { return EncryptionKeyIds.Any(keyId => !keyId.IsKeyNulled); }
        }

        public override string ToString()
        {
            return ToString(false);
        }

        public string ToString(bool formatted)
        {
            var b = new StringBuilder();
            b.AppendLine("XvcInfo:");

            string fmt = formatted ? "    " : "";

            if (Flags != 0)
                b.AppendLineSpace(fmt + "Flags != 0");
            if (PaddingD1C != 0)
                b.AppendLineSpace(fmt + "PaddingD1C != 0");
            if (UnknownD20 != 0)
                b.AppendLineSpace(fmt + "UnknownD20 != 0");
            if (PreviewRegionId != 0)
                b.AppendLineSpace(fmt + "PreviewRegionId != 0");
            if (PreviewOffset != 0)
                b.AppendLineSpace(fmt + "PreviewOffset != 0");
            if (UnusedSpace != 0)
                b.AppendLineSpace(fmt + "UnusedSpace != 0");
            if (!ReservedD54.IsArrayEmpty())
                b.AppendLineSpace(fmt + "Reserved != null");

            var signType = "Unsigned/not crypted (/LU)";
            if (KeyCount > 0)
                if (IsUsingTestCik)
                    signType = "Test-crypted (/LT)";
                else if (!IsAnyKeySet)
                    signType = "Unsigned with KeyCount > 0";
                else
                    signType = "Submission-crypted (not using test key) (/L)";

            b.AppendLineSpace(fmt + signType);
            b.AppendLineSpace(fmt + "/updcompat type " + (UpdateSegmentCount == 0 ? "1" : "2"));

            b.AppendLine();

            b.AppendLineSpace(fmt + "ContentID: " + new Guid(ContentID));
            for(int i = 0; i < EncryptionKeyIds.Length; i++)
                if(!EncryptionKeyIds[i].IsKeyNulled)
                    b.AppendLineSpace(fmt + "Encryption Key " + i + " GUID: " + new Guid(EncryptionKeyIds[i].KeyId));

            b.AppendLine();

            b.AppendLineSpace(fmt + "Description: " + Encoding.Unicode.GetString(Description).Replace("\0", ""));
            b.AppendLineSpace(fmt + "Version: 0x" + Version.ToString("X"));
            b.AppendLineSpace(fmt + "Region Count: 0x" + RegionCount.ToString("X"));
            b.AppendLineSpace(fmt + "Flags: 0x" + Flags.ToString("X"));
            b.AppendLineSpace(fmt + "Key Count: 0x" + KeyCount.ToString("X"));
            b.AppendLineSpace(fmt + "InitialPlay Region Id: 0x" + InitialPlayRegionId.ToString("X"));
            b.AppendLineSpace(fmt + "InitialPlay Offset: 0x" + InitialPlayOffset.ToString("X"));
            b.AppendLineSpace(fmt + "File Time Created: " + DateTime.FromFileTime(FileTimeCreated));
            b.AppendLineSpace(fmt + "Preview Region Id: 0x" + PreviewRegionId.ToString("X"));
            b.AppendLineSpace(fmt + "Update Segment Count: 0x" + UpdateSegmentCount.ToString("X"));
            b.AppendLineSpace(fmt + "Preview Offset: 0x" + PreviewOffset.ToString("X"));
            b.AppendLineSpace(fmt + "Unused Space: 0x" + UnusedSpace.ToString("X"));
            b.AppendLine();

            if (PaddingD1C != 0)
                b.AppendLineSpace(fmt + "PaddingD1C: 0x" + PaddingD1C.ToString("X"));

            if (UnknownD20 != 0)
                b.AppendLineSpace(fmt + "UnknownD20: 0x" + UnknownD20.ToString("X"));

            if (!ReservedD54.IsArrayEmpty())
                b.AppendLineSpace(fmt + "ReservedD54: " + Environment.NewLine + fmt + ReservedD54.ToHexString());

            return b.ToString();
        }
    }
}
