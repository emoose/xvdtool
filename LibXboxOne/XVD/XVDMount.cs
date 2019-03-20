using System;

namespace LibXboxOne
{
    public class XvdMount
    {
        public static bool MountXvd(string filepath)
        {
            // Setup Xvd handle
            IntPtr pHandle = IntPtr.Zero;
            ulong result = Natives.XvdOpenAdapter(out pHandle);
            
            if (result != 0x10000000)
            {
                Console.WriteLine("XvdOpenAdapter failed. Result: 0x{0:X}", result);
                return false;
            }
            
            int diskNum = 0;
            result = Natives.XvdMount(IntPtr.Zero, out diskNum, pHandle, filepath, 0, 0, 0);

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