using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibXboxOne;

namespace XBFSTool
{
    class Program
    {
        static void Main(string[] args)
        {
            var xbfs = new XbfsFile(@"F:\XBone\stuff\nands\DUMP1.bin");
            xbfs.Load();
            var info = xbfs.GetXbfsInfo();
            xbfs.ExtractXbfsData(@"F:\XBone\nanddump\");
            var test = xbfs.ToString(true);
        }
    }
}
