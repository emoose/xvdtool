using System;
using System.Collections.Generic;
using System.IO;
using LibXboxOne;
using LibXboxOne.Keystore;

namespace DurangoKeyExtractor
{
    public class KeyExtractor
    {
        public readonly Dictionary<string,byte[]> FoundCikKeys;
        public readonly Dictionary<string,byte[]> FoundOdkKeys;
        public readonly Dictionary<string,byte[]> FoundXvdSignKeys;
        public string FilePath { get; private set; }

        public KeyExtractor(string filePath)
        {
            if (!File.Exists(filePath))
                throw new InvalidOperationException($"File {filePath} does not exist");

            FilePath = filePath;
            FoundCikKeys = new Dictionary<string, byte[]>();
            FoundOdkKeys = new Dictionary<string, byte[]>();
            FoundXvdSignKeys = new Dictionary<string, byte[]>();
        }

        void StoreKey(string keyName, IKeyEntry keyEntry, byte[] keyData)
        {

            switch (keyEntry.KeyType)
            {
                case KeyType.CikKey:
                    // Assemble GUID + keyData blob
                    var assembledKey = new byte[0x30];
                    Array.Copy(((CikKeyEntry)keyEntry).KeyId.ToByteArray(), 0, assembledKey, 0, 16);
                    Array.Copy(keyData, 0, assembledKey, 16, 32);

                    FoundCikKeys[keyName] = assembledKey;
                    break;
                case KeyType.OdkKey:
                    FoundOdkKeys[keyName] = keyData;
                    break;
                case KeyType.XvdSigningKey:
                    FoundXvdSignKeys[keyName] = keyData;
                    break;
                default:
                    throw new InvalidDataException("Invalid KeyType provided");
            }
        }

        public int PullKeysFromFile()
        {
            var exeData = File.ReadAllBytes(FilePath);

            int foundCount = 0;
            for (int i = 0; i < exeData.Length - 32; i += 8)
            {
                byte[] hash32 = HashUtils.ComputeSha256(exeData, i, 32);
                foreach(var kvp in DurangoKeys.KnownKeys)
                {
                    string keyName = kvp.Key;
                    IKeyEntry keyEntry = kvp.Value;
                    
                    if ((keyEntry.DataSize == 32 && hash32.IsEqualTo(keyEntry.SHA256Hash)) ||
                        (keyEntry.DataSize != 32 && keyEntry.DataSize <= (exeData.Length - i) &&
                         keyEntry.SHA256Hash.IsEqualTo(HashUtils.ComputeSha256(exeData, i, keyEntry.DataSize))))
                    {
                        Console.WriteLine($"Found {keyEntry.KeyType} \"{keyName}\" at offset 0x{i:X}");
                        
                        byte[] keyData = new byte[keyEntry.DataSize];
                        Array.Copy(exeData, i, keyData, 0, keyData.Length);
                        StoreKey(keyName, keyEntry, keyData);
                        foundCount++;
                    }
                }
            }

            return foundCount;
        }

        public bool SaveFoundKeys(string destinationDirectory)
        {
            if (!Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            foreach(var entry in FoundCikKeys)
            {
                var path = Path.Combine(destinationDirectory, $"CIK_{entry.Key}.bin");
                File.WriteAllBytes(path, entry.Value);
            }

            foreach(var entry in FoundOdkKeys)
            {
                var path = Path.Combine(destinationDirectory, $"ODK_{entry.Key}.bin");
                File.WriteAllBytes(path, entry.Value);
            }

            foreach(var entry in FoundXvdSignKeys)
            {
                var path = Path.Combine(destinationDirectory, $"XVDSIGN_{entry.Key}.bin");
                File.WriteAllBytes(path, entry.Value);
            }

            return true;
        }
    }
}