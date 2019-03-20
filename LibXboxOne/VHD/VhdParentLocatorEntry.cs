using System;
using System.IO;
using System.Runtime.InteropServices;

namespace LibXboxOne.Vhd
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VhdParentLocatorEntry
    {
        /* 0x0  */ public uint Code;
        /* 0x4  */ public uint DataSpace;
        /* 0x8  */ public uint DataLength;
        /* 0xC  */ public uint Reserved;
        /* 0x10  */ public ulong DataOffset;
    }
}