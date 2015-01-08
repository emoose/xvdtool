using System.IO;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibXboxOne.Tests
{
    [TestClass]
    public class XvdFileTests
    {
        [TestMethod]
        public void Dev_Signed_ValidHash_Test()
        {
            using (var file = new XvdFile(@"F:\XBone\XVDs\TestXVDs\xvd1"))
            {
                Assert.IsTrue(file.Load());
                if(XvdFile.SignKeyLoaded)
                    Assert.IsTrue(file.Header.IsSignedWithRedKey);
                Assert.IsTrue(file.IsEncrypted);
                Assert.IsTrue(file.IsDataIntegrityEnabled);
                Assert.IsTrue(file.HashTreeValid);
                Assert.IsTrue(file.DataHashTreeValid);
            }
        }

        [TestMethod]
        public void Dev_Signed_InvalidHashTree_Test()
        {
            using (var file = new XvdFile(@"F:\XBone\XVDs\TestXVDs\xvd1_brokehash"))
            {
                Assert.IsTrue(file.Load());
                if (XvdFile.SignKeyLoaded)
                    Assert.IsTrue(file.Header.IsSignedWithRedKey);
                Assert.IsTrue(file.IsEncrypted);
                Assert.IsTrue(file.IsDataIntegrityEnabled);
                Assert.IsFalse(file.HashTreeValid);
            }
        }

        [TestMethod]
        public void Dev_Signed_InvalidDataHashTree_Test()
        {
            using (var file = new XvdFile(@"F:\XBone\XVDs\TestXVDs\xvd1_brokedatahash"))
            {
                Assert.IsTrue(file.Load());
                if (XvdFile.SignKeyLoaded)
                    Assert.IsTrue(file.Header.IsSignedWithRedKey);
                Assert.IsTrue(file.IsEncrypted);
                Assert.IsTrue(file.IsDataIntegrityEnabled);
                Assert.IsTrue(file.HashTreeValid);
                Assert.IsFalse(file.DataHashTreeValid);
            }
        }

        [TestMethod]
        public void Dev_Signed_XVC_Decrypt_Test()
        {
            const string dest = @"F:\XBone\XVDs\TestXVDs\xvd1_decrypted_temp";
            const string fileToCompare = @"F:\XBone\XVDs\TestXVDs\xvd1_decrypted";
            if (File.Exists(dest))
                File.Delete(dest);

            File.Copy(@"F:\XBone\XVDs\TestXVDs\xvd1", dest);
            using (var file = new XvdFile(dest))
            {
                Assert.IsTrue(file.Load());
                Assert.IsTrue(file.Header.IsSignedWithRedKey);
                Assert.IsTrue(file.IsEncrypted);
                Assert.IsTrue(file.IsDataIntegrityEnabled);
                Assert.IsTrue(file.HashTreeValid);
                Assert.IsTrue(file.DataHashTreeValid);
                Assert.IsTrue(file.Decrypt());
                Assert.IsFalse(file.IsEncrypted);
                int[] invalid = file.VerifyDataHashTree();
                Assert.IsTrue(invalid.Length == 0);
                Assert.IsTrue(file.VerifyHashTree());

                byte[] ntfsString = file.Read(0x87003, 4);
                byte[] expectedString = { 0x4E, 0x54, 0x46, 0x53 };
                Assert.IsTrue(ntfsString.IsEqualTo(expectedString));
            }

            byte[] generatedHash;
            using (FileStream stream = File.OpenRead(dest))
            {
                generatedHash = new SHA256Managed().ComputeHash(stream);
            }

            File.Delete(dest);

            byte[] expectedHash;
            using (FileStream stream = File.OpenRead(fileToCompare))
            {
                expectedHash = new SHA256Managed().ComputeHash(stream);
            }

            Assert.IsTrue(generatedHash.IsEqualTo(expectedHash));
        }

        [TestMethod]
        public void Dev_Signed_XVC_Encrypt_Test()
        {
            const string dest = @"F:\XBone\XVDs\TestXVDs\xvd1_encrypted_temp";
            const string fileToCompare = @"F:\XBone\XVDs\TestXVDs\xvd1";
            if (File.Exists(dest))
                File.Delete(dest);

            File.Copy(@"F:\XBone\XVDs\TestXVDs\xvd1_decrypted", dest);
            using (var file = new XvdFile(dest))
            {
                Assert.IsTrue(file.Load());
                Assert.IsFalse(file.IsEncrypted);
                Assert.IsTrue(file.IsDataIntegrityEnabled);
                Assert.IsTrue(file.HashTreeValid);
                Assert.IsTrue(file.DataHashTreeValid);
                Assert.IsTrue(file.Encrypt());
                Assert.IsTrue(file.IsEncrypted);

                int[] invalid = file.VerifyDataHashTree();
                Assert.IsTrue(invalid.Length == 0);
                Assert.IsTrue(file.VerifyHashTree());
            }

            byte[] generatedHash;
            using (FileStream stream = File.OpenRead(dest))
            {
                generatedHash = new SHA256Managed().ComputeHash(stream);
            }

            File.Delete(dest);

            byte[] expectedHash;
            using (FileStream stream = File.OpenRead(fileToCompare))
            {
                expectedHash = new SHA256Managed().ComputeHash(stream);
            }

            Assert.IsTrue(generatedHash.IsEqualTo(expectedHash));
        }

        [TestMethod]
        public void Unsigned_XVD_Decrypt_Test()
        {
            const string dest = @"F:\XBone\XVDs\TestXVDs\xvd2_decrypted_temp";
            const string fileToCompare = @"F:\XBone\XVDs\TestXVDs\xvd2_decrypted";
            if (File.Exists(dest))
                File.Delete(dest);

            File.Copy(@"F:\XBone\XVDs\TestXVDs\xvd2", dest);
            using (var file = new XvdFile(dest))
            {
                Assert.IsTrue(file.Load());
                Assert.IsTrue(file.Header.IsSignedWithRedKey);
                Assert.IsTrue(file.IsEncrypted);
                Assert.IsTrue(file.IsDataIntegrityEnabled);
                Assert.IsTrue(file.HashTreeValid);
                Assert.IsTrue(file.DataHashTreeValid);
                Assert.IsTrue(file.Decrypt());
                Assert.IsFalse(file.IsEncrypted);

                int[] invalid = file.VerifyDataHashTree();
                Assert.IsTrue(invalid.Length == 0);
                Assert.IsTrue(file.VerifyHashTree());

                byte[] ntfsString = file.Read(0x75003, 4);
                byte[] expectedString = { 0x4E, 0x54, 0x46, 0x53 };
                Assert.IsTrue(ntfsString.IsEqualTo(expectedString));
            }

            byte[] generatedHash;
            using (FileStream stream = File.OpenRead(dest))
            {
                generatedHash = new SHA256Managed().ComputeHash(stream);
            }

            File.Delete(dest);

            byte[] expectedHash;
            using (FileStream stream = File.OpenRead(fileToCompare))
            {
                expectedHash = new SHA256Managed().ComputeHash(stream);
            }

            Assert.IsTrue(generatedHash.IsEqualTo(expectedHash));
        }

        [TestMethod]
        public void Unsigned_XVD_Encrypt_Test()
        {
            const string dest = @"F:\XBone\XVDs\TestXVDs\xvd2_encrypted_temp";
            const string fileToCompare = @"F:\XBone\XVDs\TestXVDs\xvd2";
            if (File.Exists(dest))
                File.Delete(dest);

            File.Copy(@"F:\XBone\XVDs\TestXVDs\xvd2_decrypted_orig_mod", dest); // modded with CIK used in xvd2
            using (var file = new XvdFile(dest))
            {
                Assert.IsTrue(file.Load());
                Assert.IsFalse(file.Header.IsSignedWithRedKey);
                Assert.IsFalse(file.IsEncrypted);
                Assert.IsFalse(file.IsDataIntegrityEnabled);
                Assert.IsTrue(file.Encrypt());
                Assert.IsTrue(file.IsEncrypted);

                // copy values from file being compared so the hashes match
                file.Header.PDUID = new byte[] {0xEA, 0xC8, 0xE2, 0x82, 0x2F, 0x58, 0x32, 0x4F, 0x92, 0x29, 0xE1, 0xAB, 0x6E, 0x8F, 0x91, 0x63};
                using (FileStream stream = File.OpenRead(fileToCompare))
                {
                    stream.Position = 0;
                    stream.Read(file.Header.Signature, 0, 0x200);
                }

                Assert.IsTrue(file.AddHashTree());

                int[] invalid = file.VerifyDataHashTree();
                Assert.IsTrue(invalid.Length == 0);
                Assert.IsTrue(file.VerifyHashTree());
            }

            byte[] generatedHash;
            using (FileStream stream = File.OpenRead(dest))
            {
                generatedHash = new SHA256Managed().ComputeHash(stream);
            }

            File.Delete(dest);

            byte[] expectedHash;
            using (FileStream stream = File.OpenRead(fileToCompare))
            {
                expectedHash = new SHA256Managed().ComputeHash(stream);
            }

            Assert.IsTrue(generatedHash.IsEqualTo(expectedHash));
        }
    }
}
