using System;

namespace LibXboxOne
{
    public enum XvcRegionId : uint
    {
        MetadataXvc = 0x40000001,
        MetadataFilesystem = 0x40000002,
        Unknown = 0x40000003,
        EmbeddedXvd = 0x40000004,
        Header = 0x40000005,
        Mdu = 0x40000006
    }
}