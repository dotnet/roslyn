// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal static class SymbolFinderOptions
    {
        private const string LocalRegistryPath = @"Roslyn\Features\SymbolFinder\";

        public static readonly Option<bool> OutOfProcessAllowed = new Option<bool>(
            nameof(SymbolFinderOptions), nameof(OutOfProcessAllowed), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(OutOfProcessAllowed)));
    }
}