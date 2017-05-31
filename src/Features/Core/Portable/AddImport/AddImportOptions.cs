// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.AddImport
{
    internal static class AddImportOptions
    {
        private const string LocalRegistryPath = @"Roslyn\Features\AddImport\";

        public static readonly Option<bool> OutOfProcessAllowed = new Option<bool>(
            nameof(AddImportOptions), nameof(OutOfProcessAllowed), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(OutOfProcessAllowed)));
    }
}