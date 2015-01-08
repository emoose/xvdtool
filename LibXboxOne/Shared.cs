using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace LibXboxOne
{
    public class Natives
    {
        [DllImport("xsapi.dll", SetLastError = true)]
        public static extern uint XvdMount(IntPtr unk1,
                                           out int mountedDiskNum, 
                                           IntPtr hXvdHandle,
                                           [MarshalAs(UnmanagedType.LPWStr)] string pszXvdPath);

        [DllImport("xsapi.dll", SetLastError = true)]
        public static extern uint XvdUnmountDiskNumber(IntPtr hXvdHandle, 
                                                       int diskNum);

        [DllImport("xsapi.dll", SetLastError = true)]
        public static extern uint XvdUnmountFile(IntPtr hXvdHandle, 
                                                 [MarshalAs(UnmanagedType.LPWStr)] string pszXvdPath);

        [DllImport("xsapi.dll", SetLastError = true)]
        public static extern uint XvdOpenAdapter(out IntPtr phXvdHandle);

        [DllImport("xsapi.dll", SetLastError = true)]
        public static extern uint XvdCloseAdapter(IntPtr phXvdHandle);

        [StructLayout(LayoutKind.Sequential)]
// ReSharper disable once InconsistentNaming
        public struct BCRYPT_PSS_PADDING_INFO
        {
            public BCRYPT_PSS_PADDING_INFO(string pszAlgId, int cbSalt)
            {
                this.pszAlgId = pszAlgId;
                this.cbSalt = cbSalt;
            }

            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszAlgId;
            public int cbSalt;
        }


        [DllImport("ncrypt.dll", SetLastError = false)]
        public static extern uint NCryptOpenStorageProvider(out IntPtr phProvider,
                                                              [MarshalAs(UnmanagedType.LPWStr)] string pszProviderName,
                                                              uint dwFlags);

        [DllImport("ncrypt.dll", SetLastError = false)]
        public static extern uint NCryptImportKey(IntPtr hProvider,
                                                  IntPtr hImportKey,
                                                  [MarshalAs(UnmanagedType.LPWStr)] string pszBlobType,
                                                  IntPtr pParameterList,
                                                  out IntPtr phKey,
                                                  [MarshalAs(UnmanagedType.LPArray)]
                                                  byte[] pbData,
                                                  uint cbData,
                                                  uint dwFlags);

        [DllImport("ncrypt.dll", SetLastError = false)]
        public static extern uint NCryptVerifySignature(IntPtr hKey,
                                                        [In] ref BCRYPT_PSS_PADDING_INFO pPaddingInfo,
                                                        [In, MarshalAs(UnmanagedType.LPArray)] byte[] pbHashValue,
                                                        int cbHashValue,
                                                        [In, MarshalAs(UnmanagedType.LPArray)] byte[] pbSignature,
                                                        int cbSignature,
                                                        uint dwFlags);

        [DllImport("ncrypt.dll", SetLastError = false)]
        public static extern uint NCryptSignHash(IntPtr hKey,
                                                        [In] ref BCRYPT_PSS_PADDING_INFO pPaddingInfo,
                                                        [MarshalAs(UnmanagedType.LPArray)]
                                                        byte[] pbHashValue,
                                                        int cbHashValue,
                                                        [MarshalAs(UnmanagedType.LPArray)]
                                                        byte[] pbSignature,
                                                        int cbSignature,
                                                        [Out] out uint pcbResult,
                                                        int dwFlags);

        [DllImport("ncrypt.dll", SetLastError = false)]
        public static extern uint NCryptSignHash(IntPtr hKey,
                                                        [In] ref BCRYPT_PSS_PADDING_INFO pPaddingInfo,
                                                        [In, MarshalAs(UnmanagedType.LPArray)] byte[] pbHashValue,
                                                        int cbHashValue,
                                                        IntPtr pbSignature,
                                                        int cbSignature,
                                                        [Out] out uint pcbResult,
                                                        uint dwFlags);

        [DllImport("ncrypt.dll", SetLastError = false)]
        public static extern uint NCryptFreeObject(IntPtr hObject);

        [DllImport("kernel32.dll")]
        public static extern int DeviceIoControl(IntPtr hDevice, int
            dwIoControlCode, ref short lpInBuffer, int nInBufferSize, IntPtr
            lpOutBuffer, int nOutBufferSize, ref int lpBytesReturned, IntPtr
            lpOverlapped);
    }
// ReSharper disable once InconsistentNaming
    public class IO : IDisposable
    {
        public BinaryReader Reader;
        public BinaryWriter Writer;
        public Stream Stream;

        public IO(string filePath)
        {
            Stream = new FileStream(filePath, FileMode.Open);
            InitIo();
        }

        public IO(string filePath, FileMode mode)
        {
            Stream = new FileStream(filePath, mode);
            InitIo();
        }

        public IO(Stream baseStream)
        {
            Stream = baseStream;
            InitIo();
        }

        public void Dispose()
        {
            Stream.Dispose();
            Reader.Dispose();
            Writer.Dispose();
        }

        public bool AddBytes(long numBytes)
        {
            const int blockSize = 0x1000;

            long startPos = Stream.Position;
            long startSize = Stream.Length;
            long endPos = startPos + numBytes;
            long endSize = Stream.Length + numBytes;

            Stream.SetLength(endSize);

            long totalWrite = startSize - startPos;

            while (totalWrite > 0)
            {
                int toRead = totalWrite < blockSize ? (int)totalWrite : blockSize;

                Stream.Position = startPos + (totalWrite - toRead);
                var data = Reader.ReadBytes(toRead);

                Stream.Position = startPos + (totalWrite - toRead);
                var blankData = new byte[toRead];
                Writer.Write(blankData);

                Stream.Position = endPos + (totalWrite - toRead);
                Writer.Write(data);

                totalWrite -= toRead;
            }

            Stream.Position = startPos;

            return true;
        }

        public bool DeleteBytes(long numBytes)
        {
            if (Stream.Position + numBytes > Stream.Length)
                return false;

            const int blockSize = 0x1000;

            long startPos = Stream.Position;
            long endPos = startPos + numBytes;
            long endSize = Stream.Length - numBytes;
            long i = 0;

            while (i < endSize)
            {
                long totalRemaining = endSize - i;
                int toRead = totalRemaining < blockSize ? (int)totalRemaining : blockSize;

                Stream.Position = endPos + i;
                byte[] data = Reader.ReadBytes(toRead);

                Stream.Position = startPos + i;
                Writer.Write(data);

                i += toRead;
            }

            Stream.SetLength(endSize);
            return true;
        }

        private void InitIo()
        {
            Reader = new BinaryReader(Stream);
            Writer = new BinaryWriter(Stream);
        }
    }
    public static class Shared
    {
        public static string FindFile(string fileName)
        {
            string test = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
            if (File.Exists(test))
                return test;
            string[] drives = Directory.GetLogicalDrives();
            foreach (string drive in drives)
            {
                test = Path.Combine(drive, fileName);
                if (File.Exists(test))
                    return test;
            }
            return String.Empty;
        }

        public static uint SignHash(byte[] key, string keyType, byte[] hash, out byte[] signature) // keyType = RSAFULLPRIVATEBLOB, RSAPRIVATEBLOB, RSAPUBLICBLOB
        {
            IntPtr hProvider;
            IntPtr hKey;

            signature = null;

            uint result = Natives.NCryptOpenStorageProvider(out hProvider, "Microsoft Software Key Storage Provider", 0);
            if (result != 0)
                return result;

            result = Natives.NCryptImportKey(hProvider, IntPtr.Zero, keyType, IntPtr.Zero, out hKey, key, (uint)key.Length, 0);
            if (result != 0)
            {
                Natives.NCryptFreeObject(hProvider);
                return result;
            }

            var pss = new Natives.BCRYPT_PSS_PADDING_INFO("SHA256", 0x20);

            uint resultSigLength;

            result = Natives.NCryptSignHash(hKey, ref pss, hash, hash.Length, IntPtr.Zero, 0, out resultSigLength, 8);
            if (result == 0)
            {
                signature = new byte[resultSigLength];
                result = Natives.NCryptSignHash(hKey, ref pss, hash, hash.Length, signature, 0x200, out resultSigLength, 8);
            }

            Natives.NCryptFreeObject(hKey);
            Natives.NCryptFreeObject(hProvider);

            return result;
        }

        public static uint SignatureValid(byte[] key, string keyType, byte[] signature, byte[] hash) // keyType = RSAFULLPRIVATEBLOB, RSAPRIVATEBLOB, RSAPUBLICBLOB
        {
            IntPtr hProvider;
            IntPtr hKey;

            uint result = Natives.NCryptOpenStorageProvider(out hProvider, "Microsoft Software Key Storage Provider", 0);
            if (result != 0)
                return result;
            result = Natives.NCryptImportKey(hProvider, IntPtr.Zero, keyType, IntPtr.Zero, out hKey, key, (uint)key.Length, 0);
            if (result != 0)
            {
                Natives.NCryptFreeObject(hProvider);
                return result;
            }

            var pss = new Natives.BCRYPT_PSS_PADDING_INFO("SHA256", 0x20);

            result = Natives.NCryptVerifySignature(hKey, ref pss, hash, hash.Length, signature, signature.Length, 8);

            Natives.NCryptFreeObject(hKey);
            Natives.NCryptFreeObject(hProvider);

            return result;
        }

        /// <summary>
        /// Reads in a block from a file and converts it to the struct
        /// type specified by the template parameter
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static T ReadStruct<T>(this BinaryReader reader)
        {
            var size = Marshal.SizeOf(typeof (T));
            // Read in a byte array
            var bytes = reader.ReadBytes(size);

            return BytesToStruct<T>(bytes);
        }

        public static bool WriteStruct<T>(this BinaryWriter writer, T structure)
        {
            byte[] bytes = StructToBytes(structure);

            writer.Write(bytes);

            return true;
        }

        public static T BytesToStruct<T>(byte[] bytes)
        {
            // Pin the managed memory while, copy it out the data, then unpin it
            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            var theStructure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();

            return theStructure;
        }

        public static byte[] StructToBytes<T>(T structure)
        {
            var bytes = new byte[Marshal.SizeOf(typeof(T))];

            // Pin the managed memory while, copy in the data, then unpin it
            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            Marshal.StructureToPtr(structure, handle.AddrOfPinnedObject(), true);
            handle.Free();

            return bytes;
        }

        public static string ToHexString(this byte[] bytes)
        {
            return bytes.Aggregate("", (current, b) => current + (b.ToString("X2") + " "));
        }

        public static bool IsArrayEmpty(this byte[] bytes)
        {
            return bytes.All(b => b == 0);
        }
        public static bool IsEqualTo(this byte[] byte1, byte[] byte2)
        {
            if (byte1.Length != byte2.Length)
                return false;

            for (int i = 0; i < byte1.Length; i++)
                if (byte1[i] != byte2[i])
                    return false;

            return true;
        }

        public static bool IsFlagSet(this uint flags, uint flag)
        {
            return (flags & flag) == flag;
        }

        public static uint RemoveFlag(this uint flags, uint flag)
        {
            return IsFlagSet(flags, flag) ? ToggleFlag(flags, flag) : flags;
        }

        public static uint ToggleFlag(this uint flags, uint flag)
        {
            return (flags ^ flag);
        }

        public static void AppendLineSpace(this StringBuilder b, string str)
        {
            b.AppendLine(str + " ");
        }

        public static byte[] MorphIv(byte[] iv)
        {
            byte dl = 0;
            var newIv = new byte[0x10];

            for (int i = 0; i < 0x10; i++)
            {
                byte cl = iv[i];
                byte al = cl;
                al = (byte)(al + al);
                al = (byte)(al | dl);
                dl = cl;
                newIv[i] = al;
                dl = (byte)(dl >> 7);
            }
            if (dl != 0)
                newIv[0] = (byte)(newIv[0] ^ 0x87);
            return newIv;
        }

        public static byte[] CryptData(bool encrypt, byte[] data, byte[] key, byte[] startIv)
        {
            var cipher = new AesCipher(key);
            int blocks = data.Length / 0x10;
            var newData = new byte[data.Length];
            var iv = new byte[startIv.Length];
            Array.Copy(startIv, iv, startIv.Length);
            for (int i = 0; i < blocks; i++)
            {
                byte[] crypted = CryptBlock(encrypt, data, i * 0x10, 0x10, iv, cipher);
                iv = MorphIv(iv);
                Array.Copy(crypted, 0, newData, i * 0x10, 0x10);
            }
            return newData;
        }

        static byte[] CryptBlock(bool encrypt, byte[] data, int dataOffset, int dataLength, byte[] iv, AesCipher cipher)
        {
            var newData = new byte[dataLength];

            //if (!encrypt)
                for (int i = 0; i < dataLength; i++)
                {
                    newData[i] = (byte)(data[dataOffset + i] ^ iv[i % iv.Length]);
                }

            var cryptData = new byte[dataLength];

            if(encrypt)
                cipher.EncryptBlock(newData, 0, dataLength, cryptData, 0);
            else
                cipher.DecryptBlock(newData, 0, dataLength, cryptData, 0);

            for (int i = 0; i < dataLength; i++)
            {
                cryptData[i] = (byte)(cryptData[i] ^ iv[i % iv.Length]);
            }

            return cryptData;
        }
        public static ushort EndianSwap(this ushort num)
        {
            byte[] data = BitConverter.GetBytes(num);
            Array.Reverse(data);
            return BitConverter.ToUInt16(data, 0);
        }

        public static uint EndianSwap(this uint num)
        {
            byte[] data = BitConverter.GetBytes(num);
            Array.Reverse(data);
            return BitConverter.ToUInt32(data, 0);
        }

        public static ulong EndianSwap(this ulong num)
        {
            byte[] data = BitConverter.GetBytes(num);
            Array.Reverse(data);
            return BitConverter.ToUInt64(data, 0);
        }
    }
}
