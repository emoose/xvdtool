using System;

namespace LibXboxOne
{
    [Flags]
    public enum XvdMountFlags : int
    {
        None = 0x0,
        ReadOnly = 0x1,
        Boot = 0x2, // Used with XvdVmMount
        MountEmbeddedXvd = 0x8,
        AsRemovable = 0x20
    }

    public class XvdMount
    {
        public static bool MountXvd(string filepath, string mountPoint, XvdMountFlags flags=0)
        {
            // Setup Xvd handle
            IntPtr pHandle = IntPtr.Zero;
            ulong result = Natives.XvdOpenAdapter(out pHandle);
            
            if (result != 0x10000000)
            {
                Console.WriteLine("XvdOpenAdapter failed. Result: 0x{0:X}", result);
                return false;
            }
            
            IntPtr pDiskHandle = IntPtr.Zero;
            int diskNum = 0;
            result = Natives.XvdMount(out pDiskHandle, out diskNum, pHandle, filepath, 0, mountPoint, (int)flags);

            // Check for errors
            if (result == 0x80070002)
            {
                Console.WriteLine("Failed to find XVD file!");
            }
            else if (result == 0xC0000043)
            {
                Console.WriteLine("Xvd file is already mounted or being used by another process!");
            }
            else if (result != 0)
            {
                Console.WriteLine("XvdMount error: 0x{0:X}", result);
            }
            else
            {
                Console.WriteLine($"Package {filepath} attached as disk number {diskNum}");
            }

            Natives.XvdCloseAdapter(pHandle);
            return (result == 0);
        }

        public static bool UnmountXvd(string filepath)
        {
            // Setup XVD Handle
            IntPtr pHandle = IntPtr.Zero;
            ulong result = Natives.XvdOpenAdapter(out pHandle);

            if (result != 0x10000000)
            {
                Console.WriteLine("XvdOpenAdapter failed. Result: 0x{0:X}", result);
                return false;
            }

            // UnMount XVD file
            result = Natives.XvdUnmountFile(pHandle, filepath);

            //Check for errors
            if (result == 0x80070002)
            {
                Console.WriteLine("Failed to find XVD file!");
            }
            else if (result != 0)
            {
                Console.WriteLine("XvdMount error: 0x{0:X}", result);
            }

            Natives.XvdCloseAdapter(pHandle);
            return (result == 0);
        }
    }
}
