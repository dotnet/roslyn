// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Roslyn.Utilities;

namespace BuildValidator
{
    internal static class IldasmUtilities
    {
        private static string GetIldasmPath()
        {
            var ildasmExeName = PlatformInformation.IsWindows ? "ildasm.exe" : "ildasm";
            var directory = Path.GetDirectoryName(typeof(IldasmUtilities).GetTypeInfo().Assembly.Location) ?? throw new DirectoryNotFoundException();
            string ridName;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ridName = "win-x64";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                ridName = "osx-x64";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var architecture = Microsoft.CodeAnalysis.Test.Utilities.IlasmUtilities.Architecture;
                ridName = $"linux-{architecture}";
            }
            else
            {
                throw new PlatformNotSupportedException();
            }

            return Path.Combine(directory, "runtimes", ridName, "native", ildasmExeName);
        }

        internal static readonly string IldasmPath = GetIldasmPath();
    }
}
