using System;
using NDesk.Options;
using LibXboxOne;
using System.IO;

namespace DurangoKeyExtractor
{
    class Program
    {
        static void Main(string[] args)
        {
            const string fmt = "    ";

            var printHelp = false;
            var outputFolder = String.Empty;

            var p = new OptionSet {
                { "h|?|help", v => printHelp = v != null },
                { "o|output=", v => outputFolder = v}
            };

            var extraArgs = p.Parse(args);

            Console.WriteLine("durangokeyextractor 0.1: Durango key extractor");

            if (printHelp || extraArgs.Count <= 0)
            {
                Console.WriteLine("Usage  : durangokeyextractor.exe [file path]");
                Console.WriteLine();
                Console.WriteLine("Parameters:");
                Console.WriteLine(fmt + "-h (-help) - print program usage");
                Console.WriteLine(fmt + "-o (-output) - Output folder to store extracted keys");
                Console.WriteLine();
                return;
            }

            if (outputFolder == String.Empty)
            {
                outputFolder = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "extracted");
            }

            var filePath = extraArgs[0];
            KeyExtractor extractor;

            try
            {
                extractor = new KeyExtractor(filePath);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error setting up KeyExtractor: {e.Message}");
                return;
            }

            Console.WriteLine($"Scanning {filePath} for known keys...");
            int foundCount = extractor.PullKeysFromFile();
            if (foundCount == 0)
            {
                Console.WriteLine("No keys found :(");
                return;
            }

            Console.WriteLine($"Found {foundCount} keys :)");
            Console.WriteLine($"Saving keys to \"{outputFolder}\"...");
            extractor.SaveFoundKeys(outputFolder);
        }
    }
}
