# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
- XvdFile: Move duplicated "get hash entry offset" code to its own function
- XvdFile: Improve VerifyXvcHash & fix AddHashTree, remove unneeded copy constructors for structs
- XvdFile: Add RemoveMutableData function & -removemdu tool option
- XvdFile: Move data-removal logic from RemoveHashTree into seperate RemoveData function + add fixups for XvdUpdateSegments
- XvdFile: Allow fetching dataUnit from hashtables during CryptSectionXts
- General: Start using CHANGELOG.md
- XVDTool: For XVC decryption, iterate through all loaded CIKs to find a matching one

## [0.51] - 2019-03-21
### Added
- General: Ship release archives with README.md

### Changed
- General: Release archive is packed without a subfolder

## [0.5] - 2019-03-21
### Added
- DurangoKeyExtractor: Add tool to extract known keys from binaries
- Tests: Added tests for hash block related functions
- XVDTool: Add Xvc enums / structs
- XVDTool: Enable raw filesystem extraction, rework VHD extraction
- XVDTool: Add XvdMountFlags
- XVDTool: Name previously unknown XvdMount function arguments
- XVDTool: Add additional xsapi.dll XvdMount interop
- XVDTool: Enable loading keys from global/local config directory
- XVDTool: Enable supplying CIK/ODK/SignKey via cmdline
- XVDTool: Helper functions to calculate data offsets
- XBFSTool: Updated filetable
- XBFSTool: Enable rehashing XBFS table
- XBFSTool: Parsing of console certificate

### Changed
- General: Retarget solution to .NET Core 2.0
- XVDTool: Use BouncyCastle for RSA signature calculation/verification

### Fixed
- XVDTool: Parsing of MSIXVC header (XvcInfo version 2)

### Removed
- XVDTool: The -nn switch to disable native functions, only xvd mounting requires windows specific interop
- Tests: Temporarily disable xvd file tests that rely on big binary blobs

## [0.4] - 2015-01-08
### Added
- Initial release
