// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.GenerateConstructorFromMembers
{
    internal static class GenerateConstructorFromMembersOptions
    {
        public static readonly PerLanguageOption<bool> AddNullChecks = new PerLanguageOption<bool>(
            nameof(GenerateConstructorFromMembersOptions),
            nameof(AddNullChecks), defaultValue: false,
            storageLocations: new RoamingProfileStorageLocation(
                $"TextEditor.%LANGUAGE%.Specific.{nameof(GenerateConstructorFromMembersOptions)}.{nameof(AddNullChecks)}"));
    }
}
