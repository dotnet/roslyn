// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Reflection.Metadata;

namespace Microsoft.CodeAnalysis
{
    internal static class MetadataReaderExtensions
    {
        internal static bool GetWinMdVersion(this MetadataReader reader, out int majorVersion, out int minorVersion)
        {
            if (reader.MetadataKind == MetadataKind.WindowsMetadata)
            {
                // Name should be of the form "WindowsRuntime {major}.{minor}".
                const string prefix = "WindowsRuntime ";
                string version = reader.MetadataVersion;
                if (version.StartsWith(prefix, StringComparison.Ordinal))
                {
                    var parts = version.Substring(prefix.Length).Split('.');
                    if ((parts.Length == 2) &&
                        int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out majorVersion) &&
                        int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out minorVersion))
                    {
                        return true;
                    }
                }
            }

            majorVersion = 0;
            minorVersion = 0;
            return false;
        }
    }
}
