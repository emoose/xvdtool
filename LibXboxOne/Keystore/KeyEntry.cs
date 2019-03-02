using System;

namespace LibXboxOne.Keystore
{
    public interface IKeyEntry
    {
        int DataSize { get; }
        byte[] SHA256Hash { get; }
        KeyType KeyType { get; }
    }

    public class CikKeyEntry : IKeyEntry
    {
        public Guid KeyId { get; }
        public byte[] SHA256Hash { get; }
        public int DataSize { get; }
        public KeyType KeyType => KeyType.CikKey;

        public CikKeyEntry(Guid keyId, string sha256Hash, int dataSize)
        {
            KeyId = keyId;
            SHA256Hash = sha256Hash.ToBytes();
            DataSize = dataSize;
            if (SHA256Hash.Length != 0x20)
                throw new DataMisalignedException("Invalid length for SHA256Hash");
        }
    }

    public class OdkKeyEntry : IKeyEntry
    {
        public uint KeyId { get; }
        public byte[] SHA256Hash { get; }
        public int DataSize { get; }
        public KeyType KeyType => KeyType.OdkKey;

        public OdkKeyEntry(uint keyId, string sha256Hash, int dataSize)
        {
            KeyId = keyId;
            SHA256Hash = sha256Hash.ToBytes();
            DataSize = dataSize;
            if (SHA256Hash.Length != 0x20)
                throw new DataMisalignedException("Invalid length for SHA256Hash");
        }
    }

    public class XvdSignKeyEntry : IKeyEntry
    {
        public int KeyStrengthBits { get; }
        public bool IsPrivate { get; }
        public byte[] SHA256Hash { get; }
        public int DataSize { get; }
        public KeyType KeyType => KeyType.XvdSigningKey;

        public XvdSignKeyEntry(int keyStrengthBits, bool privateKey, string sha256Hash, int dataSize)
        {
            KeyStrengthBits = keyStrengthBits;
            IsPrivate = privateKey;
            SHA256Hash = sha256Hash.ToBytes();
            DataSize = dataSize;
            if (SHA256Hash.Length != 0x20)
                throw new DataMisalignedException("Invalid length for SHA256Hash");
        }
    }
}