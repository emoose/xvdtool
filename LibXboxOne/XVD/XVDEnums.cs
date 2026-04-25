using System;

namespace LibXboxOne
{
    public enum XvdType : uint
    {
        Fixed = 0,
        Dynamic = 1
    }

    public enum XvdContentType : uint
    {
        Data = 0x00,
        Title = 0x01,
        SystemOS = 0x02,
        EraOS = 0x03,
        Scratch = 0x04,
        ResetData = 0x05,
        Application = 0x06,
        HostOS = 0x07,
        X360STFS = 0x08,
        X360FATX = 0x09,
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
        AcousticModel = 0x15,
        SystemCodecsVolume = 0x16,
        QasltPackage = 0x17,
        AppDlc = 0x18,
        TitleDlc = 0x19,
        UniversalDlc = 0x1A,
        SystemDataVolume = 0x1B,
        TestVolume = 0x1C,
        HardwareTestVolume = 0x1D,
        KioskContent = 0x1E,
        DevPackage = 0x1F,
        HostProfiler = 0x20,
        Roamable = 0x21,
        ThinProvisioned = 0x22,
        StreamingOnlySRA = 0x23,
        StreamingOnlyERA = 0x24,
        StreamingOnlyHost = 0x25,
        QuickResume = 0x26,
        HostData = 0x27
    }

    [Flags]
    public enum XvcRegionFlags : uint
    {
        Resident = 1,
        InitialPlay = 2,
        Preview = 4,
        FileSystemMetadata = 8,
        Present = 0x10,
        OnDemand = 0x20,
        Available = 0x40,
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

    [Flags]
    public enum XvcRegionPresenceInfo : byte
    {
        IsPresent = 1, // not set = "not present"
        IsAvailable = 2, // not set = "unavailable"

        //value >> 4 = discnum
        Disc1 = 0x10,
        Disc2 = 0x20,
        Disc3 = 0x30,
        Disc4 = 0x40,
        Disc5 = 0x50,
        Disc6 = 0x60,
        Disc7 = 0x70,
        Disc8 = 0x80,
        Disc9 = 0x90,
        Disc10 = 0xA0,
        Disc11 = 0xB0,
        Disc12 = 0xC0,
        Disc13 = 0xD0,
        Disc14 = 0xE0,
        Disc15 = 0xF0
    }
}
