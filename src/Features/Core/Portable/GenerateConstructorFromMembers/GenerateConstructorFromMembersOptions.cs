// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.GenerateConstructorFromMembers
{
    internal static class GenerateConstructorFromMembersOptions
    {
        public static readonly PerLanguageOption2<bool> AddNullChecks = new PerLanguageOption2<bool>(
            nameof(GenerateConstructorFromMembersOptions),
            nameof(AddNullChecks), defaultValue: false,
            storageLocations: new RoamingProfileStorageLocation(
                $"TextEditor.%LANGUAGE%.Specific.{nameof(GenerateConstructorFromMembersOptions)}.{nameof(AddNullChecks)}"));
    }
}
