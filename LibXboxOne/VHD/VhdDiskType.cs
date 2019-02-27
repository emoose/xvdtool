using System;

namespace LibXboxOne.Vhd
{
    public enum VhdDiskType : uint
    {
        None = 0,
        Reserved_Deprecated1 = 1,
        Fixed = 2,
        Dynamic = 3,
        Differential = 4,
        Reserved_Deprecated2 = 5,
        Reserved_Deprecated3 = 6
    }
}