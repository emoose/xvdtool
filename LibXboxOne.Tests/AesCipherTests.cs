using System.Security.Cryptography;
using Xunit;

namespace LibXboxOne.Tests
{
    public static class AesChipherData
    {
        public static byte[] AesKey => new byte[]{
                0x58,0x51,0x28,0xbc,0xcb,0xdb,0x45,0x73,0xdb,0x92,0x18,0xc5,0x64,0x92,0x86,0x15,
                0xde,0xdb,0xc8,0x29,0x99,0x32,0x81,0xd9,0x77,0xa0,0xf9,0xc9,0x4b,0xbb,0x7f,0x62};
        public static byte[] PlaintextData => new byte[]{
                0x48,0x45,0x4c,0x4c,0x4f,0x2c,0x20,0x49,0x54,0x53,0x20,0x4d,0x45,0x2c,0x20,0x54,
                0x45,0x53,0x54,0x44,0x41,0x54,0x41,0x0a,0x01,0x11,0x21,0x31,0x41,0x51,0x61,0x71};
        
        public static byte[] EncryptedData => new byte[]{
                0x50,0x88,0xED,0x6D,0x2B,0xD6,0xDE,0xA9,0x23,0x45,0x29,0x97,0x8A,0xD1,0x8C,0xE9,
                0xED,0x65,0x53,0x0A,0xB9,0x72,0x46,0xF0,0xA7,0x49,0xE3,0x19,0x5D,0x13,0x15,0x82};
    }

    public class AesCipherTests
    {
        [Fact]
        public void TestAesEncryption()
        {
            var data = AesChipherData.PlaintextData;

            byte[] nullIv = new byte[16];
            var cipher = Aes.Create();
            cipher.Mode = CipherMode.ECB;
            cipher.Padding = PaddingMode.None;

            ICryptoTransform transform = cipher.CreateEncryptor(AesChipherData.AesKey, nullIv);
            transform.TransformBlock(data, 0, data.Length, data, 0);

            Assert.Equal(AesChipherData.EncryptedData, data);
        }

        [Fact]
        public void TestAesDecryption()
        {
            var data = AesChipherData.EncryptedData;

            byte[] nullIv = new byte[16];
            var cipher = Aes.Create();
            cipher.Mode = CipherMode.ECB;
            cipher.Padding = PaddingMode.None;

            ICryptoTransform transform = cipher.CreateDecryptor(AesChipherData.AesKey, nullIv);
            transform.TransformBlock(data, 0, data.Length, data, 0);

            Assert.Equal(AesChipherData.PlaintextData, data);
        }
    }
}