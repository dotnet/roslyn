// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    internal abstract class NuGetPackageResolver
    {
        private const string ReferencePrefix = "nuget:";

        /// <summary>
        /// Syntax is "nuget:name[/version]".
        /// </summary>
        internal static bool TryParsePackageReference(string reference, out string name, out string version)
        {
            if (reference.StartsWith(ReferencePrefix, StringComparison.Ordinal))
            {
                var parts = reference[ReferencePrefix.Length..].Split('/');
                Debug.Assert(parts.Length > 0);
                name = parts[0];
                if (name.Length > 0)
                {
                    switch (parts.Length)
                    {
                        case 1:
                            version = string.Empty;
                            return true;
                        case 2:
                            version = parts[1];
                            if (version.Length > 0)
                            {
                                return true;
                            }
                            break;
                    }
                }
            }
            name = null;
            version = null;
            return false;
        }

        internal abstract ImmutableArray<string> ResolveNuGetPackage(string packageName, string packageVersion);
    }
}
