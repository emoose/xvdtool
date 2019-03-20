using System;

namespace LibXboxOne.Vhd
{
    public static class VhdUtils
    {
        const uint VHD_SECTOR_LENGTH = 512;
        static readonly DateTime VhdEpoch = new DateTime(2000, 1, 1, 1, 0, 0, DateTimeKind.Utc);
        public static uint GetTimestamp(DateTime dateTime)
        {
            return (uint)dateTime.Subtract(VhdEpoch).TotalSeconds;
        }

        // Source: https://github.com/ctatoiu/PS-Azure/blob/master/src/ServiceManagement/Compute/VhdManagement/Model/DiskGeometry.cs
        public static VhdDiskGeometry CalculateDiskGeometry(ulong driveSize)
        {
            long totalSectors = (long)(driveSize / VHD_SECTOR_LENGTH);
            if (totalSectors > 65535 * 16 * 255)
            {
                totalSectors = 65535 * 16 * 255;
            }

            int sectorsPerTrack;
            int heads;
            long cylinderTimesHeads;
            if (totalSectors >= 65535 * 16 * 63)
            {
                sectorsPerTrack = 255;
                heads = 16;
                cylinderTimesHeads = totalSectors / sectorsPerTrack;
            }
            else
            {
                sectorsPerTrack = 17;
                cylinderTimesHeads = totalSectors / sectorsPerTrack;

                heads = (int)((cylinderTimesHeads + 1023) / 1024);

                if (heads < 4)
                {
                    heads = 4;
                }
                if (cylinderTimesHeads >= (heads * 1024) || heads > 16)
                {
                    sectorsPerTrack = 31;
                    heads = 16;
                    cylinderTimesHeads = totalSectors / sectorsPerTrack;
                }
                if (cylinderTimesHeads >= (heads * 1024))
                {
                    sectorsPerTrack = 63;
                    heads = 16;
                    cylinderTimesHeads = totalSectors / sectorsPerTrack;
                }
            }
            long cylinders = cylinderTimesHeads / heads;

            return new VhdDiskGeometry()
            {
                Cylinder = (ushort)cylinders,
                Heads = (byte)heads,
                SectorsPerCylinder = (byte)sectorsPerTrack
            };
        }
    }
}