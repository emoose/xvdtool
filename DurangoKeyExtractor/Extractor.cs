using System;
using System.Collections.Generic;
using System.IO;
using LibXboxOne;
using LibXboxOne.Keys;

namespace DurangoKeyExtractor
{
    public class KeyExtractor
    {
        public string FilePath { get; private set; }

        public KeyExtractor(string filePath)
        {
            if (!File.Exists(filePath))
                throw new InvalidOperationException($"File {filePath} does not exist");

            FilePath = filePath;
        }

        public int PullKeysFromFile()
        {
            var exeData = File.ReadAllBytes(FilePath);

            DurangoKeyEntry keyEntry;
            int foundCount = 0;
            for (int i = 0; i < exeData.Length - 32; i++)
            {
                byte[] hash32 = HashUtils.ComputeSha256(exeData, i, 32);
                foreach(var kvp in DurangoKeys.GetAllXvdSigningKeys())
                {
                    string keyName = kvp.Key;
                    keyEntry = kvp.Value;

                    if (keyEntry.HasKeyData)
                        continue;
                    else if (keyEntry.DataSize > (exeData.Length - i))
                        continue;
                    
                    byte[] signKeyHash = HashUtils.ComputeSha256(exeData, i, keyEntry.DataSize);

                    if(keyEntry.SHA256Hash.IsEqualTo(signKeyHash))
                    {
                        Console.WriteLine($"Found {keyEntry.KeyType} \"{keyName}\" at offset 0x{i:X}");
                        
                        byte[] keyData = new byte[keyEntry.DataSize];
                        Array.Copy(exeData, i, keyData, 0, keyData.Length);

                        DurangoKeys.LoadSignKey(keyName, keyData, out bool newKey, out keyName);
                        foundCount++;
                    }
                }

                foreach(var kvp in DurangoKeys.GetAllODK())
                {
                    OdkIndex keyId = kvp.Key;
                    keyEntry = kvp.Value;

                    if (keyEntry.HasKeyData)
                        continue;

                    if (hash32.IsEqualTo(keyEntry.SHA256Hash))
                    {
                        Console.WriteLine($"Found {keyEntry.KeyType} \"{keyId}\" at offset 0x{i:X}");
                        
                        byte[] keyData = new byte[keyEntry.DataSize];
                        Array.Copy(exeData, i, keyData, 0, keyData.Length);

                        DurangoKeys.LoadOdkKey(keyId, keyData, out bool newKey);
                        foundCount++;
                    }
                }

                foreach(var kvp in DurangoKeys.GetAllCIK())
                {
                    Guid keyId = kvp.Key;
                    keyEntry = kvp.Value;

                    if (keyEntry.HasKeyData)
                        continue;

                    if (hash32.IsEqualTo(keyEntry.SHA256Hash))
                    {
                        Console.WriteLine($"Found {keyEntry.KeyType} \"{keyId}\" at offset 0x{i:X}");
                        
                        byte[] keyData = new byte[0x10 + keyEntry.DataSize];
                        Array.Copy(keyId.ToByteArray(), 0, keyData, 0, 0x10);
                        Array.Copy(exeData, i, keyData, 0x10, keyEntry.DataSize);

                        DurangoKeys.LoadCikKeys(keyData, out Guid[] keyGuid);
                        foundCount++;
                    }
                }
            }

            return foundCount;
        }

        public bool SaveFoundKeys(string destinationDirectory)
        {
            var path = String.Empty;

            foreach (var keyType in Enum.GetNames(typeof(KeyType)))
            {
                path = Path.Combine(destinationDirectory, $"{keyType}");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }

            /* Xvd signing keys */
            foreach(var kvp in DurangoKeys.GetAllXvdSigningKeys())
            {
                IKeyEntry keyEntry = kvp.Value;

                if (!keyEntry.HasKeyData)
                    continue;
                
                path = Path.Combine(destinationDirectory, $"{KeyType.XvdSigningKey}", $"{kvp.Key}.rsa");
                File.WriteAllBytes(path, keyEntry.KeyData);
            }

            /* ODK */
            foreach(var kvp in DurangoKeys.GetAllODK())
            {
                IKeyEntry keyEntry = kvp.Value;

                if (!keyEntry.HasKeyData)
                    continue;
                
                path = Path.Combine(destinationDirectory, $"{KeyType.Odk}", $"{kvp.Key}.odk");
                File.WriteAllBytes(path, keyEntry.KeyData);
            }

            /* CIK */
            foreach(var kvp in DurangoKeys.GetAllCIK())
            {
                IKeyEntry keyEntry = kvp.Value;

                if (!keyEntry.HasKeyData)
                    continue;
                
                path = Path.Combine(destinationDirectory, $"{KeyType.Cik}", $"{kvp.Key}.cik");

                byte[] keyData = new byte[0x30];
                Array.Copy(kvp.Key.ToByteArray(), 0, keyData, 0, 0x10);
                Array.Copy(keyEntry.KeyData, 0, keyData, 0x10, 0x20);

                File.WriteAllBytes(path, keyData);
            }

            return true;
        }
    }
}