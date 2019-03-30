using System.IO;
using Xunit;

namespace LibXboxOne.Tests
{
    public class XvdFileTests
    {
        [Fact(Skip="Relies on xvd data blob")]
        public void Dev_Signed_ValidHash_Test()
        {
            using (var file = new XvdFile(@"F:\XBone\XVDs\TestXVDs\xvd1"))
            {
                Assert.True(file.Load());
                /*
                if(XvdFile.SignKeyLoaded)
                    Assert.True(file.Header.IsSignedWithRedKey);
                */
                Assert.True(file.IsEncrypted);
                Assert.True(file.IsDataIntegrityEnabled);
                Assert.True(file.HashTreeValid);
                Assert.True(file.DataHashTreeValid);
            }
        }

        [Fact(Skip="Relies on xvd data blob")]
        public void Dev_Signed_InvalidHashTree_Test()
        {
            using (var file = new XvdFile(@"F:\XBone\XVDs\TestXVDs\xvd1_brokehash"))
            {
                Assert.True(file.Load());
                /*
                if (XvdFile.SignKeyLoaded)
                    Assert.True(file.Header.IsSignedWithRedKey);
                */
                Assert.True(file.IsEncrypted);
                Assert.True(file.IsDataIntegrityEnabled);
                Assert.False(file.HashTreeValid);
            }
        }

        [Fact(Skip="Relies on xvd data blob")]
        public void Dev_Signed_InvalidDataHashTree_Test()
        {
            using (var file = new XvdFile(@"F:\XBone\XVDs\TestXVDs\xvd1_brokedatahash"))
            {
                Assert.True(file.Load());
                /*
                if (XvdFile.SignKeyLoaded)
                    Assert.True(file.Header.IsSignedWithRedKey);
                */
                Assert.True(file.IsEncrypted);
                Assert.True(file.IsDataIntegrityEnabled);
                Assert.True(file.HashTreeValid);
                Assert.False(file.DataHashTreeValid);
            }
        }

        [Fact(Skip="Relies on xvd data blob")]
        public void Dev_Signed_XVC_Decrypt_Test()
        {
            const string dest = @"F:\XBone\XVDs\TestXVDs\xvd1_decrypted_temp";
            const string fileToCompare = @"F:\XBone\XVDs\TestXVDs\xvd1_decrypted";
            if (File.Exists(dest))
                File.Delete(dest);

            File.Copy(@"F:\XBone\XVDs\TestXVDs\xvd1", dest);
            using (var file = new XvdFile(dest))
            {
                Assert.True(file.Load());
                /*
                Assert.True(file.Header.IsSignedWithRedKey);
                */
                Assert.True(file.IsEncrypted);
                Assert.True(file.IsDataIntegrityEnabled);
                Assert.True(file.HashTreeValid);
                Assert.True(file.DataHashTreeValid);
                Assert.True(file.Decrypt());
                Assert.False(file.IsEncrypted);
                ulong[] invalid = file.VerifyDataHashTree();
                Assert.True(invalid.Length == 0);
                Assert.True(file.VerifyHashTree());

                byte[] ntfsString = file.ReadBytes(0x87003, 4);
                byte[] expectedString = { 0x4E, 0x54, 0x46, 0x53 };
                Assert.True(ntfsString.IsEqualTo(expectedString));
            }

            byte[] generatedHash;
            using (FileStream stream = File.OpenRead(dest))
            {
                generatedHash = HashUtils.ComputeSha256(stream);
            }

            File.Delete(dest);

            byte[] expectedHash;
            using (FileStream stream = File.OpenRead(fileToCompare))
            {
                expectedHash = HashUtils.ComputeSha256(stream);
            }

            Assert.True(generatedHash.IsEqualTo(expectedHash));
        }

        [Fact(Skip="Relies on xvd data blob")]
        public void Dev_Signed_XVC_Encrypt_Test()
        {
            const string dest = @"F:\XBone\XVDs\TestXVDs\xvd1_encrypted_temp";
            const string fileToCompare = @"F:\XBone\XVDs\TestXVDs\xvd1";
            if (File.Exists(dest))
                File.Delete(dest);

            File.Copy(@"F:\XBone\XVDs\TestXVDs\xvd1_decrypted", dest);
            using (var file = new XvdFile(dest))
            {
                Assert.True(file.Load());
                Assert.False(file.IsEncrypted);
                Assert.True(file.IsDataIntegrityEnabled);
                Assert.True(file.HashTreeValid);
                Assert.True(file.DataHashTreeValid);
                // Assert.True(file.Encrypt());
                Assert.True(file.IsEncrypted);

                ulong[] invalid = file.VerifyDataHashTree();
                Assert.True(invalid.Length == 0);
                Assert.True(file.VerifyHashTree());
            }

            byte[] generatedHash;
            using (FileStream stream = File.OpenRead(dest))
            {
                generatedHash = HashUtils.ComputeSha256(stream);
            }

            File.Delete(dest);

            byte[] expectedHash;
            using (FileStream stream = File.OpenRead(fileToCompare))
            {
                expectedHash = HashUtils.ComputeSha256(stream);
            }

            Assert.True(generatedHash.IsEqualTo(expectedHash));
        }

