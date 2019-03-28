using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NDesk.Options;
using LibXboxOne;
using System.Reflection;
using LibXboxOne.Keys;

// ReSharper disable LocalizableElement

namespace XVDTool
{
    class Program
    {
        static readonly string AppName = "xvdtool";
        static void EnsureConfigDirectoryStructure(string basePath)
        {
            foreach (var keyDirName in Enum.GetNames(typeof(KeyType)))
            {
                var keyDirectory = Path.Combine(basePath, keyDirName);
                if (!Directory.Exists(keyDirectory))
                    Directory.CreateDirectory(keyDirectory);
            }
        }

        static void Main(string[] args)
        {
            const string fmt = "    ";

            var outputFile = String.Empty;
            var fileList = String.Empty;
            var folder = String.Empty;
            var exvdDest = String.Empty;
            var userDataDest = String.Empty;
            var vhdDest = String.Empty;
            var rawimageDest = String.Empty;
            var fsDest = String.Empty;

            var signKeyToUse = String.Empty;
            var odkToUse = OdkIndex.Invalid;
            var cikToUse = Guid.Empty;

            var signKeyFilepath = String.Empty;
            var odkFilepath = String.Empty;
            var cikFilepath = String.Empty;

            bool listKeys = false;

            string mountPoint = null;

            var mountPackage = false;
            var unmountPackage = false;

            var decryptPackage = false;
            var encryptPackage = false;
            var rehashPackage = false;
            var resignPackage = false;
            var addHashTree = false;
            var removeHashTree = false;
            var addMdu = false;
            var removeMdu = false;
            var printInfo = false;
            var writeInfo = false;
            var printHelp = false;

            var disableDataExtract = false;

            var p = new OptionSet {
                { "h|?|help", v => printHelp = v != null },
                { "i|info", v => printInfo = v != null },
                { "wi|writeinfo", v => writeInfo = v != null },
                { "o|output=", v => outputFile = v },

                { "m|mount", v => mountPackage = v != null },
                { "um|unmount", v => unmountPackage = v != null },
                { "mp|mountpoint=", v => mountPoint = v },

                { "lk|listkeys", v => listKeys = v != null },

                { "signfile=", v => signKeyFilepath = v },
                { "odkfile=", v => odkFilepath = v },
                { "cikfile=", v => cikFilepath = v },

                { "sk|signkey=", v => signKeyToUse = v },
                { "odk|odkid=", v =>
                {
                    if (!DurangoKeys.GetOdkIndexFromString(v, out odkToUse))
                        throw new OptionException("Invalid Odk Id", "odkid");
                }},
                { "cik|cikguid=", v =>
                {
                    if (!Guid.TryParse(v, out cikToUse))
                        throw new OptionException("Invalid CIK GUID", "cikguid");
                }},
                { "nd|nodatahash", v => XvdFile.DisableDataHashChecking = v != null },
                { "ne|noextract", v => disableDataExtract = v != null },

                { "r|rehash", v => rehashPackage = v != null },
                { "rs|resign", v => resignPackage = v != null },

                { "eu|decrypt", v => decryptPackage = v != null },
                { "ee|encrypt", v => encryptPackage = v != null },
                { "hd|removehash|removehashtree", v => removeHashTree = v != null },
                { "he|addhash|addhashtree", v => addHashTree = v != null },
                { "md|removemdu", v => removeMdu = v != null },
                { "ma|addmdu", v => addMdu = v != null },

                { "xe|extractembedded=", v => exvdDest = v },
                { "xu|extractuserdata=", v => userDataDest = v },
                { "xv|extractvhd=", v => vhdDest = v },
                { "xi|extractimage=", v => rawimageDest = v },
                { "xf|extractfiles=", v => fsDest = v },

                { "l|filelist=", v => fileList = v },
                { "f|folder=", v => folder = v },
            };

            List<string> extraArgs;
            try
            {
                extraArgs = p.Parse(args);
            }
            catch (OptionException e)
            {
                Console.WriteLine($"Failed parsing parameter \'{e.OptionName}\': {e.Message}");
                Console.WriteLine("Try 'xvdtool --help' for more information");
                return;
            }

            Console.WriteLine("xvdtool 0.5: XVD file manipulator");

            if (printHelp || (String.IsNullOrEmpty(fileList) && String.IsNullOrEmpty(folder) && !listKeys && extraArgs.Count <= 0))
            {
                Console.WriteLine("Usage  : xvdtool.exe [parameters] [filename]");
                Console.WriteLine();
                Console.WriteLine("Parameters:");
                Console.WriteLine(fmt + "-h (-help) - print xvdtool usage");
                Console.WriteLine(fmt + "-i (-info) - print info about package");
                Console.WriteLine(fmt + "-wi (-writeinfo) - write info about package to [filename].txt");
                Console.WriteLine(fmt + "-o (-output) <output-path> - specify output filename");
                Console.WriteLine();
                Console.WriteLine(fmt + "-m (-mount) - mount package");
                Console.WriteLine(fmt + "-um (-unmount) - unmount package");
                Console.WriteLine(fmt + "-mp (-mountpoint) - Mount point for package (e.g. \"X:\")");
                Console.WriteLine();
                Console.WriteLine(fmt + "-lk (-listkeys) - List known keys including their hashes / availability");
                Console.WriteLine();
                Console.WriteLine(fmt + "-signfile <path-to-file> - Path to xvd sign key (RSA)");
                Console.WriteLine(fmt + "-odkfile <path-to-file> - Path to Offline Distribution key");
                Console.WriteLine(fmt + "-cikfile <path-to-file> - Path to Content Instance key");
                Console.WriteLine();
                Console.WriteLine(fmt + "-sk (-signkey) <key-name> - Name of xvd sign key to use");
                Console.WriteLine(fmt + "-odk (-odkid) <id> - Id of Offline Distribution key to use (uint)");
                Console.WriteLine(fmt + "-cik (-cikguid) <GUID> - Guid of Content Instance key to use");
                Console.WriteLine();
                Console.WriteLine(fmt + "-nd (-nodatahash) - disable data hash checking, speeds up -l and -f");
                Console.WriteLine(fmt + "-ne (-noextract) - disable data (embedded XVD/user data) extraction, speeds up -l and -f");
                Console.WriteLine();
                Console.WriteLine(fmt + "-eu (-decrypt) - decrypt output xvd");
                Console.WriteLine(fmt + "-ee (-encrypt) - encrypt output xvd");
                Console.WriteLine(fmt + fmt +
                                  "XVDs will have a new CIK generated (if CIK in XVD header is empty), which will be encrypted with the ODK and stored in the XVD header");
                Console.WriteLine();
                Console.WriteLine(fmt + "-hd (-removehash) - remove hash tree/data integrity from package");
                Console.WriteLine(fmt + "-he (-addhash) - add hash tree/data integrity to package");
                Console.WriteLine();
                Console.WriteLine(fmt + "-md (-removemdu) - remove mutable data (MDU) from package");
                Console.WriteLine(fmt + "-ma (-addmdu) - add mutable data (MDU) to package");
                Console.WriteLine();
                Console.WriteLine(fmt + "-r (-rehash) - fix data integrity hashes inside package");
                Console.WriteLine(fmt + "-rs (-resign) - sign package using the private key from rsa3_key.bin");
                Console.WriteLine();
                Console.WriteLine(fmt + "-xe (-extractembedded) <output-file> - extract embedded XVD from package");
                Console.WriteLine(fmt + "-xu (-extractuserdata) <output-file> - extract user data from package");
                Console.WriteLine(fmt + "-xv (-extractvhd) <output-vhd> - extracts filesystem from XVD into a VHD file");
                Console.WriteLine(fmt + "-xi (-extractimage) <output-file> - extract raw filesystem image");
                Console.WriteLine(fmt + "-xf (-extractfiles) <output-folder> - extract files from XVD filesystem");
                Console.WriteLine();
                Console.WriteLine(fmt + "The next two commands will write info about each package found to [filename].txt");
                Console.WriteLine(fmt + "also extracts embedded XVD and user data to [filename].exvd.bin / [filename].userdata.bin");
                Console.WriteLine(fmt + "-l (-filelist) <path-to-file-list> - use each XVD specified in the list");
                Console.WriteLine(fmt + "-f (-folder) <path-to-folder> - scan folder for XVD files");
                Console.WriteLine();
                Console.WriteLine(@"To mount a package in Windows you'll have to decrypt it and remove the hash tables & mutable data first (-eu -hd -md)");
                return;
            }

            Console.WriteLine();

            var fallbackCik = DurangoKeys.TestCIK;
            var fallbackSignKey = DurangoKeys.RedXvdPrivateKey;

            var localConfigDir = AppDirs.GetApplicationConfigDirectory(AppName);
            var executableDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            /* If necessary, create folders to store keys */
            EnsureConfigDirectoryStructure(localConfigDir);
            EnsureConfigDirectoryStructure(executableDir);

            /* Load keys from global and local config directory */
            DurangoKeys.LoadKeysRecursive(localConfigDir);
            DurangoKeys.LoadKeysRecursive(executableDir);

            /* Check key parameters */
            if (signKeyFilepath != String.Empty)
            {
                if (!DurangoKeys.LoadKey(KeyType.XvdSigningKey, signKeyFilepath))
                {
                    Console.WriteLine($"Failed to load SignKey from {signKeyFilepath}");
                    return;
                }
            }
            if (odkFilepath != String.Empty)
            {
                if (!DurangoKeys.LoadKey(KeyType.Odk, odkFilepath))
                {
                    Console.WriteLine($"Failed to load ODK key from {odkFilepath}");
                    return;
                }
            }

            if (cikFilepath != String.Empty)
            {
                if (!DurangoKeys.LoadKey(KeyType.Cik, cikFilepath))
                {
                    Console.WriteLine($"Failed to load CIK from {cikFilepath}");
                    return;
                }
            }

            if(signKeyToUse == String.Empty)
            {
                Console.WriteLine($"No desired signkey provided, falling back to {fallbackSignKey}");
                signKeyToUse = fallbackSignKey;
            }

            if(cikToUse == Guid.Empty)
            {
                Console.WriteLine($"No desired CIK provided, falling back to {fallbackCik}");
                cikToUse = fallbackCik;
            }

            if(odkToUse == OdkIndex.Invalid)
                Console.WriteLine("No desired or invalid ODK provided, will try to use ODK indicated by XVD header");
            else if (!DurangoKeys.IsOdkLoaded(odkToUse))
                Console.WriteLine($"Warning: ODK {odkToUse} could not be loaded!");
            else
                Console.WriteLine($"Using ODK: {odkToUse}");

            Console.WriteLine(!DurangoKeys.IsSignkeyLoaded(signKeyToUse)
                ? "Warning: Signkey could not be loaded, you will be unable to resign XVD headers!"
                : $"Using Xvd Signkey: {signKeyToUse}");

            Console.WriteLine(!DurangoKeys.IsCikLoaded(cikToUse)
                ? "Warning: CIK could not be loaded!"
                : $"Using CIK: {cikToUse}");

            Console.WriteLine();

            if (listKeys)
            {
                void PrintKnownKeys<T>(KeyType type, KeyValuePair<T,DurangoKeyEntry>[] keyCollection)
                {
                    Console.WriteLine($"{type}:");
                    foreach(var keyKvp in keyCollection)
                    {
                        Console.WriteLine(fmt + $"{keyKvp.Key}: {keyKvp.Value}");
                    }
                }

                Console.WriteLine("Known Durango keys:");
                PrintKnownKeys(KeyType.XvdSigningKey, DurangoKeys.GetAllXvdSigningKeys());
                PrintKnownKeys(KeyType.Odk, DurangoKeys.GetAllODK());
                PrintKnownKeys(KeyType.Cik, DurangoKeys.GetAllCIK());
                return;
            }

            /* Write out xvd info / extract data from provided folderpath */
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

                    if (!disableDataExtract)
                    {
                        if (xvd.Header.EmbeddedXVDLength > 0)
                            File.WriteAllBytes(filename + ".exvd.bin", xvd.ExtractEmbeddedXvd());

                        if (xvd.Header.UserDataLength > 0 && !xvd.IsEncrypted)
                            File.WriteAllBytes(filename + ".userdata.bin", xvd.ExtractUserData());
                    }
                    xvd.Dispose();
                }
                return;
            }

