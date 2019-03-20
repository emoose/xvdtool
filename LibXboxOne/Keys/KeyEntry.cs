using System;
using System.IO;
using System.Security.Cryptography;

namespace LibXboxOne.Keys
{
    public interface IKeyEntry
    {
        bool HasKeyData { get; }

        int DataSize { get; }
        byte[] SHA256Hash { get; }
        byte[] KeyData { get; }
        KeyType KeyType { get; }

        void SetKey(byte[] keyData);
    }

    public class DurangoKeyEntry : IKeyEntry
    {
        public bool HasKeyData => (KeyData != null && KeyData.Length == DataSize);
        public byte[] SHA256Hash { get; }
        public int DataSize { get; }
        public byte[] KeyData { get; private set; }
        public KeyType KeyType { get; }

        public DurangoKeyEntry(KeyType keyType, string sha256Hash, int dataSize)
        {
            KeyType = keyType;
            SHA256Hash = sha256Hash.ToBytes();
            DataSize = dataSize;
            if (SHA256Hash.Length != 0x20)
                throw new DataMisalignedException("Invalid length for SHA256Hash");
        }

        public DurangoKeyEntry(KeyType keyType, byte[] keyData)
        {
            KeyType = keyType;
            SHA256Hash = HashUtils.ComputeSha256(keyData);
            DataSize = keyData.Length;
            KeyData = keyData;
        }

        public void SetKey(byte[] newKeyData)
        {
            if (HasKeyData)
                throw new InvalidOperationException($"KeyData is already filled!");
            else if (newKeyData.Length != DataSize)
                throw new InvalidProgramException($"Unexpected keydata of length: {newKeyData.Length} bytes");
            
            KeyData = newKeyData;
        }

        public override string ToString()
        {
            return $"Loaded: {HasKeyData} Hash: {SHA256Hash.ToHexString(false)} Size: {DataSize}";
        }
    }
}