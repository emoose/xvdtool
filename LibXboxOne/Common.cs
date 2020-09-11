using System;
using System.Reflection;

namespace LibXboxOne
{
    public static class Common
    {
        public static string AppVersion => 
            Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                .InformationalVersion;
    }
}