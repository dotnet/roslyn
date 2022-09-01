// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.SymbolSearch
{
    internal sealed class SymbolSearchGlobalOptions
    {
        private const string LocalRegistryPath = @"Roslyn\Features\SymbolSearch\";
        private const string FeatureName = "SymbolSearchOptions";

        public static readonly Option2<bool> Enabled = new(
            FeatureName, "Enabled", defaultValue: true,
            storageLocation: new LocalUserProfileStorageLocation(LocalRegistryPath + "Enabled"));
    }
}
