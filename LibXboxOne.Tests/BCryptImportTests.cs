using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Xunit;

namespace LibXboxOne.Tests
{
    public static class BCryptRsaHelper
    {
        static async Task<RSAParameters> GetRsa(int keyStrength, string keyIdentifier)
        {
            var blob = await ResourcesProvider.GetBytesAsync($"RSA_{keyStrength}_{keyIdentifier}.bin",
                                                ResourceType.RsaKeys);
            var rsaParams = BCryptRsaImport.BlobToParameters(blob, out int resultBitLength, out bool isPrivate);
            if (keyStrength != resultBitLength)
                throw new InvalidDataException("Desired keyStrength does not match parsed data");
            
            return rsaParams;
        }

        public static Task<RSAParameters> GetRsaPublic(int keyStrength)
        {
            return GetRsa(keyStrength, "RSAPUBLICBLOB");
        }

        public static Task<RSAParameters> GetRsaPrivate(int keyStrength)
        {
            return GetRsa(keyStrength, "RSAPRIVATEBLOB");
        }

        public static Task<RSAParameters> GetRsaFullPrivate(int keyStrength)
        {
            return GetRsa(keyStrength, "RSAFULLPRIVATEBLOB");
        }

        public static Task<RSAParameters> GetRsaPublicCAPI(int keyStrength)
        {
            return GetRsa(keyStrength, "CAPIPUBLICBLOB");
        }

        public static Task<RSAParameters> GetRsaPrivateCAPI(int keyStrength)
        {
            return GetRsa(keyStrength, "CAPIPRIVATEBLOB");
        }
    }

    public class BCryptImportTests
    {
        [Theory]
        [InlineData(512)]
        [InlineData(1024)]
        [InlineData(2048)]
        [InlineData(3072)]
        [InlineData(4096)]
        public async void ImportRsaPublicBlob(int keyStrength)
        {
            RSAParameters rsaParams = await BCryptRsaHelper.GetRsaPublic(keyStrength);

            Assert.NotEmpty(rsaParams.Exponent);
            Assert.NotEmpty(rsaParams.Modulus);
        }

        [Theory]
        [InlineData(512)]
        [InlineData(1024)]
        [InlineData(2048)]
        [InlineData(3072)]
        [InlineData(4096)]
        public async void ImportRsaPrivateBlob(int keyStrength)
        {
            RSAParameters rsaParams = await BCryptRsaHelper.GetRsaPrivate(keyStrength);
            
            Assert.NotEmpty(rsaParams.Exponent);
            Assert.NotEmpty(rsaParams.Modulus);

            Assert.NotEmpty(rsaParams.P);
            Assert.NotEmpty(rsaParams.Q);
        }

        [Theory]
        [InlineData(512)]
        [InlineData(1024)]
        [InlineData(2048)]
        [InlineData(3072)]
        [InlineData(4096)]
        public async void ImportRsaFullPrivateBlob(int keyStrength)
        {
            RSAParameters rsaParams = await BCryptRsaHelper.GetRsaFullPrivate(keyStrength);
            
            Assert.NotEmpty(rsaParams.Exponent);
            Assert.NotEmpty(rsaParams.Modulus);

            Assert.NotEmpty(rsaParams.P);
            Assert.NotEmpty(rsaParams.Q);

            Assert.NotEmpty(rsaParams.DP);
            Assert.NotEmpty(rsaParams.DQ);
            Assert.NotEmpty(rsaParams.InverseQ);
            Assert.NotEmpty(rsaParams.D);
        }

        [Theory]
        [InlineData(512)]
        [InlineData(1024)]
        [InlineData(2048)]
        [InlineData(3072)]
        [InlineData(4096)]
        public async void ImportAndComparePrivateBlobs(int keyStrength)
        {
            RSAParameters rsaParams = await BCryptRsaHelper.GetRsaPrivate(keyStrength);
            RSAParameters rsaParamsFull = await BCryptRsaHelper.GetRsaFullPrivate(keyStrength);
            
            Assert.NotEmpty(rsaParams.Exponent);
            Assert.NotEmpty(rsaParams.Modulus);

            Assert.Equal(rsaParams.Exponent, rsaParamsFull.Exponent);
            Assert.Equal(rsaParams.Modulus, rsaParamsFull.Modulus);

            Assert.Equal(rsaParams.P, rsaParamsFull.P);
            Assert.Equal(rsaParams.Q, rsaParamsFull.Q);
        }

        [Theory]
        [InlineData(512)]
        [InlineData(1024)]
        [InlineData(2048)]
        [InlineData(3072)]
        [InlineData(4096)]
        public async void ImportAndComparePublicBlobs(int keyStrength)
        {
            RSAParameters rsaParamsPublic = await BCryptRsaHelper.GetRsaPublic(keyStrength);
            RSAParameters rsaParamsFullprivate = await BCryptRsaHelper.GetRsaFullPrivate(keyStrength);
            
            Assert.NotEmpty(rsaParamsPublic.Exponent);
            Assert.NotEmpty(rsaParamsPublic.Modulus);

            Assert.Equal(rsaParamsPublic.Exponent, rsaParamsFullprivate.Exponent);
            Assert.Equal(rsaParamsPublic.Modulus, rsaParamsFullprivate.Modulus);
        }

        [Theory]
        [InlineData(512)]
        [InlineData(1024)]
        [InlineData(2048)]
        [InlineData(3072)]
        [InlineData(4096)]
        public void ImportInvalidPublicBlobCAPI(int keyStrength)
        {
            Assert.ThrowsAsync<InvalidDataException>(async () =>
                await BCryptRsaHelper.GetRsaPublicCAPI(keyStrength)
            );
        }

        [Theory]
        [InlineData(512)]
        [InlineData(1024)]
        [InlineData(2048)]
        [InlineData(3072)]
        [InlineData(4096)]
        public void ImportInvalidPrivateBlobCAPI(int keyStrength)
        {
            Assert.ThrowsAsync<InvalidDataException>(async () =>
                await BCryptRsaHelper.GetRsaPrivateCAPI(keyStrength)
            );
        }
    }
}