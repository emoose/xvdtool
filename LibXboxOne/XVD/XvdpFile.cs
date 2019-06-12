using System;
using System.Text;

namespace LibXboxOne
{
    public class XvdpFile : IDisposable
    {
        private readonly IO _io;

        public readonly string FilePath;
        public const uint Magic = 0x58564450; // XVDP
        public const int HeaderSize = 0x400;

        public XvdpHeader Header { get; private set; }
        public XvdFile XvdFile { get; private set; }

        public XvdpFile(string filePath)
        {
            FilePath = filePath;
            _io = new IO(FilePath);
        }

        public bool Load()
        {
            _io.Stream.Position = 0;
            Header = _io.Reader.ReadStruct<XvdpHeader>();

            if (!Header.IsMagicValid)
            {
                Console.WriteLine($"XvdpFile.Load: Invalid Magic");
                return false;
            }

            XvdFile = new XvdFile(OffsetSubStream.Create(_io.Stream));
            if (!XvdFile.Load())
            {
                Console.WriteLine("XvdpFile.Load: XvdFile.Load failed!");
                return false;
            }

            return true;
        }

        public override string ToString()
        {
            return ToString(true);
        }

        public string ToString(bool formatted)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(Header.ToString(formatted));
            sb.AppendLine();
            sb.Append(XvdFile.ToString(formatted));

            return sb.ToString();
        }

        public void Dispose()
        {
            XvdFile.Dispose();
            _io.Dispose();
        }
    }
}