using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace LibXboxOne
{
    public enum BCRYPT_RSABLOB_MAGIC : uint
    {
        // The key is an RSA public key. 
        RSAPUBLIC = 0x31415352,
        // The key is an RSA private key. 
        RSAPRIVATE = 0x32415352,
        // The key is a full RSA private key. 
        RSAFULLPRIVATE = 0x33415352
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct BCRYPT_RSAKEY_BLOB
    {
        public BCRYPT_RSABLOB_MAGIC Magic;
        public uint BitLength;
        public uint cbPublicExp;
        public uint cbModulus;
        public uint cbPrime1;
        public uint cbPrime2;
    };

    public sealed class BCryptRsaImport
    {
        public static RSAParameters BlobToParameters(byte[] blobData)
        {
            var parameters = new RSAParameters();
            var reader = new BinaryReader(new MemoryStream(blobData));

            BCRYPT_RSAKEY_BLOB header = reader.ReadStruct<BCRYPT_RSAKEY_BLOB>();
            if (header.Magic != BCRYPT_RSABLOB_MAGIC.RSAPUBLIC &&
                header.Magic != BCRYPT_RSABLOB_MAGIC.RSAPRIVATE &&
                header.Magic != BCRYPT_RSABLOB_MAGIC.RSAFULLPRIVATE)
            {
                throw new InvalidDataException("Unexpected RSA keyblob");
            }

            parameters.Exponent = reader.ReadBytes((int)header.cbPublicExp);
            parameters.Modulus = reader.ReadBytes((int)header.cbModulus);

            if (header.Magic != BCRYPT_RSABLOB_MAGIC.RSAPUBLIC)
            {
                // RSAPRIVATEBLOB
                parameters.P = reader.ReadBytes((int)header.cbPrime1);
                parameters.Q = reader.ReadBytes((int)header.cbPrime2);

                if (header.Magic != BCRYPT_RSABLOB_MAGIC.RSAPRIVATE)
                {
                    // RSAFULLPRIVATEBLOB
                    parameters.DP = reader.ReadBytes((int)header.cbPrime1);
                    parameters.DQ = reader.ReadBytes((int)header.cbPrime2);
                    parameters.InverseQ = reader.ReadBytes((int)header.cbPrime1);
                    parameters.D = reader.ReadBytes((int)header.cbModulus);
                }
            }

            return parameters;
        }
    }
}