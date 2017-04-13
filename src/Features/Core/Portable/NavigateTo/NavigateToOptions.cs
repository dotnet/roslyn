// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    internal static class NavigateToOptions
    {
        private const string LocalRegistryPath = @"Roslyn\Features\NavigateTo\";

        public static readonly Option<bool> OutOfProcessAllowed = new Option<bool>(
            nameof(NavigateToOptions), nameof(OutOfProcessAllowed), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(OutOfProcessAllowed)));
    }
}