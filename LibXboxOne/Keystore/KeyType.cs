using System;

namespace LibXboxOne.Keystore
{
    public enum KeyType
    {
        XvdSigningKey,
        OdkKey, // Offline Distribution Key, AES256 (32 bytes) key
        CikKey // Content Instance Key, Guid (16 bytes) + AES256 (32 bytes) key
    }
}