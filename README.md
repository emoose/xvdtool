# xvdtool

[![GitHub Workflow - Build](https://img.shields.io/github/actions/workflow/status/emoose/xvdtool/build.yml?branch=master)](https://github.com/emoose/xvdtool/actions?query=workflow%3Abuild)

⚠️ No support for leaked files or copyrighted source code is provided, issues or pull requests will be closed without further comment. ⚠️


xvdtool is a C# command-line utility for manipulating Xbox One XVD/XVC packages. It can print detailed info about package headers, resign, rehash, en/decrypt and verify data integrity of a package, it can also convert decrypted XVD files to VHD or extract the filesystem itself.

So far it's only been tested with dev-crypted packages (which use a different 256-bit **Offline Distribution Key (ODK)** to retail packages), as the retail key is still unknown. **This currently makes the tool useless for 90% of people**, but developers looking into how XVD files work will find a detailed mapping of the XVD structures and near-complete methods for manipulating them.

However **no encryption keys are provided with this tool**, you'll have to find them yourself. Hashes for the dev keys are provided below.
If you have an Xbox One development kit or GamingServices framework (Windows10-exclusive) installed, you can use DurangoKeyExtractor to extract the keys from there.

Also included is a tool for extracting files from the XBFS (Xbox Boot File System) inside the Xbox One NAND, based on tuxuser's original [NANDOne](https://github.com/tuxuser/NANDOne) work with a few small additions.
Thanks Kebob for providing [OpenXvd](https://github.com/Kebob/OpenXvd).

## Usage
```
Usage  : xvdtool.exe [parameters] [filename]

Parameters:
    -h (-help) - print xvdtool usage
    -i (-info) - print info about package
    -wi (-writeinfo) - write info about package to [filename].txt
    -o (-output) <output-path> - specify output filename

    -m (-mount) - mount package
    -um (-unmount) - unmount package
    -mp (-mountpoint) - Mount point for package (e.g. "X:")

    -lk (-listkeys) - List known keys including their hashes / availability

    -signfile <path-to-file> - Path to xvd sign key (RSA)
    -odkfile <path-to-file> - Path to Offline Distribution key
    -cikfile <path-to-file> - Path to Content Instance key

    -sk (-signkey) <key-name> - Name of xvd sign key to use
    -odk (-odkid) <id> - Id of Offline Distribution key to use (uint)
    -cik (-cikguid) <GUID> - Guid of Content Instance key to use

    -nd (-nodatahash) - disable data hash checking, speeds up -l and -f
    -ne (-noextract) - disable data (embedded XVD/user data) extraction, speeds up -l and -f

    -eu (-decrypt) - decrypt output xvd
    -ee (-encrypt) - encrypt output xvd
        XVDs will have a new CIK generated (if CIK in XVD header is empty), which will be encrypted with the ODK and stored in the XVD header

    -hd (-removehash) - remove hash tree/data integrity from package
    -he (-addhash) - add hash tree/data integrity to package

    -md (-removemdu) - remove mutable data (MDU) from package

    -r (-rehash) - fix data integrity hashes inside package
    -rs (-resign) - sign package using the private key from rsa3_key.bin

    -xe (-extractembedded) <output-file> - extract embedded XVD from package
    -xu (-extractuserdata) <output-file> - extract user data from package
    -xv (-extractvhd) <output-vhd> - extracts filesystem from XVD into a VHD file
    -xi (-extractimage) <output-file> - extract raw filesystem image
    -xf (-extractfiles) <output-folder> - extract files from XVD filesystem

    The next two commands will write info about each package found to [filename].txt
    also extracts embedded XVD and user data to [filename].exvd.bin / [filename].userdata.bin
    -l (-filelist) <path-to-file-list> - use each XVD specified in the list
    -f (-folder) <path-to-folder> - scan folder for XVD files

To mount a package in Windows you'll have to decrypt it and remove the hash tables & mutable data first (-eu -hd -md)
```

To decrypt non-XVC packages you'll need the correct ODK. The devkit ODK is "widely known" and a hashes are provided below, but as mentioned above the retail key is currently unknown.

Decrypting XVC packages is a different matter, XVC packages use a **Content Instance Key (CIK)** which appears to be stored somewhere outside the package, however where and how it's stored is currently unknown. If you have the correct deobfuscated CIK for a given package you should be able to use it to to decrypt the package.

Devkit/test-signed XVC packages use a static CIK which is also "widely known" (Hash provided below).

## Required Files
To make full use of this tool you'll need the following files, which **are not included**. The tool will work fine without them, but some functions might not work.

You can use the included tool "DurangoKeyExtractor" to extract these keys from the Microsoft.GamingServices framework available on Windows 10.
Just check some DLL / SYS / EXE files - you might find them.

- 33ec8436-5a0e-4f0d-b1ce-3f29c3955039.cik: CIK keys for XVC crypto.
First entry should be the key used by SDK tools/devkits.
Format: `[16 byte encryption key GUID][32 byte CIK]`
~~~
MD5: C9E58F4E1DC611E110A849648DADCC9B
SHA256: 855CCA97C85558AE8E5FF87D8EEDB44AE6B8510601EB71423178B80EF1A7FF7F
~~~
- RedOdk.odk: ODK key used by SDK tools/devkits
Format: `[32 byte ODK]`
~~~
MD5: A2BCFA87F6F83A560BD5739586A5D516
SHA256: CA37132DFB4B811506AE4DC45F45970FED8FE5E58C1BACB259F1B96145B0EBC6
~~~
- RedXvdPrivateKey.rsa: Private RSA key used by SDK tools to verify/sign packages.
Format: `RSAFULLPRIVATEBLOB` struct
~~~
MD5: 2DC371F46B67E29FFCC514C5B134BF73
SHA256: 8E2B60377006D87EE850334C42FC200081386A838C65D96D1EA52032AA9628C5
~~~

For other known keys and their hashes use the `-listkeys` cmdline switch.
To chose a specific key use the following cmdline switches:
```
    -sk (-signkey) <key-name> - Name of xvd sign key to use
    -odk (-odkid) <id> - Id of Offline Distribution key to use (uint)
    -cik (-cikguid) <GUID> - Guid of Content Instance key to use
```

### Mounting XVDs

For mounting of XVD/XVC files, you require DLLs from [GamingServices](https://www.microsoft.com/en-us/p/gaming-services/9mwpm2cqnlhn?activetab=pivot:overviewtab) component.
Download & install it via the Microsoft Store and you should be good to go.

## Possible locations to store keys
XVDTool will create configuration/keys folders on first start - Global and local to the app.

Global configuration folder:
* Windows: `C:\Users\<username>\AppData\Local\xvdtool`
* Linux: `/home/<username>/.config/xvdtool`
* Mac OS X: `/Users/<username>/.config/xvdtool`

Local configuration folder is the current directory of the executable.

Inside these folders you can can store your keys to be autoloaded.

* Xvd Signing keys: `<config dir>/XvdSigningKey/`
* Content Instance keys: `<config dir>/Cik/`
* Offline distribution keys: `<config dir>/Odk/`

Additionally, you can provide keys from arbitrary filesystem locations via the respective cmdline switches: `-signfile, -odkfile, -cikfile`

### Naming the keys
For CIK it is not important how the keys are named if they have the binary structure of `[16 byte encryption key GUID][32 byte CIK]`.
XVD signing keys should have a distinct identifier so you can refer to them via the `-sk (-signkey)` cmdline switch.
ODK needs to be named either by OdkIndex (`<index>.odk`) or by its identifier: `RedOdk.odk, StandardOdk.odk etc.`
For detailed up-to-date info refer to: `LibXboxOne/Keys/`

## What are XVDs?
XVD packages are a secured file format used by the Xbox One to store data, an analogue to the Xbox 360's STFS packages. XVD files are usually used to store system images/data while XVCs (a slightly modified variant of XVDs) are used to store game data.

For a more detailed explanation of XVD files see xvd_info.md

## Third party libraries used
* BouncyCastle (https://www.bouncycastle.org/csharp/)
* NDesk.Options (http://www.ndesk.org/Options)
* DiscUtils (https://github.com/DiscUtils/DiscUtils)

## Building from source

### Requirements

- [.NET 7.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/7.0) - Choose Installer x64 for ease of use

### Building

- After installing the SDK, open up a new powershell window
- Clone the repository
```
git clone https://github.com/emoose/xvdtool
```
- Navigate into the directory
```
cd xvdtool
```
- Build
```
dotnet build -c Release
```

NOTE: If you want to build as DEBUG, either omit `-c Release` or supply `-c Debug` instead.

## Help / Support
xvdtool has been tested on Windows and MacOS but it should work on all systems supported by .NET Core.

There's no help given for this tool besides this readme, it's also currently **very** experimental and **very** likely to blow up in your face. If you do encounter any bugs please submit a description of what happened to the issue tracker.

If you want to help out with development feel free, just make a fork of this repo, make your changes in a new branch of that fork and then submit a pull request from that branch to the master branch of this repo.
