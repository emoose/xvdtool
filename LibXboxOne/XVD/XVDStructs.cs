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

        /* 0x8 */ public uint VolumeFlags;
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

        /* 0x80 */ public uint Unknown1_HashTableRelated; // can only be 1 or 0, seems to be hash table related
        /* 0x84 */ public uint ContentType; // if above 0x1A = not an XVC
        /* 0x88 */ public uint EmbeddedXVDLength;
        /* 0x8C */ public uint UserDataLength; // aka Persistent Local Storage ?
        /* 0x90 */ public uint XvcDataLength;
        /* 0x94 */ public uint DynamicHeaderLength;
        /* 0x98 */ public uint Unknown2; // always 0x000AA000, value seems to be used (staticly built into the exe) during xvdsign en/decryption

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x60)]
        /* 0x9C */ public byte[] Unknown3; // padding?

        /* 0xFC */ public ulong PECatalogInfo0; // not exactly sure what these PECatalogInfo fields are filled with
        /* 0x104 */ public ulong Unknown4; // ? never seems to be set

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x4)]
        /* 0x10C */ public ulong[] PECatalogInfo1;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        /* 0x12C */ public byte[] EmbeddedXVD_PDUID;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        /* 0x13C */ public byte[] Unknown5;

        // encrypted CIK is only used in non-XVC XVDs, field is decrypted with ODK and then used as the CIK to decrypt data blocks
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x20)]
        /* 0x14C */ public byte[] EncryptedCIK;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x4)]
        /* 0x16C */ public ulong[] PECatalogInfo2;

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

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x14)]
        /* 0x1C4 */ public ulong[] PECatalogInfo3;

        /* 0x264 */ public ulong Unknown6;
        /* 0x26C */ public uint Unknown7;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x24)]
        /* 0x270 */ public byte[] Unknown8;
        
        /* 0x294 */ public ushort RequiredSystemVersion1;
        /* 0x296 */ public ushort RequiredSystemVersion2;
        /* 0x298 */ public ushort RequiredSystemVersion3;
        /* 0x29A */ public ushort RequiredSystemVersion4;

        /* 0x29C */ public uint ODKKeyslotID; // 0x2 for test ODK, 0x0 for retail ODK? (makepkg doesn't set this for test ODK crypted packages?)

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0xB60)]
        /* 0x2A0 */ public byte[] Reserved;

        /* 0xE00 = END */

        public bool IsSignedWithRedKey
        {
            get
            {
                if (XvdFile.DisableNativeFunctions)
                    return false;
                if (!XvdFile.SignKeyLoaded)
                    return false;

                var hdrRawData = Shared.StructToBytes(this);
                var hdrData = new byte[0xE00];
                for (int i = 0; i < 0xE00; i++)
                    hdrData[i] = 0;

                Array.Copy(hdrRawData, 0x200, hdrData, 0, hdrData.Length - 0x200);
                Array.Resize(ref hdrRawData, 0x200); // hdrRawData is just the signature now

                byte[] hash = HashUtils.ComputeSha256(hdrData);
                return HashUtils.VerifySignature(XvdFile.SignKey, "RSAFULLPRIVATEBLOB", hdrRawData, hash) == 0;
            }
        }
        public bool Resign(byte[] key, string keyType)
        {
            if (XvdFile.DisableNativeFunctions)
                return false;

            var hdrRawData = Shared.StructToBytes(this);
            var hdrData = new byte[0xE00];
            for (int i = 0; i < 0xE00; i++)
                hdrData[i] = 0;

            Array.Copy(hdrRawData, 0x200, hdrData, 0, hdrData.Length - 0x200);

            byte[] hash = HashUtils.ComputeSha256(hdrData);
            uint result = HashUtils.SignHash(key, keyType, hash, out Signature);
            return result == 0;
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

            if (FormatVersion != 3)
                b.AppendLineSpace(fmt + "FormatVersion != 3");
            if (Unknown1_HashTableRelated != 1)
                b.AppendLineSpace(fmt + "Unknown1 != 1");
            if (Unknown2 != 0xAA000)
                b.AppendLineSpace(fmt + "Unknown2 != 0xAA000");
            if (!Unknown3.IsArrayEmpty())
                b.AppendLineSpace(fmt + "Unknown3 != null");
            if (Unknown4 != 0)
                b.AppendLineSpace(fmt + "Unknown4 != 0");
            if (!Unknown5.IsArrayEmpty())
                b.AppendLineSpace(fmt + "Unknown5 != null");
            if (Unknown6 != 0)
                b.AppendLineSpace(fmt + "Unknown6 != 0");
            if (Unknown7 != 0)
                b.AppendLineSpace(fmt + "Unknown7 != 0");
            if (!Unknown8.IsArrayEmpty())
                b.AppendLineSpace(fmt + "Unknown8 != null");
            if (!Reserved.IsArrayEmpty())
                b.AppendLineSpace(fmt + "Reserved != null");

            if (!Enum.IsDefined(typeof(XvdContentType), ContentType))
                b.AppendLineSpace(fmt + "Unknown content type 0x" + ContentType.ToString("X"));

            b.AppendLineSpace(fmt + (IsSignedWithRedKey ? "Signed" : "Not signed") + " with red key");

            b.AppendLineSpace(fmt + "Using " + (ODKKeyslotID == 2 ? "test" : "unknown") + " ODK(?)");

            if (VolumeFlags.IsFlagSet((uint)XvdVolumeFlags.SystemFile))
                b.AppendLineSpace(fmt + "System file");

            b.AppendLineSpace(fmt + "Read-only flag " + (VolumeFlags.IsFlagSet((uint)XvdVolumeFlags.ReadOnly) ? "set" : "not set"));

            b.AppendLineSpace(fmt + (VolumeFlags.IsFlagSet((uint)XvdVolumeFlags.EncryptionDisabled)
                ? "Decrypted"
                : "Encrypted"));

            b.AppendLineSpace(fmt + (VolumeFlags.IsFlagSet((uint)XvdVolumeFlags.DataIntegrityDisabled)
                ? "Data integrity disabled (doesn't use hash tree)"
                : "Data integrity enabled (uses hash tree)"));

            b.AppendLineSpace(fmt + "Unknown flag 0x40 " + (VolumeFlags.IsFlagSet((uint)XvdVolumeFlags.Unknown) ? "set" : "not set"));

            b.AppendLine();

            b.AppendLineSpace(fmt + "Magic: " + new string(Magic));
            b.AppendLineSpace(fmt + "Volume Flags: 0x" + VolumeFlags.ToString("X"));

            string contentType = "0x" + ContentType.ToString("X") + " (" + ((XvdContentType)ContentType) + ")";

            b.AppendLineSpace(fmt + "File Time Created: " + DateTime.FromFileTime(FileTimeCreated));
            b.AppendLineSpace(fmt + "Drive Size: 0x" + DriveSize.ToString("X"));
            b.AppendLineSpace(fmt + "Format Version: 0x" + FormatVersion.ToString("X"));

            b.AppendLineSpace(fmt + String.Format("VDUID / Drive Id: {0} (UDUID / User Id: {1})", new Guid(VDUID), new Guid(UDUID)));
            b.AppendLineSpace(fmt + "Content Type: " + contentType);
            b.AppendLineSpace(fmt + "Embedded XVD PDUID/Build Id: " + new Guid(EmbeddedXVD_PDUID));
            b.AppendLineSpace(fmt + "Embedded XVD Length: 0x" + EmbeddedXVDLength.ToString("X"));
            b.AppendLineSpace(fmt + "User Data Length: 0x" + UserDataLength.ToString("X"));
            b.AppendLineSpace(fmt + "XVC Data Length: 0x" + XvcDataLength.ToString("X"));
            b.AppendLineSpace(fmt + "Dynamic Header Length: 0x" + DynamicHeaderLength.ToString("X"));
            b.AppendLineSpace(fmt + "Top Hash Block Hash:" + Environment.NewLine + fmt + TopHashBlockHash.ToHexString());
            b.AppendLineSpace(fmt + "Original XVC Data Hash:" + Environment.NewLine + fmt + OriginalXvcDataHash.ToHexString());
            b.AppendLineSpace(fmt + "Sandbox Id: " + new string(SandboxId).Replace("\0", ""));
            b.AppendLineSpace(fmt + "Product Id: " + new Guid(ProductId));
            b.AppendLineSpace(fmt + "PDUID/Build Id: " + new Guid(PDUID));
            b.AppendLineSpace(fmt + String.Format("Package Version: {3}.{2}.{1}.{0}", PackageVersion1, PackageVersion2, PackageVersion3, PackageVersion4));
            b.AppendLineSpace(fmt + String.Format("Required System Version: {3}.{2}.{1}.{0}", RequiredSystemVersion1, RequiredSystemVersion2, RequiredSystemVersion3, RequiredSystemVersion4));
            b.AppendLineSpace(fmt + "ODK Keyslot ID: " + ODKKeyslotID.ToString());
            b.AppendLineSpace(fmt + "Encrypted CIK:" + Environment.NewLine + fmt + EncryptedCIK.ToHexString());
            b.AppendLineSpace(fmt + "PECatalogInfo0: 0x" + PECatalogInfo0.ToString("X"));

            string catalogInfo1 = "";
            foreach (ulong catalogInfo in PECatalogInfo1)
                catalogInfo1 += "0x" + catalogInfo.ToString("X") + " ";

            b.AppendLine(fmt + "PECatalogInfo1: " + catalogInfo1);

            string catalogInfo2 = "";
            foreach (ulong catalogInfo in PECatalogInfo2)
                catalogInfo2 += "0x" + catalogInfo.ToString("X") + " ";

            b.AppendLine(fmt + "PECatalogInfo2: " + catalogInfo2);

            b.AppendLine(fmt + "PECatalogInfo3: ");
            b.Append(fmt);
            foreach (ulong catalogInfo in PECatalogInfo3)
                b.Append("0x" + catalogInfo.ToString("X") + " ");

            b.AppendLine();
            b.AppendLine();

            b.AppendLineSpace(fmt + "Unknown1: 0x" + Unknown1_HashTableRelated.ToString("X"));
            b.AppendLineSpace(fmt + "Unknown2: 0x" + Unknown2.ToString("X"));
            b.AppendLineSpace(fmt + "Unknown3:" + Environment.NewLine + fmt + Unknown3.ToHexString());
            b.AppendLineSpace(fmt + "Unknown4: 0x" + Unknown4.ToString("X"));
            b.AppendLineSpace(fmt + "Unknown5:" + Environment.NewLine + fmt + Unknown5.ToHexString());
            b.AppendLineSpace(fmt + "Unknown6: 0x" + Unknown6.ToString("X"));
            b.AppendLineSpace(fmt + "Unknown7: 0x" + Unknown7.ToString("X"));
            b.AppendLineSpace(fmt + "Unknown8: " + Environment.NewLine + fmt + Unknown8.ToHexString());

            if (!Reserved.IsArrayEmpty())
                b.AppendLineSpace(fmt + "Reserved: " + Environment.NewLine + fmt + Reserved.ToHexString());

            return b.ToString();
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