            /* Write out xvd info / extract data from provided filelist */
            if (!String.IsNullOrEmpty(fileList))
            {
                string[] files = File.ReadAllLines(fileList);
                foreach (string filename in files)
                {
                    var xvd = new XvdFile(filename);
                    xvd.Load();
                    File.WriteAllText(filename + ".txt", xvd.ToString(true));

                    if (!disableDataExtract)
                    {
                        if (xvd.Header.EmbeddedXVDLength > 0)
                            File.WriteAllBytes(filename + ".exvd.bin", xvd.ExtractEmbeddedXvd());

                        if (xvd.Header.UserDataLength > 0 && !xvd.IsEncrypted)
                            File.WriteAllBytes(filename + ".userdata.bin", xvd.ExtractUserData());
                    }

                    xvd.Dispose();
                }
                return;
            }

            /* Handle input xvd */
            if (extraArgs.Count <= 0)
                return;

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

            if (mountPackage)
            {
                bool success = XvdMount.MountXvd(filePath, mountPoint);
                Console.WriteLine("Mounting {0} {1}", filePath, success ?
                    "completed successfully" :
                    "failed with error"
                );
                return;
            }

            if (unmountPackage)
            {
                bool success = XvdMount.UnmountXvd(filePath);
                Console.WriteLine("Unmounting {0} {1}", filePath, success ?
                    "completed successfully" :
                    "failed with error"
                );
                return;
            }

