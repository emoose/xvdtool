using System;

namespace LibXboxOne
{
    public class XvdMount
    {
        public static ulong MountXvd(string filepath)
        {
            // Setup Xvd handle
            IntPtr pHandle = IntPtr.Zero;
            ulong result = Natives.XvdOpenAdapter(out pHandle);
            
            if (result != 0x10000000)
            {
                Console.WriteLine("XvdOpenAdapter failed. Result: 0x{0:X}", result);
                return result;
            }
            
            int diskNum = 0;
            result = Natives.XvdMount(IntPtr.Zero, out diskNum, pHandle, filepath, 0, 0, 0);

            // Check Success or known errors
            if (result == 0)
            {
                Console.WriteLine("Successfully mounted Xvd! DiskNum: {0}", diskNum);
            }
            else if (result == 0x80070002)
            {
                Console.WriteLine("Failed to find XVD file!");
            }
            else if (result == 0xC0000043)
            {
                Console.WriteLine("Xvd file is already mounted or being used by another process!");
            }
            else
            {
                Console.WriteLine("XvdMount result: 0x{0:X}", result);
                Console.WriteLine("Failed to mount xvd!");
            }

            result = Natives.XvdCloseAdapter(pHandle);
            return result;
        }

        public static ulong UnmountXvd(string filepath)
        {
            // Setup XVD Handle
            IntPtr pHandle = IntPtr.Zero;
            ulong result = Natives.XvdOpenAdapter(out pHandle);

            if (result != 0x10000000)
            {
                Console.WriteLine("XvdOpenAdapter failed. Result: 0x{0:X}", result);
                return result;
            }

            // UnMount XVD file
            result = Natives.XvdUnmountFile(pHandle, filepath);

            //Check for Success or known errors
            if (result == 0)
            {
                Console.WriteLine("Successfully unmounted Xvd!");
            }
            else if (result == 0x80070002)
            {
                Console.WriteLine("Failed to find XVD file!");
            }
            else
            {
                Console.WriteLine("XvdUnmount result: 0x{0:X}", result);
                Console.WriteLine("Failed to unmount xvd!");
            }

            result = Natives.XvdCloseAdapter(pHandle);
            return result;
        }
    }
}