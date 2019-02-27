using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibXboxOne;
using NDesk.Options;

namespace XBFSTool
{
    class Program
    {
        static void Main(string[] args)
        {
            const string fmt = "    ";

            var printHelp = false;
            var printInfo = false;
            var outputFolder = String.Empty;

            var p = new OptionSet {
                { "h|?|help", v => printHelp = v != null },
                { "i|info", v => printInfo = v != null },
                { "x|extract=", v => outputFolder = v }
            };

            var extraArgs = p.Parse(args);

            Console.WriteLine("xbfstool 0.1: Xbox boot filesystem tool");

            if (printHelp || extraArgs.Count <= 0)
            {
                Console.WriteLine("Usage  : xbfstool.exe [parameters] [filename]");
                Console.WriteLine();
                Console.WriteLine("Parameters:");
                Console.WriteLine(fmt + "-h (-help) - print xbfstool usage");
                Console.WriteLine(fmt + "-i (-info) - print info about nand dump");
                Console.WriteLine(fmt + "-x (-extract) <output-path> - specify output filepath");
                Console.WriteLine();
                return;
            }

            var filePath = extraArgs[0];
            var xbfs = new XbfsFile(filePath);
            xbfs.Load();

            if (printInfo)
            {
                var infoString = xbfs.ToString();
                Console.WriteLine(infoString);
            }

            if (outputFolder != String.Empty)
            {
                Console.WriteLine("Extracting boot filesystem...");
                xbfs.ExtractXbfsData(outputFolder);
            }
        }
    }
}
