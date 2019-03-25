using System;
using System.IO;
using System.Collections.Generic;
using Xunit;
using System.Threading.Tasks;

namespace LibXboxOne.Tests
{
    public enum ResourceType
    {
        RsaKeys,
        AesKeys,
        Rc4Keys,
        DataBlobs,
        TestFiles,
        Misc
    }

    public class ResourcesProvider
    {
        static readonly string ResourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../Resources");

        public static byte[] GetBytes(string fileName, ResourceType type = ResourceType.Misc)
        {
            var file = $"{ResourcePath}/{type}/{fileName}";
            if (File.Exists(file))
            {
                return File.ReadAllBytes(file);
            }
            throw new FileNotFoundException(file);
        }

        public static async Task<byte[]> GetBytesAsync(string fileName, ResourceType type = ResourceType.Misc)
        {
            var file = $"{ResourcePath}/{type}/{fileName}";
            if (File.Exists(file))
            {
                using (FileStream stream = File.OpenRead(file))
                {
                    byte[] result = new byte[stream.Length];
                    await stream.ReadAsync(result, 0, (int)stream.Length);
                    return result;
                }
            }
            throw new FileNotFoundException(file);
        }
        public static string GetString(string fileName, ResourceType type = ResourceType.Misc)
        {
            var file = $"{ResourcePath}/{type}/{fileName}";
            if (File.Exists(file))
            {
                return System.Text.Encoding.UTF8.GetString(
                    File.ReadAllBytes(file)
                );
            }
            throw new FileNotFoundException(file);
        }
    }
}