using System;
using System.IO;
using System.Security.Cryptography;

namespace LibXboxOne
{
    class AesXtsTransform : IDisposable
    {
        public int BlockSize;
        private readonly byte[] _tweakBytes;
        private readonly ICryptoTransform _tweakEncryptor;
        private readonly ICryptoTransform _dataTransform;
        private readonly SymmetricAlgorithm _symmetricAlgorithm;

        public AesXtsTransform(byte[] tweakBytes, byte[] dataAesKey, byte[] tweakAesKey, bool encrypt)
        {
            if (tweakBytes == null) throw new InvalidDataException("Tweak bytes not provided");
            if (dataAesKey == null) throw new InvalidDataException("Data AES key not provided");
            if (tweakAesKey == null) throw new InvalidDataException("Tweak AES key not provided");
            if (tweakBytes.Length != 16) throw new InvalidDataException("Tweak bytes not 16 bytes");
            if (dataAesKey.Length != 16) throw new InvalidDataException("Data AES key not 16 bytes");
            if (tweakAesKey.Length != 16) throw new InvalidDataException("Tweak AES not 16 bytes");
            
            _tweakBytes = tweakBytes;

            _symmetricAlgorithm = Aes.Create();
            _symmetricAlgorithm.Padding = PaddingMode.None;
            _symmetricAlgorithm.Mode = CipherMode.ECB;

            byte[] nullIv = new byte[16];
            _tweakEncryptor = _symmetricAlgorithm.CreateEncryptor(tweakAesKey, nullIv);
            _dataTransform = encrypt ? _symmetricAlgorithm.CreateEncryptor(dataAesKey, nullIv) :
                                        _symmetricAlgorithm.CreateDecryptor(dataAesKey, nullIv);

            BlockSize = _symmetricAlgorithm.BlockSize / 8;
        }

        internal static byte[] MultiplyTweak(byte[] tweak)
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

        public int TransformDataUnit(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset, uint dataUnit)
        {
            byte[] encryptedTweak = new byte[0x10];
            byte[] tweak = _tweakBytes;

            // Update tweak with data unit number
            Array.Copy(BitConverter.GetBytes(dataUnit), tweak, 4);

            // Encrypt tweak
            _tweakEncryptor.TransformBlock(tweak, 0, tweak.Length, encryptedTweak, 0);

            byte[] encryptedTweakOrig = new byte[0x10];
            Array.Copy(encryptedTweak, encryptedTweakOrig, 0x10);

            int blocks = inputCount / BlockSize;

            // Apply first part of tweak (input-tweak) to input buffer all at once
            for (int i = 0; i < blocks; i++)
            {
                for (int y = 0; y < BlockSize; y++)
                    outputBuffer[outputOffset + (i * BlockSize) + y] = (byte)(inputBuffer[inputOffset + (i * BlockSize) + y] ^ encryptedTweak[y % encryptedTweak.Length]);

                encryptedTweak = MultiplyTweak(encryptedTweak);
            }

            // AES transform the data...
            var transformedBytes = _dataTransform.TransformBlock(outputBuffer, outputOffset, inputCount, outputBuffer, outputOffset);

            // Reset tweak back to original encrypted tweak and then apply output-tweak
            Array.Copy(encryptedTweakOrig, encryptedTweak, 0x10);
            for (int i = 0; i < blocks; i++)
            {
                for (int y = 0; y < BlockSize; y++)
                    outputBuffer[outputOffset + (i * BlockSize) + y] = (byte)(outputBuffer[outputOffset + (i * BlockSize) + y] ^ encryptedTweak[y % encryptedTweak.Length]);

                encryptedTweak = MultiplyTweak(encryptedTweak);
            }

            return transformedBytes;
        }

        public void Dispose()
        {
            _tweakEncryptor.Dispose();
            _dataTransform.Dispose();
            _symmetricAlgorithm.Dispose();
        }
    }
}