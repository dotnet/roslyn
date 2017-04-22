// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.FindUsages
{
    internal static class FindUsagesOptions
    {
        private const string LocalRegistryPath = @"Roslyn\Features\FindUsages\";

        public static readonly Option<bool> OutOfProcessAllowed = new Option<bool>(
            nameof(FindUsagesOptions), nameof(OutOfProcessAllowed), defaultValue: false,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(OutOfProcessAllowed)));
    }
}