using System;

namespace LibXboxOne.Keys
{
    public enum KeyType
    {
        XvdSigningKey,
        Odk, // Offline Distribution Key, AES256 (32 bytes) key
        Cik // Content Instance Key, Guid (16 bytes) + AES256 (32 bytes) key
    }
}