// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.GenerateEqualsAndGetHashCodeFromMembers
{
    internal static class GenerateEqualsAndGetHashCodeFromMembersOptions
    {
        public static readonly PerLanguageOption2<bool> GenerateOperators = new(
            nameof(GenerateEqualsAndGetHashCodeFromMembersOptions),
            nameof(GenerateOperators), defaultValue: false,
            storageLocation: new RoamingProfileStorageLocation(
                $"TextEditor.%LANGUAGE%.Specific.{nameof(GenerateEqualsAndGetHashCodeFromMembersOptions)}.{nameof(GenerateOperators)}"));

        public static readonly PerLanguageOption2<bool> ImplementIEquatable = new(
            nameof(GenerateEqualsAndGetHashCodeFromMembersOptions),
            nameof(ImplementIEquatable), defaultValue: false,
            storageLocation: new RoamingProfileStorageLocation(
                $"TextEditor.%LANGUAGE%.Specific.{nameof(GenerateEqualsAndGetHashCodeFromMembersOptions)}.{nameof(ImplementIEquatable)}"));
    }
}
