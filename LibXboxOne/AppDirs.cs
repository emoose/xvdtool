using System;
using System.IO;
using System.Runtime.InteropServices;

namespace LibXboxOne
{
    public static class AppDirs
    {
        internal static string GetApplicationBaseDirectory()
        {
            string baseDir;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                /*
                 * Windows
                 * Result: C:\Users\<username>\AppData\Local
                 */
                baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                /*
                 * Mac OS X
                 * Result: /Users/<username>/.config
                 */
                baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            }
            else
            {
                /*
                 * Linux
                 * Assemble application config directory manually, SpecialFolder.* is not
                 * really prepared for Linux yet it seems...
                 *
                 * Result: /home/<username>/.config
                 */
                baseDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                baseDir = Path.Combine(baseDir, ".config");
            }

            return baseDir;
        }

        public static string GetApplicationConfigDirectory(string appName)
        {
            /*
             * Windows: C:\Users\<username>\AppData\Local\<appName>
             * Linux: /home/<username>/.config/<appName>
             * Mac OS X: /Users/<username>/.config/<appName>
             */
            return Path.Combine(GetApplicationBaseDirectory(), appName);
        }
    }
}