            Console.WriteLine($"Loading file from {filePath}...");
            if (!XvdFile.DisableDataHashChecking)
                Console.WriteLine("(and verifying hash table, use -nd to disable)");

            var file = new XvdFile(filePath)
            {
                OverrideOdk = odkToUse
            };

            file.Load();
            if (printInfo || writeInfo)
            {
                string info = file.ToString(true);
                if (writeInfo)
                {
                    File.WriteAllText(filePath + ".txt", info);
                    Console.WriteLine($"Wrote package info to \"{filePath}.txt\"");
                }
                else
                    Console.WriteLine(info);
            }

            if (addHashTree)
            {
                if (file.IsDataIntegrityEnabled)
                    Console.WriteLine("Warning: -addhashtree failed as package already has a hash tree.");
                else
                {
                    Console.WriteLine("Attempting to add hash tree to package...");
                    bool success = file.AddHashTree() && file.Save();
                    Console.WriteLine(success
                        ? "Hash tree added successfully and header updated."
                        : "Error: failed to extend package to make room for hash tree, is there enough disk space?");
                    if (!success)
                        return;
                }
            }

            if (encryptPackage)
            {
                if (file.IsEncrypted)
                    Console.WriteLine("Warning: -encrypt failed as package is already encrypted");
                else
                {
                    string keyToUse = "ODK";
                    if (file.IsXvcFile)
                    {
                        var cikKeys = DurangoKeys.GetAllCIK();
                        var chosenKey = cikKeys.Single(kvp => kvp.Key == cikToUse).Value;
                        if (chosenKey == null)
                        {
                            Console.WriteLine("Error: Invalid CIK key \"{encryptKeyId}\" specified, make sure said key exists!");
                            return;
                        }
                        keyToUse = $"CIK:{cikToUse}";
                    }
                    Console.WriteLine($"Encrypting package using \"{keyToUse}\" key...");
                    bool success = file.Encrypt(cikToUse);
                    Console.WriteLine(success ? "Package encrypted successfully!" : "Error during encryption!");
                    if (!success)
                        return;
                }
            }

