using System;

namespace LibXboxOne.Vhd
{
    public static class VhdCreatorApplication
    {
        public static readonly byte[]  Disk2Vhd = {(byte)'d', (byte)'2', (byte)'v', 0x00};
        public static readonly byte[]  QEMU = {(byte)'q',(byte)'e',(byte)'m',(byte)'u'};
        public static readonly byte[]  VirtualPC = {(byte)'v',(byte)'p',(byte)'c',0x20};
        public static readonly byte[]  VirtualServer = {(byte)'v',(byte)'s',0x20,0x20};
        public static readonly byte[]  WindowsDiskMngmt = {(byte)'w',(byte)'i',(byte)'n',0x20};
    }
}