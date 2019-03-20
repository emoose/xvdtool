using System;
using System.IO;
using Xunit;

namespace LibXboxOne.Tests
{
    public static class AesXtsData
    {
        public static byte[] WholeKey => new byte[]{
                0x82,0x21,0xd4,0xac,0xf4,0x31,0x9d,0x95,0xe2,0x8e,0x72,0x2e,0x82,0xa3,0xb5,0xe2,
                0x32,0xba,0x06,0xdd,0x96,0x86,0x15,0x91,0x3f,0x6b,0xf8,0x10,0xd0,0x89,0x16,0xa0};
        public static byte[] TweakKey => new byte[]{
                0x82,0x21,0xd4,0xac,0xf4,0x31,0x9d,0x95,0xe2,0x8e,0x72,0x2e,0x82,0xa3,0xb5,0xe2};
        
        public static byte[] DataKey => new byte[]{
                0x32,0xba,0x06,0xdd,0x96,0x86,0x15,0x91,0x3f,0x6b,0xf8,0x10,0xd0,0x89,0x16,0xa0};
        
        public static byte[] Tweak => new byte[]{
                0x00,0x00,0x00,0x00,0x01,0x00,0x00,0x00,0x34,0xFF,0x87,0x90,0xCA,0xDB,0x14,0x22};
        
        public static byte[] PlainData => ResourcesProvider.GetBytes("xts_plain.bin", ResourceType.DataBlobs);
        public static byte[] CipherData => ResourcesProvider.GetBytes("xts_cipher.bin", ResourceType.DataBlobs);

        public static uint HeaderId => 0x1;
    }

    public class AesXtsTests
    {
        [Fact]
        public void TestEncrypt()
        {
            var cipher = new AesXtsTransform(AesXtsData.Tweak, AesXtsData.DataKey, AesXtsData.TweakKey,
                                             encrypt: true);
            
            var plaintext = AesXtsData.PlainData;
            var ciphertext = AesXtsData.CipherData;
            byte[] result = new byte[0x3000];
            int transformedBytes = 0;

            for (int dataUnit=0; dataUnit < 3; dataUnit++)
            {
                transformedBytes += cipher.TransformDataUnit(plaintext, dataUnit * 0x1000, 0x1000,
                                         result, dataUnit * 0x1000, (ulong)dataUnit);
            }

            Assert.Equal(0x3000, transformedBytes);
            Assert.Equal(ciphertext, result);
            Assert.NotEqual(plaintext, result);
        }

        [Fact]
        public void TestDecrypt()
        {
            var cipher = new AesXtsTransform(AesXtsData.Tweak, AesXtsData.DataKey, AesXtsData.TweakKey,
                                             encrypt: false);
            
            var plaintext = AesXtsData.PlainData;
            var ciphertext = AesXtsData.CipherData;
            byte[] result = new byte[0x3000];
            int transformedBytes = 0;

            for (int dataUnit=0; dataUnit < 3; dataUnit++)
            {
                transformedBytes += cipher.TransformDataUnit(ciphertext, dataUnit * 0x1000, 0x1000,
                                         result, dataUnit * 0x1000, (ulong)dataUnit);
            }

            Assert.Equal(0x3000, transformedBytes);
            Assert.Equal(plaintext, result);
            Assert.NotEqual(ciphertext, result);
        }

        [Fact]
        public void TestEncryptFail()
        {
            var cipher = new AesXtsTransform(AesXtsData.Tweak, AesXtsData.DataKey, AesXtsData.TweakKey,
                                             encrypt: true);
            
            var plaintext = AesXtsData.PlainData;
            var ciphertext = AesXtsData.CipherData;
            byte[] result = new byte[0x3000];
            int transformedBytes = 0;

            for (int dataUnit=0; dataUnit < 3; dataUnit++)
            {
                transformedBytes += cipher.TransformDataUnit(plaintext, dataUnit * 0x1000, 0x1000,
                                         result, dataUnit * 0x1000,
                                         (ulong)dataUnit+1); // <- Invalid data unit
            }

            Assert.Equal(0x3000, transformedBytes);
            Assert.NotEqual(plaintext, result);
            Assert.NotEqual(ciphertext, result);
        }
    }
}