using System;

namespace LibXboxOne.Vhd
{
    public static class VhdCreatorHostOs
    {
        public static readonly byte[] Macintosh = {(byte)'M',(byte)'a',(byte)'c',0x20};
        public static readonly byte[] Windows = {(byte)'W',(byte)'i',(byte)'2',(byte)'k'};
    }
}