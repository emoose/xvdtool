using System;
using System.IO;
using System.Collections.Generic;
using NDesk.Options;
using LibXboxOne;

namespace DurangoKeyExtractor
{
    class Program
    {
        static void Main(string[] args)
        {
            var printHelp = false;
            var outputFolder = String.Empty;

            var p = new OptionSet {
                { "h|?|help", "Show this help and exit", v => printHelp = v != null },
                { "o|output=", "Specify {OUTPUT DIRECTORY} for extracted keys",
                    v => outputFolder = v}
            };

            List<string> extraArgs;
            try
            {
                extraArgs = p.Parse(args);
            }
            catch (OptionException e)
            {
                Console.WriteLine($"Failed parsing parameter \'{e.OptionName}\': {e.Message}");
                Console.WriteLine("Try 'durangokeyextractor --help' for more information");
                return;
            }

            if(extraArgs.Count <= 0)
            {
                Console.WriteLine("ERROR: Missing filepath!");
                Console.WriteLine();
            }

            Console.WriteLine("durangokeyextractor 0.1: Durango key extractor");
            if (printHelp || extraArgs.Count <= 0)
            {
                Console.WriteLine("Usage  : durangokeyextractor.exe [parameters] [filepath]");
                Console.WriteLine();
                Console.WriteLine("Parameters:");
                p.WriteOptionDescriptions(Console.Out);
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
