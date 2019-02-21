using System;
using System.IO;
using System.Security.Cryptography;

namespace LibXboxOne
{
    public class AesXts
    {
        const int AES_BLOCK_SIZE = 0x10;
        readonly byte[] TweakAesKey;
        readonly byte[] DataAesKey;
        readonly byte[] InitialCounterBytes;

        readonly AesCipher CounterCipher;
        readonly AesCipher DataCipher;

        public AesXts(byte[] tweakAesKey, byte[] dataAesKey, byte[] initialCounterBytes)
        {
            TweakAesKey = tweakAesKey;
            DataAesKey = dataAesKey;
            InitialCounterBytes = initialCounterBytes;

            CounterCipher = new AesCipher(TweakAesKey);
            DataCipher = new AesCipher(DataAesKey);
        }

        public static byte[] MultiplyTweak(byte[] tweak)
        {
            byte dl = 0;
            var newTweak = new byte[0x10];

            for (int i = 0; i < 0x10; i++)
            {
                byte cl = tweak[i];
                byte al = cl;
                al = (byte)(al + al);
                al = (byte)(al | dl);
                dl = cl;
                newTweak[i] = al;
                dl = (byte)(dl >> 7);
            }
            if (dl != 0)
                newTweak[0] = (byte)(newTweak[0] ^ 0x87);
            return newTweak;
        }
        public byte[] CryptData(bool encrypt, byte[] data, int dataUnit)
        {
            int blocks = data.Length / AES_BLOCK_SIZE;
            var newData = new byte[data.Length];
            byte[] encryptedTweak = new byte[0x10];
            byte[] tweak = InitialCounterBytes;

            // Update tweak with data unit number
            Array.Copy(BitConverter.GetBytes(dataUnit), tweak, 4);

            // Encrypt tweak
            CounterCipher.EncryptBlock(tweak, 0, tweak.Length, encryptedTweak, 0);

            for (int i = 0; i < blocks; i++)
            {
                // Encrypt data, using encrypted tweak as IV
                byte[] crypted = CryptBlock(encrypt, data, i * AES_BLOCK_SIZE, AES_BLOCK_SIZE, encryptedTweak);
                Array.Copy(crypted, 0, newData, i * AES_BLOCK_SIZE, AES_BLOCK_SIZE);

                encryptedTweak = MultiplyTweak(encryptedTweak);
            }
            return newData;
        }

        byte[] CryptBlock(bool encrypt, byte[] data, int dataOffset, int dataLength, byte[] encryptedTweak)
        {
            var newData = new byte[dataLength];

            for (int i = 0; i < dataLength; i++)
            {
                newData[i] = (byte)(data[dataOffset + i] ^ encryptedTweak[i % encryptedTweak.Length]);
            }

            var cryptData = new byte[dataLength];

            if(encrypt)
                DataCipher.EncryptBlock(newData, 0, dataLength, cryptData, 0);
            else
                DataCipher.DecryptBlock(newData, 0, dataLength, cryptData, 0);

            for (int i = 0; i < dataLength; i++)
            {
                cryptData[i] = (byte)(cryptData[i] ^ encryptedTweak[i % encryptedTweak.Length]);
            }

            return cryptData;
        }
    }
}