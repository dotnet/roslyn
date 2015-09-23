// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    internal abstract class NuGetPackageResolver
    {
        private const string ReferencePrefix = "nuget:";

        /// <summary>
        /// Syntax is "nuget:id/version".
        /// </summary>
        internal static bool TryParsePackageReference(string reference, out string name, out string version)
        {
            if (reference.StartsWith(ReferencePrefix, StringComparison.Ordinal))
            {
                var parts = reference.Substring(ReferencePrefix.Length).Split('/');
                if ((parts.Length == 2) &&
                    (parts[0].Length > 0) &&
                    (parts[1].Length > 0))
                {
                    name = parts[0];
                    version = parts[1];
                    return true;
                }
            }
            name = null;
            version = null;
            return false;
        }

        internal abstract ImmutableArray<string> ResolveNuGetPackage(string packageName, string packageVersion);
    }
}
