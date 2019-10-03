using System;
using System.Collections.Generic;
using LibXboxOne.Nand;
using NDesk.Options;

namespace XBFSTool
{
    class Program
    {
        static void Main(string[] args)
        {
            var printHelp = false;
            var printInfo = false;
            var printCertInfo = false;
            var outputFolder = String.Empty;

            var p = new OptionSet {
                { "h|?|help", "Show this help and exit", v => printHelp = v != null },
                { "i|info", "Print info about nand dump", v => printInfo = v != null },
                { "c|certinfo", "Print certificate info", v => printCertInfo = v != null },
                { "x|extract=", "Specify {OUTPUT DIRECTORY} for extracted files", v => outputFolder = v }
            };

            List<string> extraArgs;
            try
            {
                extraArgs = p.Parse(args);
            }
            catch (OptionException e)
            {
                Console.WriteLine($"Failed parsing parameter \'{e.OptionName}\': {e.Message}");
                Console.WriteLine("Try 'xbfstool --help' for more information");
                return;
            }

            if(extraArgs.Count <= 0)
            {
                Console.WriteLine("ERROR: Missing filepath!");
                Console.WriteLine();
            }

            Console.WriteLine("xbfstool 0.1: Xbox boot filesystem tool");
            Console.WriteLine();
            if (printHelp || extraArgs.Count <= 0)
            {
                Console.WriteLine("Usage  : xbfstool.exe [parameters] [filepath]");
                Console.WriteLine("Parse Xbox boot filesystem");
                Console.WriteLine();
                Console.WriteLine("Parameters:");
                p.WriteOptionDescriptions(Console.Out);
                return;
            }

            var filePath = extraArgs[0];
            var xbfs = new XbfsFile(filePath);
            xbfs.Load();

            if (printCertInfo)
            {
                var consoleCert = xbfs.ReadPspConsoleCertificate();
                var bootcapCert = xbfs.ReadBootcapCertificate();

                Console.WriteLine(consoleCert != null ? consoleCert.ToString() : "No PspConsoleCertificate available");
                Console.WriteLine(bootcapCert != null ? bootcapCert.ToString() : "No BootCapabilityCertificate available");
            }

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
