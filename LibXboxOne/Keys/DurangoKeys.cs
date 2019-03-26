using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LibXboxOne.Keys
{
    public static class DurangoKeys
    {
        public static Guid TestCIK => new Guid("33EC8436-5A0E-4F0D-B1CE-3F29C3955039");
        public static string RedXvdPrivateKey => "RedXvdPrivateKey";

        static readonly Dictionary<string,DurangoKeyEntry> SignkeyStorage = new Dictionary<string,DurangoKeyEntry>(){
            // Xvd signing keys
            {"RedXvdPrivateKey", new DurangoKeyEntry(KeyType.XvdSigningKey, sha256Hash: "8E2B60377006D87EE850334C42FC200081386A838C65D96D1EA52032AA9628C5", dataSize: 0x91B)},
            {"GreenXvdPublicKey", new DurangoKeyEntry(KeyType.XvdSigningKey, sha256Hash: "618C5FB1193040AF8BC1C0199B850B4B5C42E43CE388129180284E4EF0B18082", dataSize: 0x21B)},
            {"GreenGamesPublicKey", new DurangoKeyEntry(KeyType.XvdSigningKey, sha256Hash: "183F0AE05431E4AD91554E88946967C872997227DBE6C85116F5FD2FD2D1229E", dataSize: 0x21B)},
        };

        static readonly Dictionary<OdkIndex,DurangoKeyEntry> OdkStorage = new Dictionary<OdkIndex,DurangoKeyEntry>(){
            {OdkIndex.RedOdk, new DurangoKeyEntry(KeyType.Odk, sha256Hash: "CA37132DFB4B811506AE4DC45F45970FED8FE5E58C1BACB259F1B96145B0EBC6", dataSize: 0x20)}
        };

        static readonly Dictionary<Guid,DurangoKeyEntry> CikStorage = new Dictionary<Guid,DurangoKeyEntry>()
        {
            {new Guid("33EC8436-5A0E-4F0D-B1CE-3F29C3955039"), new DurangoKeyEntry(KeyType.Cik, sha256Hash: "6786C11B788ED5CCE3C7695425CB82970347180650893D1B5613B2EFB33F9F4E", dataSize: 0x20)}, // TestCIK
            {new Guid("F0522B7C-7FC1-D806-43E3-68A5DAAB06DA"), new DurangoKeyEntry(KeyType.Cik, sha256Hash: "B767CE5F83224375E663A1E01044EA05E8022C033D96BED952475D87F0566642", dataSize: 0x20)},
        };

        static DurangoKeys()
        {
        }

        public static KeyValuePair<string,DurangoKeyEntry>[] GetAllXvdSigningKeys()
        {
            return SignkeyStorage.ToArray();
        }

        public static KeyValuePair<OdkIndex,DurangoKeyEntry>[] GetAllODK()
        {
            return OdkStorage.ToArray();
        }

        public static KeyValuePair<Guid,DurangoKeyEntry>[] GetAllCIK()
        {
            return CikStorage.ToArray();
        }

        public static bool KnowsSignKeySHA256(byte[] sha256Hash, out string keyName)
        {
            foreach (var kvp in SignkeyStorage)
            {
                if (kvp.Value.SHA256Hash.IsEqualTo(sha256Hash))
                {
                    keyName = kvp.Key;
                    return true;
                }
            }
            keyName = "<UNKNOWN>";
            return false;
        }

        public static bool KnowsOdkSHA256(byte[] sha256Hash, out OdkIndex keyId)
        {
            foreach (var kvp in OdkStorage)
            {
                if (kvp.Value.SHA256Hash.IsEqualTo(sha256Hash))
                {
                    keyId = kvp.Key;
                    return true;
                }
            }
            keyId = OdkIndex.Invalid;
            return false;
        }

        public static bool KnowsCikSHA256(byte[] sha256Hash, out Guid keyGuid)
        {
            foreach (var kvp in CikStorage)
            {
                if (kvp.Value.SHA256Hash.IsEqualTo(sha256Hash))
                {
                    keyGuid = kvp.Key;
                    return true;
                }
            }
            keyGuid = Guid.Empty;
            return false;
        }

        public static bool GetOdkIndexFromString(string name, out OdkIndex odkIndex)
        {
            // First, try to convert to know Enum values
            var success = Enum.TryParse<OdkIndex>(name, true, out odkIndex);
            if (!success)
            {
                odkIndex = OdkIndex.Invalid;
                success = UInt32.TryParse(name, out uint odkUint);
                if (success)
                    // Odk Id is valid uint but we don't know its Enum name yet
                    odkIndex = (OdkIndex)odkUint;
            }
            
            return success;
        }

        public static int LoadCikKeys(byte[] keyData, out Guid[] loadedKeys)
        {
            int foundCount = 0;
            if (keyData.Length < 0x30 || keyData.Length % 0x30 != 0)
                throw new Exception("Misaligned CIK, expecting array of 0x30 bytes: 0x10 bytes: GUID, 0x20 bytes: Key");
            
            int cikKeyCount = keyData.Length / 0x30;
            loadedKeys = new Guid[cikKeyCount];
            using (BinaryReader br = new BinaryReader(new MemoryStream(keyData)))
            {
                for (int keyIndex = 0; keyIndex < cikKeyCount; keyIndex++)
                {
                    var guid = new Guid(br.ReadBytes(0x10));
                    var cikKeyData = br.ReadBytes(0x20);
                    var sha256Hash = HashUtils.ComputeSha256(cikKeyData);
                    bool hashMatches = KnowsCikSHA256(sha256Hash, out Guid verifyGuid);
                    var hashString = sha256Hash.ToHexString("");

                    if (hashMatches && verifyGuid != guid)
                    {
                        Console.WriteLine($"CIK {guid} with hash {hashString} is known as {verifyGuid}");
                        continue;
                    }
                    else if (hashMatches && CikStorage[guid].HasKeyData)
                    {
                        // Duplicate key, already loaded
                        Console.WriteLine($"CIK {guid} is already loaded");
                        continue;
                    }

                    CikStorage[guid] = new DurangoKeyEntry(KeyType.Cik, cikKeyData);
                    foundCount++;
                }
            }
            return foundCount;
        }

        public static bool LoadOdkKey(OdkIndex keyId, byte[] keyData, out bool isNewKey)
        {
            isNewKey = false;
            byte[] sha256Hash = HashUtils.ComputeSha256(keyData);
            DurangoKeyEntry existingKey = GetOdkById(keyId);

            if (existingKey != null)
            {
                bool hashMatches = KnowsOdkSHA256(sha256Hash, out OdkIndex verifyKeyId);
                if (hashMatches && verifyKeyId != keyId)
                {
                    var hashString = sha256Hash.ToHexString("");
                    Console.WriteLine($"ODK {keyId} with hash {hashString} is known as ODK {verifyKeyId}");
                    return false;
                }
                else if (hashMatches && OdkStorage[keyId].HasKeyData)
                {
                    // Duplicate key, already loaded
                    Console.WriteLine($"ODK {keyId} is already loaded");
                    return false;
                }
                else if (!hashMatches)
                {
                    Console.WriteLine($"ODK {keyId} does not match expected hash");
                    return false;
                }

                OdkStorage[keyId].SetKey(keyData);
                return true;
            }

            isNewKey = true;
            OdkStorage[keyId] = new DurangoKeyEntry(KeyType.Odk, keyData);
            return true;
        }

        public static bool LoadSignKey(string desiredKeyName, byte[] keyData, out bool isNewKey, out string keyName)
        {
            isNewKey = false;
            byte[] sha256Hash = HashUtils.ComputeSha256(keyData);

            bool hashMatches = KnowsSignKeySHA256(sha256Hash, out keyName);
            if (hashMatches && SignkeyStorage[keyName].HasKeyData)
            {
                // Duplicate key, already loaded
                Console.WriteLine($"SignKey {keyName} is already loaded");
                return false;
            }
            else if (hashMatches)
            {
                SignkeyStorage[keyName].SetKey(keyData);
                return true;
            }

            // New key, using user-set keyname
            isNewKey = true;
            SignkeyStorage[desiredKeyName] = new DurangoKeyEntry(KeyType.Odk, keyData);
            return true;
        }

        public static bool LoadKey(KeyType keyType, string keyFilePath)
        {
            if (keyFilePath == String.Empty)
                return false;

            var success = false;
            var isNewKey = false;
            var keyName = String.Empty;
            var filename = Path.GetFileNameWithoutExtension(keyFilePath);
            var keyBytes = File.ReadAllBytes(keyFilePath);

            switch(keyType)
            {
                case KeyType.XvdSigningKey:
                    success = LoadSignKey(filename, keyBytes, out isNewKey, out keyName);
                    break;
                case KeyType.Odk:
                    success = GetOdkIndexFromString(filename, out OdkIndex odkIndex);
                    if (!success)
                    {
                        Console.WriteLine($"Could not get OdkIndex from filename: {filename}");
                        break;
                    }
                    success = LoadOdkKey(odkIndex, keyBytes, out isNewKey);
                    break;
                case KeyType.Cik:
                    var keyCount = LoadCikKeys(keyBytes, out Guid[] loadedCiks);
                    success = keyCount > 0;
                    break;
                default:
                    throw new InvalidOperationException("Invalid KeyType supplied");
            }

            return success;
        }

        public static int LoadKeysRecursive(string basePath)
        {
            int foundCount = 0;
            foreach (KeyType keyType in Enum.GetValues(typeof(KeyType)))
            {
                var keyDirectory = Path.Combine(basePath, keyType.ToString());
                if (!Directory.Exists(keyDirectory))
                {
                    Console.WriteLine($"Key directory \"{keyDirectory}\" was not found!");
                    continue;
                }

                var keyFiles = Directory.GetFiles(keyDirectory);
                foreach (var keyFilePath in keyFiles)
                {
                    if (LoadKey(keyType, keyFilePath))
                    {
                        Console.WriteLine($"Loaded key {keyType} from {keyFilePath}");
                        foundCount++;
                    }
                    else
                        Console.WriteLine($"Unable to load key from \"{keyFilePath}\"");
                }
            }
            return foundCount;
        }

        public static DurangoKeyEntry GetSignkeyByName(string keyName)
        {
            return SignkeyStorage.ContainsKey(keyName) ? SignkeyStorage[keyName] : null;
        }

        public static DurangoKeyEntry GetCikByGuid(Guid keyId)
        {
            return CikStorage.ContainsKey(keyId) ? CikStorage[keyId] : null;
        }

        public static DurangoKeyEntry GetOdkById(OdkIndex keyId)
        {
            return OdkStorage.ContainsKey(keyId) ? OdkStorage[keyId] : null;
        }

        public static bool IsSignkeyLoaded(string keyName)
        {
            var key = GetSignkeyByName(keyName);
            return (key != null && key.HasKeyData);
        }

        public static bool IsCikLoaded(Guid keyId)
        {
            var key = GetCikByGuid(keyId);
            return (key != null && key.HasKeyData);
        }

        public static bool IsOdkLoaded(OdkIndex keyId)
        {
            var key = GetOdkById(keyId);
            return (key != null && key.HasKeyData);
        }
    }
}