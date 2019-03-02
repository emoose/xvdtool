using System;
using System.Collections.Generic;

namespace LibXboxOne.Keystore
{
    public static class DurangoKeys
    {
        public static readonly Dictionary<string,IKeyEntry> KnownKeys = new Dictionary<string, IKeyEntry>(){
            // Cik keys
            {"TestCIK", new CikKeyEntry(new Guid("33EC8436-5A0E-4F0D-B1CE-3F29C3955039"), sha256Hash: "6786C11B788ED5CCE3C7695425CB82970347180650893D1B5613B2EFB33F9F4E", dataSize: 0x20)},
            {"Unknown0CIK", new CikKeyEntry(new Guid("F0522B7C-7FC1-D806-43E3-68A5DAAB06DA"), sha256Hash: "B767CE5F83224375E663A1E01044EA05E8022C033D96BED952475D87F0566642", dataSize: 0x20)},
            
            // Odk keys
            {"RedODK", new OdkKeyEntry(2, sha256Hash: "CA37132DFB4B811506AE4DC45F45970FED8FE5E58C1BACB259F1B96145B0EBC6", dataSize: 0x20)},
            
            // Xvd signing keys
            {"RedXvdPrivateKey", new XvdSignKeyEntry(4096, privateKey: true, sha256Hash: "8E2B60377006D87EE850334C42FC200081386A838C65D96D1EA52032AA9628C5", dataSize: 0x91B)},
            {"GreenXvdPublicKey", new XvdSignKeyEntry(4096, privateKey: false, sha256Hash: "618C5FB1193040AF8BC1C0199B850B4B5C42E43CE388129180284E4EF0B18082", dataSize: 0x21B)},
            {"GreenGamesPublicKey", new XvdSignKeyEntry(4096, privateKey: false, sha256Hash: "183F0AE05431E4AD91554E88946967C872997227DBE6C85116F5FD2FD2D1229E", dataSize: 0x21B)},
        };
    }
}