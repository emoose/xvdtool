using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NDesk.Options;
using LibXboxOne;

// ReSharper disable LocalizableElement

namespace XVDTool
{
    class Program
    {
        static void Main(string[] args)
        {
            const string fmt = "    ";

            var outputFile = String.Empty;
            var fileList = String.Empty;
            var folder = String.Empty;
            var exvdDest = String.Empty;
            var userDataDest = String.Empty;
            var vhdDest = String.Empty;

            var decryptPackage = false;
            var encryptPackage = false;
            var encryptKeyId = 0;
            var rehashPackage = false;
            var resignPackage = false;
            var addHashTree = false;
            var removeHashTree = false;
            var printInfo = false;
            var writeInfo = false;
            var printHelp = false;

            var p = new OptionSet {
   	            { "h|?|help", v => printHelp = v != null },
                { "i|info", v => printInfo = v != null },
                { "wi|writeinfo", v => writeInfo = v != null },
   	            { "o|output=", v => outputFile = v },
                { "nd|nodatahash", v => XvdFile.DisableDataHashChecking = v != null },
                { "nn|nonatives", v => XvdFile.DisableNativeFunctions = v != null },

   	            { "r|rehash", v => rehashPackage = v != null },
                { "rs|resign", v => resignPackage = v != null },

   	            { "eu|decrypt", v => decryptPackage = v != null },
   	            { "ee|encrypt", v =>
   	            {
                    encryptPackage = v != null;
   	                if (!int.TryParse(v, out encryptKeyId))
   	                {
   	                    Console.WriteLine("Error: invalid keyid specified for -encrypt");
   	                    System.Diagnostics.Process.GetCurrentProcess().Kill();
   	                }
   	            } },
                
   	            { "hd|removehash|removehashtree", v => removeHashTree = v != null },
   	            { "he|addhash|addhashtree", v => addHashTree = v != null },

   	            { "xe|extractembedded=", v => exvdDest = v },
   	            { "xu|extractuserdata=", v => userDataDest = v },
                { "xv|extractvhd=", v => vhdDest = v },

                { "l|filelist=", v => fileList = v },
                { "f|folder=", v => folder = v },
            };
            var extraArgs = p.Parse(args);

            Console.WriteLine("xvdtool 0.4: XVD file manipulator");

            XvdFile.LoadKeysFromDisk();

            if(!XvdFile.SignKeyLoaded)
                Console.WriteLine("Warning: rsa3_key.bin file not found, you will be unable to resign packages.");
            if (!XvdFile.OdkKeyLoaded)
                Console.WriteLine("Warning: odk_key.bin file not found, you will be unable to decrypt XVDs.");
            if (!XvdFile.CikFileLoaded)
                Console.WriteLine("Warning: cik_keys.bin file not found, you will be unable to decrypt XVCs.");

            if (printHelp || (String.IsNullOrEmpty(fileList) && String.IsNullOrEmpty(folder) && extraArgs.Count <= 0))
            {
                if (!XvdFile.CikFileLoaded || !XvdFile.OdkKeyLoaded || !XvdFile.SignKeyLoaded)
                    Console.WriteLine();

                Console.WriteLine("Usage  : xvdtool.exe [parameters] [filename]");
                Console.WriteLine();
                Console.WriteLine("Parameters:");
                Console.WriteLine(fmt + "-h (-help) - print xvdtool usage");
                Console.WriteLine(fmt + "-i (-info) - print info about package");
                Console.WriteLine(fmt + "-wi (-writeinfo) - write info about package to [filename].txt");
                Console.WriteLine(fmt + "-o (-output) <output-path> - specify output filename");
                Console.WriteLine(fmt + "-nd (-nodatahash) - disable data hash checking, speeds up -l and -f");
                Console.WriteLine(fmt + "-nn (-nonatives) - disable importing native windows functions (ncrypt etc)");
                Console.WriteLine(fmt + fmt +
                                  "note that signature verification/resigning won't work with this!");
                Console.WriteLine();
                Console.WriteLine(fmt + "-eu (-decrypt) = decrypt output xvd");
                Console.WriteLine(fmt + "-ee (-encrypt) [keyid] = encrypt output xvd");
                Console.WriteLine(fmt + fmt + "(optional [keyid] param for XVCs to choose which key inside cik_keys.bin to use)");
                Console.WriteLine(fmt + fmt +
                                  "XVDs will have a new CIK generated (if CIK in XVD header is empty), which will be encrypted with the odk_key.bin and stored in the XVD header");
                Console.WriteLine();
                Console.WriteLine(fmt + "-hd (-removehash) - remove hash tree/data integrity from package");
                Console.WriteLine(fmt + "-he (-addhash) - add hash tree/data integrity to package");
                Console.WriteLine();
                Console.WriteLine(fmt + "-r (-rehash) - fix data integrity hashes inside package");
                Console.WriteLine(fmt + "-rs (-resign) - sign package using the private key from rsa3_key.bin");
                Console.WriteLine();
                Console.WriteLine(fmt + "-xe (-extractembedded) <output-file> - extract embedded XVD from package");
                Console.WriteLine(fmt + "-xu (-extractuserdata) <output-file> - extract user data from package");
                Console.WriteLine(fmt + "-xv (-extractvhd) <output-vhd> - extracts filesystem from XVD into a VHD file, doesn't seem to work properly with XVC packages yet (also removes NTFS compression from output VHD so Windows can mount it, use -nn to disable)");
                Console.WriteLine();
                Console.WriteLine(fmt + "The next two commands will write info about each package found to [filename].txt");
                Console.WriteLine(fmt + "also extracts embedded XVD and user data to [filename].exvd.bin / [filename].userdata.bin");
                Console.WriteLine(fmt + "-l (-filelist) <path-to-file-list> - use each XVD specified in the list");
                Console.WriteLine(fmt + "-f (-folder) <path-to-folder> - scan folder for XVD files");
                Console.WriteLine();
                Console.WriteLine(@"Note that to mount an XVD/XVC in Windows you'll have to decrypt it and remove the hash tables first (-eu -hd)");
                return;
            }

            Console.WriteLine();

            if (!String.IsNullOrEmpty(folder))
            {
                IEnumerable<string> files = ScanFolderForXvds(folder, true);
                foreach (string filename in files)
                {
                    var xvd = new XvdFile(filename);
                    xvd.Load();
                    try
                    {
                        File.WriteAllText(filename + ".txt", xvd.ToString(true));
                    }
// ReSharper disable once EmptyGeneralCatchClause
                    catch
                    {
                    }

                    if (xvd.Header.EmbeddedXVDLength > 0)
                        File.WriteAllBytes(filename + ".exvd.bin", xvd.ExtractEmbeddedXvd());

                    if(xvd.Header.UserDataLength > 0 && !xvd.IsEncrypted)
                        File.WriteAllBytes(filename + ".userdata.bin", xvd.ExtractUserData());

                    xvd.Dispose();
                }
                return;
            }

            if (!String.IsNullOrEmpty(fileList))
            {
                string[] files = File.ReadAllLines(fileList);
                foreach (string filename in files)
                {
                    var xvd = new XvdFile(filename);
                    xvd.Load();
                    File.WriteAllText(filename + ".txt", xvd.ToString(true));

                    if (xvd.Header.EmbeddedXVDLength > 0)
                        File.WriteAllBytes(filename + ".exvd.bin", xvd.ExtractEmbeddedXvd());

                    if (xvd.Header.UserDataLength > 0 && !xvd.IsEncrypted)
                        File.WriteAllBytes(filename + ".userdata.bin", xvd.ExtractUserData());

                    xvd.Dispose();
                }
                return;
            }

            if (extraArgs.Count > 0)
            {
                string filePath = extraArgs[0];

                if (!File.Exists(filePath))
                {
                    Console.WriteLine(@"Error: input file doesn't exist");
                    return;
                }

                if (!String.IsNullOrEmpty(outputFile))
                {
                    if (File.Exists(outputFile))
                    {
                        Console.WriteLine(@"Error: output file already exists.");
                        return;
                    }
                    File.Copy(filePath, outputFile);
                    filePath = outputFile;
                }

                var file = new XvdFile(filePath);
                file.Load();
                if (printInfo || writeInfo)
                {
                    string info = file.ToString(true);
                    if (writeInfo)
                    {
                        File.WriteAllText(filePath + ".txt", info);
                        Console.WriteLine("Wrote package info to \"" + filePath + ".txt\"");
                        return;
                    }
                    Console.WriteLine(info);
                    return;
                }

                if (decryptPackage)
                {
                    if (!file.IsEncrypted)
                    {
                        Console.WriteLine(@"Error: package already decrypted");
                        return;
                    }
                    string keyToUse = "TestODK";
                    if (file.IsXvcFile)
                    {
                        byte[] outputKey;
                        keyToUse = file.GetXvcKey(0, out outputKey);
                        if (String.IsNullOrEmpty(keyToUse))
                        {
                            Console.WriteLine("Error: unable to find key for key GUID " +
                                              new Guid(file.XvcInfo.EncryptionKeyIds[0].KeyId));
                            return;
                        }
                    }
                    Console.WriteLine("Decrypting package using \"" + keyToUse + "\" key...");
                    bool success = file.Decrypt();
                    Console.WriteLine(success ? "Package decrypted successfully!" : "Error during decryption!");
                    if (!success)
                        return;
                }

                if (encryptPackage)
                {
                    if (file.IsEncrypted)
                    {
                        Console.WriteLine("Error: package already encrypted");
                        return;
                    }
                    string keyToUse = "ODK";
                    if (file.IsXvcFile)
                    {
                        var keyGuids = XvdFile.CikKeys.Keys.ToList();
                        if (encryptKeyId < 0 || encryptKeyId >= keyGuids.Count)
                        {
                            Console.WriteLine(
                                "Error: invalid key index \"" + encryptKeyId + "\" specified, make sure the index you provided exists inside cik_keys.bin!");
                            return;
                        }
                        keyToUse = keyGuids[encryptKeyId].ToString();
                    }
                    Console.WriteLine("Encrypting package using \"" + keyToUse + "\" key...");
                    bool success = file.Encrypt(encryptKeyId);
                    Console.WriteLine(success ? "Package encrypted successfully!" : "Error during encryption!");
                    if (!success)
                        return;
                }

                if (removeHashTree)
                {
                    if (!file.IsDataIntegrityEnabled)
                    {
                        Console.WriteLine("Error: cannot remove hash tree from package that hasn't got a hash tree.");
                        return;
                    }
                    Console.WriteLine("Attempting to remove hash tree from package...");
                    bool success = file.RemoveHashTree() && file.Save();
                    Console.WriteLine(success
                        ? "Hash tree removed successfully and header updated."
                        : "Error: hash tree is larger than input package (???)");
                    return;
                }

                if (addHashTree)
                {
                    if (file.IsDataIntegrityEnabled)
                    {
                        Console.WriteLine("Error: cannot add hash tree to package that already has a hash tree.");
                        return;
                    }
                    Console.WriteLine("Attempting to add hash tree to package...");
                    bool success = file.AddHashTree() && file.Save();
                    Console.WriteLine(success
                        ? "Hash tree added successfully and header updated."
                        : "Error: failed to extend package to make room for hash tree, is there enough disk space?");
                    if (!success)
                        return;
                }

                if (rehashPackage)
                {
                    if (!file.IsDataIntegrityEnabled)
                    {
                        Console.WriteLine("Error: cannot rehash package that hasn't got a hash tree.");
                        return;
                    }
                    Console.WriteLine("Old top hash block hash: " + file.Header.TopHashBlockHash.ToHexString());
                    Console.WriteLine("Rehashing package...");
                    int[] fixedHashes = file.VerifyDataHashTree(true);
                    bool success = file.CalculateHashTree();
                    if (success)
                    {
                        Console.WriteLine("New top hash block hash: " + file.Header.TopHashBlockHash.ToHexString());
                        file.Save();
                    }

                    Console.WriteLine(success
                        ? "Successfully rehashed " + fixedHashes.Length + " invalid data hashes inside package."
                        : "Error: there was a problem rehashing the package.");
                    if (!success)
                        return;
                }

                if (resignPackage)
                {
                    if (!XvdFile.SignKeyLoaded)
                    {
                        Console.WriteLine("Error: rsa3_key.bin file was not found, unable to resign package without it.");
                        return;
                    }
                    bool success = file.Header.ResignWithSignKey();
                    Console.WriteLine(success
                        ? "Successfully resigned package."
                        : "Error: there was a problem resigning the package.");
                    if (!success)
                        return;
                }

                if (!String.IsNullOrEmpty(exvdDest))
                {
                    byte[] exvd = file.ExtractEmbeddedXvd();
                    if (exvd == null || exvd.Length <= 0)
                    {
                        Console.WriteLine("Error: no embedded XVD to extract.");
                        return;
                    }
                    try
                    {
                        File.WriteAllBytes(exvdDest, exvd);
                        Console.WriteLine(
                            "Extracted embedded XVD to \"" + exvdDest + "\" successfully (0x{0:X} bytes)", exvd.Length);
                    }
                    catch
                    {
                        Console.WriteLine("Error: failed to extract embedded XVD.");
                    }
                }


                if (!String.IsNullOrEmpty(userDataDest))
                {
                    byte[] userData = file.ExtractUserData();
                    if (userData == null || userData.Length <= 0)
                    {
                        Console.WriteLine("Error: no user data to extract.");
                        return;
                    }
                    try
                    {
                        File.WriteAllBytes(userDataDest, userData);
                        Console.WriteLine(
                            "Extracted XVD user data to \"" + userDataDest + "\" successfully (0x{0:X} bytes)",
                            userData.Length);
                    }
                    catch
                    {
                        Console.WriteLine("Error: failed to extract XVD user data.");
                    }
                }

                if (!String.IsNullOrEmpty(vhdDest))
                {
                    if (!file.IsEncrypted)
                    {
                        Console.WriteLine("Extracting XVD filesystem to VHD file \"" + vhdDest + "\"...");
                        bool success = file.ConvertToVhd(vhdDest);
                        Console.WriteLine(success
                            ? "Wrote VHD successfully."
                            : "Error: there was a problem extracting the filesystem from the XVD.");
                        if (!success)
                            return;
                    }
                    else
                    {
                        Console.WriteLine("Error: can't convert encrypted package to VHD.");
                    }
                }

                file.Dispose();
            }
        }

        static IEnumerable<string> ScanFolderForXvds(string folderPath, bool recursive)
        {
            var xvdFiles = new List<string>();
            var files = Directory.GetFiles(folderPath);
            foreach (string file in files)
            {
                try
                {
                    using (var io = new IO(file))
                    {
                        io.Stream.Position = 0x200;
                        ulong test = io.Reader.ReadUInt64();
                        if (test == 0x6476782d7466736d) // msft-xvd
                            xvdFiles.Add(file);
                    }
                }
                // ReSharper disable once EmptyGeneralCatchClause
                catch
                {
                }
            }
            if (!recursive)
                return xvdFiles.ToArray();

            var folders = Directory.GetDirectories(folderPath);
            xvdFiles.AddRange(folders.SelectMany(folder => ScanFolderForXvds(folder, true)));

            return xvdFiles;
        }
    }
}