            if (decryptPackage)
            {
                if (!file.IsEncrypted)
                    Console.WriteLine(@"Warning: -decrypt failed as package is already decrypted");
                else
                {
                    if (file.IsXvcFile)
                    {
                        Console.WriteLine("Decrypting XVC...");
                    }
                    else
                    {
                        string keyToUse = odkToUse != OdkIndex.Invalid ? odkToUse.ToString() : "<ODK indicated by XVD header>";
                        Console.WriteLine($"Decrypting XVD using \"{keyToUse}\" key...");
                    }

                    bool success = file.Decrypt();
                    Console.WriteLine(success ? "Package decrypted successfully!" : "Error during decryption!");
                    if (!success)
                        return;
                }
            }

            if (removeHashTree)
            {
                if (!file.IsDataIntegrityEnabled)
                    Console.WriteLine("Warning: -removehashtree failed as package doesn't have a hash tree.");
                else
                {
                    Console.WriteLine("Attempting to remove hash tree from package...");
                    bool success = file.RemoveHashTree();
                    Console.WriteLine(success
                        ? "Hash tree removed successfully and header updated."
                        : "Error: hash tree is larger than input package (???)");
                    if (!success)
                        return;
                }
            }

            if(removeMdu)
            {
                if (file.Header.MutableDataPageCount <= 0)
                    Console.WriteLine("Warning: -removemdu failed as package doesn't have any mutable data.");
                else
                {
                    Console.WriteLine("Removing mutable data from package...");
                    bool success = file.RemoveMutableData();
                    Console.WriteLine(success
                        ? "Mutable data removed successfully and header updated."
                        : "Failed to remove mutable data?");
                    if (!success)
                        return;
                }
            }

