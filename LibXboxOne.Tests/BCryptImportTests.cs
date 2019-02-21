using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Xunit;

namespace LibXboxOne.Tests
{
    public class BCryptImportTests
    {
        async Task<RSAParameters> GetRsaPublic(int keyStrength)
        {
            var blob = await ResourcesProvider.GetBytesAsync($"RSA_{keyStrength}_RSAPUBLICBLOB.bin",
                                                    ResourceType.RsaKeys);
            return BCryptRsaImport.BlobToParameters(blob);
        }

        async Task<RSAParameters> GetRsaPrivate(int keyStrength)
        {
            var blob = await ResourcesProvider.GetBytesAsync($"RSA_{keyStrength}_RSAPRIVATEBLOB.bin",
                                                    ResourceType.RsaKeys);
            return BCryptRsaImport.BlobToParameters(blob);
        }

        async Task<RSAParameters> GetRsaFullPrivate(int keyStrength)
        {
            var blob = await ResourcesProvider.GetBytesAsync($"RSA_{keyStrength}_RSAFULLPRIVATEBLOB.bin",
                                                    ResourceType.RsaKeys);
            return BCryptRsaImport.BlobToParameters(blob);
        }

        async Task<RSAParameters> GetRsaPublicCAPI(int keyStrength)
        {
            var blob = await ResourcesProvider.GetBytesAsync($"RSA_{keyStrength}_CAPIPUBLICBLOB.bin",
                                                    ResourceType.RsaKeys);
            return BCryptRsaImport.BlobToParameters(blob);
        }

        async Task<RSAParameters> GetRsaPrivateCAPI(int keyStrength)
        {
            var blob = await ResourcesProvider.GetBytesAsync($"RSA_{keyStrength}_CAPIPRIVATEBLOB.bin",
                                                    ResourceType.RsaKeys);
            return BCryptRsaImport.BlobToParameters(blob);
        }

        [Theory]
        [InlineData(512)]
        [InlineData(1024)]
        [InlineData(2048)]
        [InlineData(3072)]
        [InlineData(4096)]
        public async void ImportRsaPublicBlob(int keyStrength)
        {
            RSAParameters rsaParams = await GetRsaPublic(keyStrength);

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
            RSAParameters rsaParams = await GetRsaPrivate(keyStrength);
            
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
            RSAParameters rsaParams = await GetRsaFullPrivate(keyStrength);
            
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
            RSAParameters rsaParams = await GetRsaPrivate(keyStrength);
            RSAParameters rsaParamsFull = await GetRsaFullPrivate(keyStrength);
            
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
            RSAParameters rsaParamsPublic = await GetRsaPublic(keyStrength);
            RSAParameters rsaParamsFullprivate = await GetRsaFullPrivate(keyStrength);
            
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
                await GetRsaPublicCAPI(keyStrength)
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
                await GetRsaPrivateCAPI(keyStrength)
            );
        }
    }
}