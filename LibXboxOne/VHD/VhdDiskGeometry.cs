using System;

namespace LibXboxOne.Vhd
{
    public struct VhdDiskGeometry
    {
        public ushort Cylinder;
        public byte Heads;
        public byte SectorsPerCylinder;
    }
}