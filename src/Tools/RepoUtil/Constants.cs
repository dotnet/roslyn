// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace RepoUtil
{
    internal static class Constants
    {
        /// <summary>
        /// NuGet package names are not case sensitive.
        /// </summary>
        internal static readonly StringComparer NugetPackageNameComparer = StringComparer.OrdinalIgnoreCase;

        /// <summary>
        /// NuGet package versions case sensitivity is not documented anywhere that could be found.  Assuming
        /// case insensitive for now.
        /// </summary>
        internal static readonly StringComparer NugetPackageVersionComparer = StringComparer.OrdinalIgnoreCase;

        internal struct IgnoreGenerateNameComparer : IEqualityComparer<NuGetPackage>
        {
            public bool Equals(NuGetPackage x, NuGetPackage y) =>
                x.Name.Equals(y.Name, StringComparison.OrdinalIgnoreCase) &&
                x.Version.Equals(y.Version, StringComparison.OrdinalIgnoreCase);

            public int GetHashCode(NuGetPackage obj) => obj.GetHashCode();
        }
    }
}
