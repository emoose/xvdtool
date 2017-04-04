namespace LibXboxOne
{
    public enum XvdContentType : uint
    {
        // ReSharper disable InconsistentNaming
        Data = 0,
        GameContainer = 1,
        SystemOS = 2, // system.xvd / sharedOS
        EraOS = 3, // era.xvd / exclusiveOS
        Scratch = 4,
        ResetData = 5,
        Application = 6,
        HostOS = 7, // host.xvd / hostOS
        // 8
        // 9
        // 0xA
        Updater = 0xB,
        UpdaterAlt = 0xC, // some updater.xvd files use this
        Template = 0xD, // sostmpl.xvd SettingsTemplate.xvd
        // 0xE
        // 0xF
        // 0x10
        // 0x11
        // 0x12
        SystemTools = 0x13,
        SystemAux = 0x14,
        // 0x15
        // 0x16
        // 0x17
        AppDLC = 0x18, // downloadable content for an application
        GameDLC = 0x19, // downloadable content for a game title
        UniversalDLC = 0x1A // dowloadable content not associated with an application or game
        // ReSharper restore InconsistentNaming
    }

    public enum XvdContentTypeNew : uint
    {
        ContentTypeData = 0,
        ContentTypeTitle = 1,
        ContentTypeSystemOS = 2,
        ContentTypeEraOS = 3,
        ContentTypeScratch = 4,
        ContentTypeResetData = 5,
        ContentTypeApplication = 6,
        ContentTypeHostOS = 7,
        ContentTypeX360STFS = 8,
        ContentTypeX360FATX = 9,
        ContentTypeX360GDFX = 0xA,
        ContentTypeUpdater = 0xB,
        ContentTypeOfflineUpdater = 0xC,
        ContentTypeTemplate = 0xD,
        ContentTypeMteHost = 0xE,
        ContentTypeMteApp = 0xF,
        ContentTypeMteTitle = 0x10,
        ContentTypeMteEraOS = 0x11,
        ContentTypeEraTools = 0x12,
        ContentTypeSystemTools = 0x13,
        ContentTypeSystemAux = 0x14,
        ContentTypeSomethingSomething = 0x15,
        ContentTypeCodec = 0x16,
        ContentTypeQaslt = 0x17,
        ContentTypeAppDlc = 0x18,
        ContentTypeTitleDlc = 0x19,
        ContentTypeUniversalDlc = 0x1A,
        ContentTypeSystemData = 0x1B,
        ContentTypeTest = 0x1C
    }

    public enum XvcRegionFlags : uint
    {
        Resident = 1,
        InitialPlay = 2, // might be 4, or maybe InitialPlay stuff in XvcInfo struct should be swapped with Preview
        Preview = 4,
        FileSystemMetadata = 8
    }

    public enum XvdVolumeFlags : uint
    {
        ReadOnly = 1,
        EncryptionDisabled = 2, // data decrypted, no encrypted CIKs
        DataIntegrityDisabled = 4, // unsigned and unhashed
        SystemFile = 8, // only observed in system files
        Unknown = 0x40, // unsure, never set on unsigned/unhashed files
    }
}
