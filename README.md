### xvdtool - by emoose (aka noob25x)

xvdtool is a C# command-line utility for manipulating Xbox One XVD/XVC packages. It can print detailed info about package headers, resign, rehash, en/decrypt and verify data integrity of a package, it can also convert (some, but not all) decrypted XVD files to VHD.

So far it's only been tested with dev-crypted packages (which use a different 256-bit "ODK" obfuscation key to retail packages), as the retail key is still unknown. **This currently makes the tool useless for 90% of people**, but developers looking into how XVD files work will find a detailed mapping of the XVD structures and complete methods for manipulating them.

However **no encryption keys are provided with this tool**, you'll have to find them yourself. MD5 hashes for the dev keys are provided below, if you have an Xbox One development kit installed the keys can automatically be extracted from there too.

Also included is a tool for extracting files from the XBFS (Xbox Boot File System) inside the Xbox One NAND, based on tuxuser's original [NANDOne](https://github.com/tuxuser/NANDOne) work with a few small additions.

### Usage
Usage  : xvdtool.exe [parameters] [filename]

Parameters:

    -h (-help) - print xvdtool usage
    -i (-info) - print info about package
    -wi (-writeinfo) - write info about package to [filename].txt
    -o (-output) <output-path> - specify output filename
    -nd (-nodatahash) - disable data hash checking, speeds up -l and -f
    -ne (-noextract) - disable data (embedded XVD/user data) extraction, speeds up -l and -f
    -nn (-nonatives) - disable importing native windows functions (ncrypt etc)
        note that signature verification/resigning won't work with this!

    -eu (-decrypt) = decrypt output xvd
    -ee (-encrypt) [keyid] = encrypt output xvd
        (optional keyid param for XVCs to choose which key inside cik_keys.bin to use)
        XVDs will have a new CIK generated, which will be encrypted with the odk_key.bin and stored in the XVD header

    -hd (-removehash) - remove hash tree/data integrity from package
    -he (-addhash) - add hash tree/data integrity to package

    -r (-rehash) - fix data integrity hashes inside package
    -rs (-resign) - sign package using the private key from rsa3_key.bin

    -xe (-extractembedded) <output-file> - extract embedded XVD from package
    -xu (-extractuserdata) <output-file> - extract user data from package
    -xv (-extractvhd) <output-vhd> - extracts filesystem from XVD into a VHD file, doesn't seem to work properly with XVC packages yet (also removes NTFS compression from output VHD so Windows can mount it, use -nn to disable)

    The next two commands will write info about each package found to [filename].txt
    also extracts embedded XVD and user data to [filename].exvd.bin / [filename].userdata.bin
    -l (-filelist) <path-to-file-list> - use each XVD specified in the list
    -f (-folder) <path-to-folder> - scan folder for XVD files

    Note that to mount an XVD/XVC in Windows you'll have to decrypt it and remove the hash tables first (-eu -hd)

To decrypt non-XVC packages you'll need the correct ODK, this key should be saved as odk_key.bin in the same folder as xvdtool. The devkit ODK is "widely known" and a MD5 hash is provided below, but as mentioned above the retail key is currently unknown.

Decrypting XVC packages is a different matter, XVC packages use a **CIK (Content Integrity Key)** which appears to be stored somewhere outside the package, however where and how it's stored is currently unknown. If you have the correct deobfuscated CIK for a given package you should be able to save it as cik_key.bin to decrypt the package.

Devkit/test-signed XVC packages use a static CIK which is also "widely known" (MD5 hash provided below), this key should be saved as cik_key.bin.

### Required Files
To make full use of this tool you'll need the following files, which **are not included**. The tool will work fine without them, but some functions might not work.

If you have an Xbox One development kit installed xvdtool will also try to extract the keys from there.

- cik_keys.bin (CIK keys for XVC crypto, first entry should be the key used by SDK tools/devkits), format: [16 byte encryption key GUID][32 byte CIK]
~~~
md5sum: C9E58F4E1DC611E110A849648DADCC9B
~~~
- odk_key.bin (ODK key used by SDK tools/devkits), format: [32 byte ODK]
~~~
md5sum: A2BCFA87F6F83A560BD5739586A5D516
~~~
- rsa3_key.bin (RSA key used by SDK tools to sign packages), format: RSAFULLPRIVATEBLOB struct
~~~
md5sum: 2DC371F46B67E29FFCC514C5B134BF73
~~~

These files should be placed in the same folder as xvdtool.exe, or at the root of any drive in your system.

### What are XVDs?
XVD packages are a secured file format used by the Xbox One to store data, an analogue to the Xbox 360's STFS packages. XVD files are usually used to store system images/data while XVCs (a slightly modified variant of XVDs) are used to store game data.

For a more detailed explanation of XVD files see xvd_info.md

### Help / Support
xvdtool has only been tested on Windows but it might work on other systems via Mono. It does use some Windows imports to create/verify signatures but these imports can be disabled with the -nn parameter.

There's no help given for this tool besides this readme, it's also currently **very** experimental and **very** likely to blow up in your face. If you do encounter any bugs please submit a description of what happened to the issue tracker.

If you want to help out with development feel free, just make a fork of this repo, make your changes in a new branch of that fork and then submit a pull request from that branch to the master branch of this repo.
