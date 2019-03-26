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
        public static extern uint XvdMount(out IntPtr hDiskHandle,
                                           out int mountedDiskNum, 
                                           IntPtr hXvdHandle,
                                           [MarshalAs(UnmanagedType.LPWStr)] string pszXvdPath,
                                           long unknown,
                                           [MarshalAs(UnmanagedType.LPWStr)] string pszMountPoint,
                                           int mountFlags);

        [DllImport("xsapi.dll", SetLastError = true)]
        public static extern uint XvdMountContentType(out IntPtr hDiskHandle,
                                           out int mountedDiskNum,
                                           IntPtr hXvdHandle,
                                           [MarshalAs(UnmanagedType.LPWStr)] string pszXvdPath,
                                           long xvdContentType,
                                           long unknown,
                                           [MarshalAs(UnmanagedType.LPWStr)] string pszMountPoint,
                                           int mountFlags);

        [DllImport("xsapi.dll", SetLastError = true)]
        public static extern uint XvdVmMount(out IntPtr hDiskHandle,
                                           IntPtr hXvdHandle,
                                           long vmNumber,
                                           [MarshalAs(UnmanagedType.LPWStr)] string pszXvdPath,
                                           long unknown,
                                           [MarshalAs(UnmanagedType.LPWStr)] string pszMountPoint,
                                           int mountFlags);

        [DllImport("xsapi.dll", SetLastError = true)]
        public static extern uint XvdUnmountDiskNumber(IntPtr hXvdHandle, 
                                                       int diskNum);

        [DllImport("xsapi.dll", SetLastError = true)]
        public static extern uint XvdUnmountFile(IntPtr hXvdHandle, 
                                                 [MarshalAs(UnmanagedType.LPWStr)] string pszXvdPath);

        [DllImport("xsapi.dll", SetLastError = true)]
        public static extern uint XvdVmUnmountFile(IntPtr hXvdHandle,
                                                 long vmId,
                                                 [MarshalAs(UnmanagedType.LPWStr)] string pszXvdPath);

        [DllImport("xsapi.dll", SetLastError = true)]
        public static extern uint XvdOpenAdapter(out IntPtr phXvdHandle);

        [DllImport("xsapi.dll", SetLastError = true)]
        public static extern uint XvdCloseAdapter(IntPtr phXvdHandle);

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

        public static string ToHexString(this byte[] bytes, string seperator = " ")
        {
            return bytes.Aggregate("", (current, b) => $"{current}{b:X2}{seperator}");
        }

        public static string ToHexString(this uint[] array, string seperator = " ")
        {
            return array.Aggregate("", (current, b) => $"{current}0x{b:X8}{seperator}");
        }

        public static string ToHexString(this ushort[] array, string seperator = " ")
        {
            return array.Aggregate("", (current, b) => $"{current}0x{b:X4}{seperator}");
        }

        public static byte[] ToBytes(this string hexString)
        {
            hexString = hexString.Replace(" ", "");

            byte[] retval = new byte[hexString.Length / 2];
            for (int i = 0; i < hexString.Length; i += 2)
                retval[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
            return retval;
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

        public static void AppendLineSpace(this StringBuilder b, string str)
        {
            b.AppendLine(str + " ");
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
