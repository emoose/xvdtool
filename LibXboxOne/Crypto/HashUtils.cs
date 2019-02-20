using System.Security.Cryptography;

namespace LibXboxOne
{
    public static class HashUtils
    {
        public static byte[] ComputeSha256(byte[] data)
        {
            var gen = SHA256.Create();
            return gen.ComputeHash(data);
        }
    }
}