            if(addMdu)
            {
                if (file.Header.MutableDataPageCount > 0)
                    Console.WriteLine("Warning: -addmdu failed as package already has mutable data.");
                else
                {
                    Console.WriteLine("Adding mutable data to package...");
                    bool success = file.AddMutableData();
                    Console.WriteLine(success
                        ? "Mutable data added successfully and header updated."
                        : "Failed to add mutable data?");
                    if (!success)
                        return;
                }
            }

            if (rehashPackage)
            {
                if (!file.IsDataIntegrityEnabled)
                    Console.WriteLine("Warning: -rehash failed as package doesn't have a hash tree.");
                else
                {
                    Console.WriteLine($"Old top hash block hash: {file.Header.TopHashBlockHash.ToHexString()}");
                    Console.WriteLine("Rehashing package...");
                    ulong[] fixedHashes = file.VerifyDataHashTree(true);
                    bool success = file.CalculateHashTree();
                    if (success)
                    {
                        Console.WriteLine($"New top hash block hash: {file.Header.TopHashBlockHash.ToHexString()}");
                        file.Save();
                    }

                    Console.WriteLine(success
                        ? $"Successfully rehashed {fixedHashes.Length} invalid data hashes inside package."
                        : "Error: there was a problem rehashing the package.");
                    if (!success)
                        return;
                }
            }

            if (resignPackage)
            {
                var key = DurangoKeys.GetSignkeyByName(signKeyToUse);
                bool success = file.Header.Resign(key.KeyData, "RSAFULLPRIVATEBLOB");
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
                    Console.WriteLine("Warning: -extractembedded failed as package doesn't contain an embedded XVD.");
                else
                {
                    try
                    {
                        File.WriteAllBytes(exvdDest, exvd);
                        Console.WriteLine($"Extracted embedded XVD to \"{exvdDest}\" successfully (0x{exvd.Length:X} bytes)");
                    }
                    catch
                    {
                        Console.WriteLine("Error: failed to extract embedded XVD.");
                    }
                }
            }


            if (!String.IsNullOrEmpty(userDataDest))
            {
                byte[] userData = file.ExtractUserData();
                if (userData == null || userData.Length <= 0)
                    Console.WriteLine("Warning: -extractuserdata failed as package doesn't contain user data.");
                else
                {
                    try
                    {
                        File.WriteAllBytes(userDataDest, userData);
                        Console.WriteLine($"Extracted XVD user data to \"{userDataDest}\" successfully (0x{userData.Length:X} bytes)");
                    }
                    catch
                    {
                        Console.WriteLine("Error: failed to extract XVD user data.");
                    }
                }
            }

            if (!String.IsNullOrEmpty(vhdDest))
            {
                if (file.IsEncrypted)
                    Console.WriteLine("Warning: -extractvhd failed as package is still encrypted.");
                else
                {
                    Console.WriteLine($"Extracting XVD filesystem to VHD file \"{vhdDest}\"...");
                    bool success = file.Filesystem.ConvertToVhd(vhdDest);
                    Console.WriteLine(success
                        ? "Wrote VHD successfully."
                        : "Error: there was a problem extracting the filesystem from the XVD.");
                    if (!success)
                        return;
                }
            }

            if (!String.IsNullOrEmpty(rawimageDest))
            {
                if(file.IsEncrypted)
                    Console.WriteLine("Warning: -extractimage failed as package is still encrypted.");
                else
                {
                    Console.WriteLine($"Extracting raw filesystem image to file \"{rawimageDest}\"...");
                    bool success = file.Filesystem.ExtractFilesystemImage(rawimageDest, false);
                    Console.WriteLine(success
                        ? "Extracted raw image successfully."
                        : "Error: there was a problem extracting raw image from the XVD.");
                    if (!success)
                        return;
                }
            }

            if (!String.IsNullOrEmpty(fsDest))
            {
                if(file.IsEncrypted)
                    Console.WriteLine("Warning: -extractfiles failed as package is still encrypted.");
                else
                {
                    Console.WriteLine($"Extracting XVD files to folder \"{fsDest}\"...");
                    bool success = file.Filesystem.ExtractFilesystem(fsDest);
                    Console.WriteLine(success
                        ? "Extracted files successfully."
                        : "Error: there was a problem extracting the files from the XVD.");
                    if (!success)
                        return;
                }
            }

            file.Dispose();
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
