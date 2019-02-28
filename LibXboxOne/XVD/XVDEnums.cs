using System;

namespace LibXboxOne
{
    public enum XvdContentType : uint
    {
        Data = 0,
        Title = 1,
        SystemOS = 2,
        EraOS = 3,
        Scratch = 4,
        ResetData = 5,
        Application = 6,
        HostOS = 7,
        X360STFS = 8,
        X360FATX = 9,
        X360GDFX = 0xA,
        Updater = 0xB,
        OfflineUpdater = 0xC,
        Template = 0xD,
        MteHost = 0xE,
        MteApp = 0xF,
        MteTitle = 0x10,
        MteEraOS = 0x11,
        EraTools = 0x12,
        SystemTools = 0x13,
        SystemAux = 0x14,
        SomethingSomething = 0x15,
        Codec = 0x16,
        Qaslt = 0x17,
        AppDlc = 0x18,
        TitleDlc = 0x19,
        UniversalDlc = 0x1A,
        SystemData = 0x1B,
        Test = 0x1C
    }

    [Flags]
    public enum XvcRegionFlags : uint
    {
        Resident = 1,
        InitialPlay = 2, // might be 4, or maybe InitialPlay stuff in XvcInfo struct should be swapped with Preview
        Preview = 4,
        FileSystemMetadata = 8
    }

    [Flags]
    public enum XvdVolumeFlags : uint
    {
        ReadOnly = 1,
        EncryptionDisabled = 2, // data decrypted, no encrypted CIKs
        DataIntegrityDisabled = 4, // unsigned and unhashed
        LegacySectorSize = 8,
        ResiliencyEnabled = 0x10,
        SraReadOnly = 0x20,
        RegionIdInXts = 0x40,
        EraSpecific = 0x80
    }
}
