using System;

namespace LibXboxOne
{
    public enum XvcLicenseBlockId : uint
    {
        // EKB block IDs follow

        EkbUnknown1 = 1, // length 0x2, value 0x5
        EkbKeyIdString = 2, // length 0x20
        EkbUnknown2 = 3, // length 0x2, value 0x7
        EkbUnknown4 = 4, // length 0x1, value 0x31
        EkbUnknown5 = 5, // length 0x2, value 0x1
        EkbEncryptedCik = 7, // length 0x180

        // SPLicenseBlock block IDs follow

        LicenseSection = 0x14,
        Unknown1 = 0x15,
        Unknown2 = 0x16,
        UplinkKeyId = 0x18,
        KeyId = 0x1A,
        EncryptedCik = 0x1B,
        DiscIdSection = 0x1C,
        Unknown3 = 0x1E,
        Unknown4 = 0x24,
        DiscId = 0x25,
        SignatureSection = 0x28,
        Unknown5 = 0x29,
        Unknown6 = 0x2C,
        PossibleHash = 0x2A,
        PossibleSignature = 0x2B, // length 0x100, could be the encrypted CIK if it's encrypted in the same way as EKB files
    }

    // XvcLicenseBlock can load in SPLicenseBlock data and EKB data
    public class XvcLicenseBlock
    {
        public XvcLicenseBlockId BlockId;
        public uint BlockSize;
        public byte[] BlockData;

        public XvcLicenseBlock BlockDataAsBlock => BlockData.Length < MinBlockLength ? null : new XvcLicenseBlock(BlockData, IsEkbFile);

        public byte[] NextBlockData;
        public XvcLicenseBlock NextBlockDataAsBlock => NextBlockData.Length < MinBlockLength ? null : new XvcLicenseBlock(NextBlockData, IsEkbFile);

        public bool IsEkbFile;

        public int MinBlockLength = 8;

        public XvcLicenseBlock(byte[] data, bool isEkbFile = false)
        {
            IsEkbFile = isEkbFile;
            int idLen = IsEkbFile ? 2 : 4;
            int szLen = 4;

            if (data.Length >= idLen)
                BlockId = (XvcLicenseBlockId)(isEkbFile ? BitConverter.ToUInt16(data, 0) : BitConverter.ToUInt32(data, 0));

            if (isEkbFile && BlockId == (XvcLicenseBlockId)0x4b45)
            {
                // if its an ekb file and we read the magic of the EKB skip 2 bytes
                idLen = 4;
                BlockId = (XvcLicenseBlockId)BitConverter.ToUInt16(data, 2);
            }

            if (!isEkbFile && BlockId == (XvcLicenseBlockId)0x31424b45)
            {
                // if we're not an EKB file but we read the EKB magic act like its an EKB
                IsEkbFile = true;
                idLen = 4;
                BlockId = (XvcLicenseBlockId)BitConverter.ToUInt16(data, 2);
            }

            if (data.Length >= idLen + szLen)
                BlockSize = BitConverter.ToUInt32(data, idLen);

            if (data.Length < BlockSize + (idLen + szLen))
                return;

            BlockData = new byte[BlockSize];
            Array.Copy(data, idLen + szLen, BlockData, 0, BlockSize);

            if (data.Length - (BlockSize + (idLen + szLen)) <= 0)
                return;

            NextBlockData = new byte[data.Length - (BlockSize + (idLen + szLen))];
            Array.Copy(data, BlockSize + (idLen + szLen), NextBlockData, 0, NextBlockData.Length);

            idLen = IsEkbFile ? 2 : 4;
            szLen = 4;
            MinBlockLength = idLen + szLen;
        }
        public XvcLicenseBlock GetBlockWithId(XvcLicenseBlockId id)
        {
            int idLen = IsEkbFile ? 2 : 4;
            int szLen = 4;

            if (BlockId == id)
                return this;

            XvcLicenseBlock block;
            if (BlockSize > idLen + szLen && BlockSize >= idLen + szLen + BlockDataAsBlock.BlockSize)
            {
                block = BlockDataAsBlock.GetBlockWithId(id);
                if (block != null)
                    return block;
            }

            if (NextBlockData.Length <= idLen + szLen || NextBlockData.Length < idLen + szLen + NextBlockDataAsBlock.BlockSize)
                return null;

            block = NextBlockDataAsBlock.GetBlockWithId(id);
            return block;
        }
    }
}
