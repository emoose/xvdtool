using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

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
        /* 0x13C */ public byte[] Reserved0;

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

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x1C)]
        /* 0x270 */ public byte[] Reserved1;
        /* 0x28C */ public long SequenceNumber;
        /* 0x294 */ public ushort RequiredSystemVersion1;
        /* 0x296 */ public ushort RequiredSystemVersion2;
        /* 0x298 */ public ushort RequiredSystemVersion3;
        /* 0x29A */ public ushort RequiredSystemVersion4;

        /* 0x29C */ public uint ODKKeyslotID; // 0x2 for test ODK, 0x0 for retail ODK? (makepkg doesn't set this for test ODK crypted packages?)

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0xB60)]
        /* 0x2A0 */ public byte[] Reserved2;

        /* 0xE00 = END */

        public bool IsSignedWithRedKey
        {
            get
            {
                if (!XvdFile.SignKeyLoaded)
                    return false;

                var hdrRawData = Shared.StructToBytes(this);
                var hdrData = new byte[0xE00];
                for (int i = 0; i < 0xE00; i++)
                    hdrData[i] = 0;

                Array.Copy(hdrRawData, 0x200, hdrData, 0, hdrData.Length - 0x200);
                Array.Resize(ref hdrRawData, 0x200); // hdrRawData is just the signature now

                return HashUtils.VerifySignature(XvdFile.SignKey, "RSAFULLPRIVATEBLOB", hdrRawData, hdrData);
            }
        }
        public bool Resign(byte[] key, string keyType)
        {
            var hdrRawData = Shared.StructToBytes(this);
            var hdrData = new byte[0xE00];
            for (int i = 0; i < 0xE00; i++)
                hdrData[i] = 0;

            Array.Copy(hdrRawData, 0x200, hdrData, 0, hdrData.Length - 0x200);

            return HashUtils.SignData(key, keyType, hdrData, out Signature);
        }

        public bool ResignWithSignKey()
        {
            if (!XvdFile.SignKeyLoaded)
                return false;
            return Resign(XvdFile.SignKey, "RSAFULLPRIVATEBLOB");
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

            b.AppendLineSpace(fmt + (IsSignedWithRedKey ? "Signed" : "Not signed") + " with red key");

            b.AppendLineSpace(fmt + "Using " + (ODKKeyslotID == 2 ? "test" : "unknown") + " ODK(?)");

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
            b.AppendLineSpace(fmt + "Ext Entries: " + ExtEntries.Length);
            foreach(XvdExtEntry entry in ExtEntries)
                b.AppendLineSpace(fmt + entry.ToString(true));

            b.AppendLineSpace(fmt + "Capabilities: " + Capabilities.ToHexString());

            b.AppendLineSpace(fmt + "PE Catalog Hash: " + PECatalogHash.ToHexString());
            b.AppendLineSpace(fmt + "Userdata hash: " + UserDataHash.ToHexString());

            b.AppendLineSpace(fmt + "PE Catalog Caps: " + PECatalogCaps.ToHexString());
            b.AppendLineSpace(fmt + "PE Catalogs: " + PECatalogs.ToHexString());

            b.AppendLineSpace(fmt + "Writeable Expiration Date: 0x" + WriteableExpirationDate.ToString("X"));
            b.AppendLineSpace(fmt + "Writeable Policy flags: 0x" + WriteablePolicyFlags.ToString("X"));
            b.AppendLineSpace(fmt + "Persistent Local storage length: 0x" + PersistentLocalStorageSize.ToString("X"));

            b.AppendLineSpace(fmt + "Sandbox Id: " + new string(SandboxId).Replace("\0", ""));
            b.AppendLineSpace(fmt + "Product Id: " + new Guid(ProductId));
            b.AppendLineSpace(fmt + "PDUID/Build Id: " + new Guid(PDUID));
            b.AppendLineSpace(fmt + "Sequence Number: " + SequenceNumber);
            b.AppendLineSpace(fmt + String.Format("Package Version: {3}.{2}.{1}.{0}", PackageVersion1, PackageVersion2, PackageVersion3, PackageVersion4));
            b.AppendLineSpace(fmt + String.Format("Required System Version: {3}.{2}.{1}.{0}", RequiredSystemVersion1, RequiredSystemVersion2, RequiredSystemVersion3, RequiredSystemVersion4));
            b.AppendLineSpace(fmt + "ODK Keyslot ID: " + ODKKeyslotID.ToString());
            b.AppendLineSpace(fmt + "KeyMaterial:" + Environment.NewLine + fmt + KeyMaterial.ToHexString());

            if (!Reserved0.IsArrayEmpty())
                b.AppendLineSpace(fmt + "Reserved0: " + Environment.NewLine + fmt + Reserved0.ToHexString());
            if (!Reserved1.IsArrayEmpty())
                b.AppendLineSpace(fmt + "Reserved1: " + Environment.NewLine + fmt + Reserved1.ToHexString());
            if (!Reserved2.IsArrayEmpty())
                b.AppendLineSpace(fmt + "Reserved2: " + Environment.NewLine + fmt + Reserved2.ToHexString());

            return b.ToString();
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
    public struct XvdExtEntry
    {
        /* 0x00 */ uint Code;
        /* 0x04 */ uint Length;
        /* 0x08 */ ulong Offset;
        /* 0x10 */ uint DataLength;
        /* 0x14 */ uint Reserved;

        /* 0x18 = END */

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
    public struct XvcUpdateSegmentInfo
    {
        /* 0x0 */ public uint Unknown1;
        /* 0x4 */ public uint Unknown2;
        /* 0x8 */ public uint Unknown3;

        /* 0xC = END */
        
        public override string ToString()
        {
            return ToString(false);
        }
        public string ToString(bool formatted)
        {
            var b = new StringBuilder();
            b.AppendLine("XvcUpdateSegmentInfo");
            b.AppendLine();

            string fmt = formatted ? "    " : "";

            b.AppendLineSpace(fmt + "Unknown1: 0x" + Unknown1.ToString("X"));
            b.AppendLineSpace(fmt + "Unknown2: 0x" + Unknown2.ToString("X"));
            b.AppendLineSpace(fmt + "Unknown3: 0x" + Unknown3.ToString("X"));

            return b.ToString();
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
    public struct XvcRegionHeader
    {
        /* 0x0 */ public uint Id;
        /* 0x4 */ public ushort KeyId;
        /* 0x6 */ public ushort Unknown1;
        /* 0x8 */ public uint Flags;
        /* 0xC */ public uint Unknown2;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)] 
        /* 0x10 */ public string Description; // XVC-HD = Header, XVC-EXVD = Embedded XVD, XVC-MD = XVC metadata, FS-MD = FileSystem metadata

        /* 0x50 */ public ulong Offset;
        /* 0x58 */ public ulong Length;
        /* 0x60 */ public ulong RegionPDUID; // set at UpdateXVCMetadata+22B
        /* 0x68 */ public ulong Unknown3;
        /* 0x70 */ public ulong Unknown4;
        /* 0x78 */ public ulong Unknown5;

        /* 0x80 = END */

        public XvcRegionHeader(XvcRegionHeader src)
        {
            Id = src.Id;
            KeyId = src.KeyId;
            Unknown1 = src.Unknown1;
            Flags = src.Flags;
            Unknown2 = src.Unknown2;
            Description = src.Description;

            Offset = src.Offset;
            Length = src.Length;
            RegionPDUID = src.RegionPDUID;
            Unknown3 = src.Unknown3;
            Unknown4 = src.Unknown4;
            Unknown5 = src.Unknown5;
        }



        public override string ToString()
        {
            return ToString(false);
        }
        public string ToString(bool formatted)
        {
            var b = new StringBuilder();
            b.AppendLine("XvcRegionHeader (ID/EncryptionIV: 0x" + Id.ToString("X8") + "):");

            string fmt = formatted ? "    " : "";

            if(Unknown1 != 0)
                b.AppendLineSpace(fmt + "Unknown1 != 0");
            if (Unknown2 != 0)
                b.AppendLineSpace(fmt + "Unknown2 != 0");
            if (Unknown3 != 0)
                b.AppendLineSpace(fmt + "Unknown3 != 0");
            if (Unknown4 != 0)
                b.AppendLineSpace(fmt + "Unknown4 != 0");
            if (Unknown5 != 0)
                b.AppendLineSpace(fmt + "Unknown5 != 0");

            string keyid = KeyId.ToString("X");
            if (KeyId == 0xFFFF)
                keyid += " (not encrypted)";
            b.AppendLineSpace(fmt + "Description: " + Description.Replace("\0", ""));
            b.AppendLineSpace(fmt + "Key ID: 0x" + keyid);
            b.AppendLineSpace(fmt + "Flags: 0x" + Flags.ToString("X"));
            b.AppendLineSpace(fmt + "Offset: 0x" + Offset.ToString("X"));
            b.AppendLineSpace(fmt + "Length: 0x" + Length.ToString("X"));
            b.AppendLineSpace(fmt + "Region PDUID: 0x" + RegionPDUID.ToString("X"));
            b.AppendLine();
            b.AppendLineSpace(fmt + "Unknown1: 0x" + Unknown1.ToString("X"));
            b.AppendLineSpace(fmt + "Unknown2: 0x" + Unknown2.ToString("X"));
            b.AppendLineSpace(fmt + "Unknown3: 0x" + Unknown3.ToString("X"));
            b.AppendLineSpace(fmt + "Unknown4: 0x" + Unknown4.ToString("X"));
            b.AppendLineSpace(fmt + "Unknown5: 0x" + Unknown5.ToString("X"));

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
        /* 0xD1C */ public ushort Unknown1;
        /* 0xD1E */ public ushort KeyCount;
        /* 0xD20 */ public uint Unknown2;
        /* 0xD24 */ public uint InitialPlayRegionId;
        /* 0xD28 */ public ulong InitialPlayOffset;
        /* 0xD30 */ public long FileTimeCreated;
        /* 0xD38 */ public uint PreviewRegionId;
        /* 0xD3C */ public uint UpdateSegmentCount;
        /* 0xD40 */ public ulong PreviewOffset;
        /* 0xD48 */ public ulong UnusedSpace;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x58)]
        /* 0xD50 */ public byte[] Reserved;

        /* 0xDA8 = END (actually 0x2000 but rest is read in XVDFile class) */

        public bool IsUsingTestCik
        {
            get
            {
                return XvdFile.CikFileLoaded && !XvdFile.GetTestCikKey().ToByteArray().IsArrayEmpty() && EncryptionKeyIds != null && EncryptionKeyIds.Length > 0 && EncryptionKeyIds[0].KeyId.IsEqualTo(XvdFile.GetTestCikKey().ToByteArray());
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
            if (Unknown1 != 0)
                b.AppendLineSpace(fmt + "Unknown1 != 0");
            if (Unknown2 != 0)
                b.AppendLineSpace(fmt + "Unknown2 != 0");
            if (PreviewRegionId != 0)
                b.AppendLineSpace(fmt + "PreviewRegionId != 0");
            if (PreviewOffset != 0)
                b.AppendLineSpace(fmt + "PreviewOffset != 0");
            if (UnusedSpace != 0)
                b.AppendLineSpace(fmt + "UnusedSpace != 0");
            if (!Reserved.IsArrayEmpty())
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
            b.AppendLineSpace(fmt + "Unknown1: 0x" + Unknown1.ToString("X"));
            b.AppendLineSpace(fmt + "Unknown2: 0x" + Unknown2.ToString("X"));

            if (!Reserved.IsArrayEmpty())
                b.AppendLineSpace(fmt + "Reserved: " + Environment.NewLine + fmt + Reserved.ToHexString());

            return b.ToString();
        }
    }
}
