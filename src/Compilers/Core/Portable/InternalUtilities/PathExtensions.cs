// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Roslyn.Utilities;

internal static class PathExtensions
{
#if !NET
    extension(Path)
    {
        /// <summary>
        /// Polyfill for Path.IsPathFullyQualified(string)
        /// </summary>
        public static bool IsPathFullyQualified(string path)
        {
            if (path is null)
                throw new ArgumentNullException(nameof(path));

            if (path.Length == 0)
            {
                return false;
            }

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Unix has no drive-relative concept: rooted implies fully qualified.
                return path[0] == '/';
            }

            if (path.Length < 2)
            {
                // No way to specify a fixed path in one character or less.
                return false;
            }

            if (isDirectorySeparator(path[0]))
            {
                // Two leading slashes is UNC or device (\\?\); a single slash followed
                // by '?' is \??\, which is equivalent to \\?\. '?' is not legal in a
                // drive-relative path, so both forms are fully qualified.
                return path[1] == '?' || isDirectorySeparator(path[1]);
            }

            // Otherwise the only fully qualified form is drive + colon + separator (C:\).
            // The drive letter is validated to match legacy behavior: "=:\" is the
            // default data stream of a file named "=", not a rooted path.
            return path.Length >= 3
                && path[1] == ':'
                && isDirectorySeparator(path[2])
                && isValidDriveChar(path[0]);

            static bool isDirectorySeparator(char value)
                => value == Path.DirectorySeparatorChar || value == Path.AltDirectorySeparatorChar;

            static bool isValidDriveChar(char value)
                => (uint)((value | 0x20) - 'a') <= 'z' - 'a';
        }
    }
#endif

}