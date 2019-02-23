using System;
using System.IO;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

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

        public static bool SignData(byte[] key, string keyType, byte[] data, out byte[] signature) // keyType = RSAFULLPRIVATEBLOB, RSAPRIVATEBLOB, RSAPUBLICBLOB
        {
            if (keyType != "RSAFULLPRIVATEBLOB")
                throw new CryptographicException("Only RSAFULLPRIVATEBLOB can be used for signing");

            var rsaKey = DotNetUtilities.GetRsaKeyPair(BCryptRsaImport.BlobToParameters(key)).Private;
            ISigner s = SignerUtilities.GetSigner("SHA256withRSA/PSS");

            s.Init(true, new ParametersWithRandom(rsaKey));
            s.BlockUpdate(data, 0, data.Length);

            signature = s.GenerateSignature();
            return true;
        }

        public static bool VerifySignature(byte[] key, string keyType, byte[] signature, byte[] data) // keyType = RSAFULLPRIVATEBLOB, RSAPRIVATEBLOB, RSAPUBLICBLOB
        {
            var rsaKey = DotNetUtilities.GetRsaPublicKey(BCryptRsaImport.BlobToParameters(key));
            ISigner s = SignerUtilities.GetSigner("SHA256withRSA/PSS");

            s.Init(false, new ParametersWithRandom(rsaKey));
            s.BlockUpdate(data, 0, data.Length);

            return s.VerifySignature(signature);
        }
    }
}