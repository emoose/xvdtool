using System;

namespace LibXboxOne.Vhd
{
    public enum VhdDiskFeatures : uint
    {
        None = 0x00000000,
        TemporaryDisk = 0x00000001,
        Reserved = 0x00000002
    }
}