// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.ReferenceHighlighting
{
    internal static class DocumentHighlightsOptions
    {
        private const string LocalRegistryPath = @"Roslyn\Features\DocumentHighlightsOptions\";

        public static readonly Option<bool> OutOfProcessAllowed = new Option<bool>(
            nameof(DocumentHighlightsOptions), nameof(OutOfProcessAllowed), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(OutOfProcessAllowed)));
    }
}