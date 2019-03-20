using System;
using System.IO;
using System.Security.Cryptography;

namespace LibXboxOne
{
    class AesXtsTransform : IDisposable
    {
        public int BlockSize;
        private readonly bool _encrypt;
        private readonly byte[] _tweakBytes;
        private readonly byte[] _dataAesKey;
        private readonly byte[] _tweakAesKey;
        private readonly ICryptoTransform _tweakEncryptor;
        private readonly ICryptoTransform _dataTransform;
        private readonly SymmetricAlgorithm _symmetricAlgorithm;

        public AesXtsTransform(byte[] tweakBytes, byte[] dataAesKey, byte[] tweakAesKey, bool encrypt)
        {
            if (tweakBytes == null) throw new InvalidDataException("Tweak bytes not provided");
            else if (dataAesKey == null) throw new InvalidDataException("Data AES key not provided");
            else if (tweakAesKey == null) throw new InvalidDataException("Tweak AES key not provided");
            else if (tweakBytes.Length != 16) throw new InvalidDataException("Tweak bytes not 16 bytes");
            else if (dataAesKey.Length != 16) throw new InvalidDataException("Data AES key not 16 bytes");
            else if (tweakAesKey.Length != 16) throw new InvalidDataException("Tweak AES not 16 bytes");

            _encrypt = encrypt;
            _tweakBytes = tweakBytes;
            _dataAesKey = dataAesKey;
            _tweakAesKey = tweakAesKey;

            _symmetricAlgorithm = Aes.Create();
            _symmetricAlgorithm.Padding = PaddingMode.None;
            _symmetricAlgorithm.Mode = CipherMode.ECB;

            byte[] nullIv = new byte[16];
            _tweakEncryptor = _symmetricAlgorithm.CreateEncryptor(_tweakAesKey, nullIv);
            _dataTransform = _encrypt ? _symmetricAlgorithm.CreateEncryptor(_dataAesKey, nullIv) :
                                        _symmetricAlgorithm.CreateDecryptor(_dataAesKey, nullIv);

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

        public int TransformDataUnit(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset, ulong dataUnit)
        {
            int transformedBytes = 0;
            int blocks = inputCount / BlockSize;
            byte[] encryptedTweak = new byte[0x10];
            byte[] tweak = _tweakBytes;

            // Update tweak with data unit number
            Array.Copy(BitConverter.GetBytes(dataUnit), tweak, 4);

            // Encrypt tweak
            _tweakEncryptor.TransformBlock(tweak, 0, tweak.Length, encryptedTweak, 0);

            for (int i = 0; i < blocks; i++)
            {
                // Encrypt data, using encrypted tweak as IV
                transformedBytes += TransformBlock(inputBuffer, inputOffset + (i * BlockSize), BlockSize,
                                                   outputBuffer, outputOffset + (i * BlockSize),
                                                   encryptedTweak);
                encryptedTweak = MultiplyTweak(encryptedTweak);
            }

            return transformedBytes;
        }

        int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset, byte[] encryptedTweak)
        {
            for (int i = 0; i < inputCount; i++)
            {
                inputBuffer[inputOffset + i] = (byte)(inputBuffer[inputOffset + i] ^ encryptedTweak[i % encryptedTweak.Length]);
            }

            int transformedBytes = _dataTransform.TransformBlock(inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset);

            for (int i = 0; i < inputCount; i++)
            {
                outputBuffer[outputOffset + i] = (byte)(outputBuffer[outputOffset + i] ^ encryptedTweak[i % encryptedTweak.Length]);
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