        [Fact(Skip="Relies on xvd data blob")]
        public void Unsigned_XVD_Decrypt_Test()
        {
            const string dest = @"F:\XBone\XVDs\TestXVDs\xvd2_decrypted_temp";
            const string fileToCompare = @"F:\XBone\XVDs\TestXVDs\xvd2_decrypted";
            if (File.Exists(dest))
                File.Delete(dest);

            File.Copy(@"F:\XBone\XVDs\TestXVDs\xvd2", dest);
            using (var file = new XvdFile(dest))
            {
                Assert.True(file.Load());
                /*
                Assert.True(file.Header.IsSignedWithRedKey);
                */
                Assert.True(file.IsEncrypted);
                Assert.True(file.IsDataIntegrityEnabled);
                Assert.True(file.HashTreeValid);
                Assert.True(file.DataHashTreeValid);
                Assert.True(file.Decrypt());
                Assert.False(file.IsEncrypted);

                ulong[] invalid = file.VerifyDataHashTree();
                Assert.True(invalid.Length == 0);
                Assert.True(file.VerifyHashTree());

                byte[] ntfsString = file.ReadBytes(0x75003, 4);
                byte[] expectedString = { 0x4E, 0x54, 0x46, 0x53 };
                Assert.True(ntfsString.IsEqualTo(expectedString));
            }

            byte[] generatedHash;
            using (FileStream stream = File.OpenRead(dest))
            {
                generatedHash = HashUtils.ComputeSha256(stream);
            }

            File.Delete(dest);

            byte[] expectedHash;
            using (FileStream stream = File.OpenRead(fileToCompare))
            {
                expectedHash = HashUtils.ComputeSha256(stream);
            }

            Assert.True(generatedHash.IsEqualTo(expectedHash));
        }

        [Fact(Skip="Relies on xvd data blob")]
        public void Unsigned_XVD_Encrypt_Test()
        {
            const string dest = @"F:\XBone\XVDs\TestXVDs\xvd2_encrypted_temp";
            const string fileToCompare = @"F:\XBone\XVDs\TestXVDs\xvd2";
            if (File.Exists(dest))
                File.Delete(dest);

            File.Copy(@"F:\XBone\XVDs\TestXVDs\xvd2_decrypted_orig_mod", dest); // modded with CIK used in xvd2
            using (var file = new XvdFile(dest))
            {
                Assert.True(file.Load());
                /*
                Assert.False(file.Header.IsSignedWithRedKey);
                */
                Assert.False(file.IsEncrypted);
                Assert.False(file.IsDataIntegrityEnabled);
                // Assert.True(file.Encrypt());
                Assert.True(file.IsEncrypted);

                // copy values from file being compared so the hashes match
                file.Header.PDUID = new byte[] {0xEA, 0xC8, 0xE2, 0x82, 0x2F, 0x58, 0x32, 0x4F, 0x92, 0x29, 0xE1, 0xAB, 0x6E, 0x8F, 0x91, 0x63};
                using (FileStream stream = File.OpenRead(fileToCompare))
                {
                    stream.Position = 0;
                    stream.Read(file.Header.Signature, 0, 0x200);
                }

                Assert.True(file.AddHashTree());

                ulong[] invalid = file.VerifyDataHashTree();
                Assert.True(invalid.Length == 0);
                Assert.True(file.VerifyHashTree());
            }

            byte[] generatedHash;
            using (FileStream stream = File.OpenRead(dest))
            {
                generatedHash = HashUtils.ComputeSha256(stream);
            }

            File.Delete(dest);

            byte[] expectedHash;
            using (FileStream stream = File.OpenRead(fileToCompare))
            {
                expectedHash = HashUtils.ComputeSha256(stream);
            }

            Assert.True(generatedHash.IsEqualTo(expectedHash));
        }

        /*
        [Fact(Skip="Relies on xvd data blob")]
        public void XvdSign_Key_Extract()
        {
            var sdkVersions = new List<string> { "XDK_11785" };
            var versionsTextFile = @"F:\Xbone\Research\xdk_versions.txt";
            if (File.Exists(versionsTextFile))
            {
                string[] sdkVersionArr = File.ReadAllLines(versionsTextFile);
                foreach (string ver in sdkVersionArr)
                    if (!sdkVersions.Contains(ver.ToUpper()))
                        sdkVersions.Add(ver.ToUpper());
            }

            foreach (string ver in sdkVersions)
            {
                string path = Path.Combine(@"F:\Xbone\Research\", ver);
                string binPath = Path.Combine(path, "bin");
                if (Directory.Exists(binPath))
                    path = binPath;
                if (!File.Exists(Path.Combine(path, "xvdsign.exe")))
                    continue;

                XvdFile.CikFileLoaded = false;
                XvdFile.OdkKeyLoaded = false;
                XvdFile.SignKeyLoaded = false;
                XvdFile.LoadKeysFromSdk(path);
                Assert.True(XvdFile.CikFileLoaded && XvdFile.OdkKeyLoaded && XvdFile.SignKeyLoaded);
            }

            XvdFile.CikFileLoaded = false;
            XvdFile.OdkKeyLoaded = false;
            XvdFile.SignKeyLoaded = false;
        }
        */
    }
}
