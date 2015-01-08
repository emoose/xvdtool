### What are XVDs?
XVD packages are a secured file format used by the Xbox One to store data, an advancement of the Xbox 360's STFS packages. XVD files are usually used to store system images/data while XVCs (a slightly modified version of XVDs) are used to store game data.

An XVD file consists of a header containing the hash of the top level hash table, certain metadata about the file (such as content ID, content type, the sandbox ID the package was created for, the product ID that the package belongs to, etc) and also a signature of the header itself which is stored at the beginning of the XVD file, from 0h to 200h.

After the header there's space for an optional embedded XVD (which is usually the GameOS partition that the game inside the XVC is coded against). This embedded XVD is just a copy-paste of the XVD being embedded with no changes made to it.

After the embedded XVD comes an (optional) area for the hash tree, the tree is a multilevel array of hashes, with the topmost levels containing the hashes of the blocks in the levels underneath it. At the lowest level of the tree hashes of the data blocks (the blocks following the hash tree) are made. The hashes in the hash tree are computed using SHA-256, with the result usually being resized to 18h bytes (but for data block hashes can be slightly modified depending on flags in the XVD header and the XVC region that the data being hashed is located). The full 256-bit hash of the top-most level is stored in the XVD header.

Following the hash tree is another optional area reserved for user data (also known as Persistent Local Storage). This area is for games to store local-only data, although some system XVD packages seem to store data here too.

Finally after the user data comes the actual XVD data. If the XVD file is an XVC the first 3 blocks are reserved for an XVC descriptor (which is never encrypted). This specifies the content ID, any encryption keys used, the chunks used to update packages (if the XVC is using chunk-based updates) and offsets/lengths/keyIDs of the different XVC regions in the file, along with other metadata. XVC regions can be encrypted with any of the keys specified in the XVC descriptor. The region-based encryption also includes the XVC region ID as part of the AES-128-CTR IV/counter.

Then comes the actual filesystem data. This data is encrypted with the CIK (not sure what it stands for, CIK can either be the decrypted value of the encrypted CIK in the XVD header if it's an XVD, or the key corresponding to the XVC key GUID.)

The filesystem data is just a normal NTFS filesystem containing the files inside the package, other filesystems may be possible but NTFS is the only one observed so far.

### Security Overview
From a security standpoint XVDs are provably secure:

- Each block of data is hashed, with the hash stored in the bottom-most hash tree level
- Each block in that hash tree level is then hashed with the hash result stored in the level above it
- This continues, until eventually the number of hashes in the level can fit into one hash block
- That hash block is then hashed with the result stored in the XVD header
- The XVD header is then hashed, and the hash signed with Microsoft's private key, the signature is then stored at the beginning of the file.

To make sure the package is authenticated by Microsoft and not tampered with the console just needs to verify the signature of the header-hash, verify the top-most hash tree hash and then verify that each hash in the hash tree matches up with the actual hash. This is similar to the way STFS packages were secured on the Xbox 360, however instead of having the hash tables scattered around the file (as with STFS) they're instead stored before the data actually begins.

The data blocks inside XVD files are also secured with customized AES-128-CTR encryption (the encrypted data is then used for the hashes), with XVC packages the Xbox One either retrieves the encryption key over Xbox Live or retrieves it from the game disc, however it seems that the keys from these methods don't work as CIK keys. It's assumed that these keys are obfuscated/encrypted in some way (possibly with the retail ODK in the same way that the encrypted CIK in non-XVC files is encrypted?)

Non-XVC files use an ODK (origin decryption key?) which appears to be static for all XVDs (but differs between retail/devkits ?), this key is used to decrypt the encrypted CIK in the XVD header, the decrypted CIK is then used to decrypt the XVD data.