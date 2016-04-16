// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
                var parts = reference.Substring(ReferencePrefix.Length).Split('/');
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
