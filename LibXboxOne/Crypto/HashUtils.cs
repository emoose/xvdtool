using System;
using System.IO;
using System.Security.Cryptography;

namespace LibXboxOne
{
    public static class HashUtils
    {
        public static byte[] ComputeSha256(byte[] data)
        {
            return ComputeSha256(data, 0, data.Length);
        }

        public static byte[] ComputeSha256(Stream stream)
        {
            return SHA256.Create().ComputeHash(stream);
        }

        public static byte[] ComputeSha256(byte[] data, int offset, int length)
        {
            return SHA256.Create().ComputeHash(data, offset, length);
        }

        public static byte[] ComputeSha1(byte[] data)
        {
            return ComputeSha1(data, 0, data.Length);
        }

        public static byte[] ComputeSha1(Stream stream)
        {
            return SHA1.Create().ComputeHash(stream);
        }

        public static byte[] ComputeSha1(byte[] data, int offset, int length)
        {
            return SHA1.Create().ComputeHash(data, offset, length);
        }

        public static uint SignHash(byte[] key, string keyType, byte[] hash, out byte[] signature) // keyType = RSAFULLPRIVATEBLOB, RSAPRIVATEBLOB, RSAPUBLICBLOB
        {
            IntPtr hProvider;
            IntPtr hKey;

            signature = null;

            uint result = Natives.NCryptOpenStorageProvider(out hProvider, "Microsoft Software Key Storage Provider", 0);
            if (result != 0)
                return result;

            result = Natives.NCryptImportKey(hProvider, IntPtr.Zero, keyType, IntPtr.Zero, out hKey, key, (uint)key.Length, 0);
            if (result != 0)
            {
                Natives.NCryptFreeObject(hProvider);
                return result;
            }

            var pss = new Natives.BCRYPT_PSS_PADDING_INFO("SHA256", 0x20);

            uint resultSigLength;

            result = Natives.NCryptSignHash(hKey, ref pss, hash, hash.Length, IntPtr.Zero, 0, out resultSigLength, 8);
            if (result == 0)
            {
                signature = new byte[resultSigLength];
                result = Natives.NCryptSignHash(hKey, ref pss, hash, hash.Length, signature, 0x200, out resultSigLength, 8);
            }

            Natives.NCryptFreeObject(hKey);
            Natives.NCryptFreeObject(hProvider);

            return result;
        }

        public static uint VerifySignature(byte[] key, string keyType, byte[] signature, byte[] hash) // keyType = RSAFULLPRIVATEBLOB, RSAPRIVATEBLOB, RSAPUBLICBLOB
        {
            IntPtr hProvider;
            IntPtr hKey;

            uint result = Natives.NCryptOpenStorageProvider(out hProvider, "Microsoft Software Key Storage Provider", 0);
            if (result != 0)
                return result;
            result = Natives.NCryptImportKey(hProvider, IntPtr.Zero, keyType, IntPtr.Zero, out hKey, key, (uint)key.Length, 0);
            if (result != 0)
            {
                Natives.NCryptFreeObject(hProvider);
                return result;
            }

            var pss = new Natives.BCRYPT_PSS_PADDING_INFO("SHA256", 0x20);

            result = Natives.NCryptVerifySignature(hKey, ref pss, hash, hash.Length, signature, signature.Length, 8);

            Natives.NCryptFreeObject(hKey);
            Natives.NCryptFreeObject(hProvider);

            return result;
        }
    }
}