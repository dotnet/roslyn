// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ImplementType
{
    internal enum ImplementTypeInsertionBehavior
    {
        WithOtherMembersOfTheSameKind = 0,
        AtTheEnd = 1,
    }

    internal enum ImplementTypePropertyGenerationBehavior
    {
        PreferThrowingProperties = 0,
        PreferAutoProperties = 1,
    }

    internal static class ImplementTypeOptions
    {
        public static readonly PerLanguageOption2<ImplementTypeInsertionBehavior> InsertionBehavior =
            new PerLanguageOption2<ImplementTypeInsertionBehavior>(
                nameof(ImplementTypeOptions),
                nameof(InsertionBehavior),
                defaultValue: ImplementTypeInsertionBehavior.WithOtherMembersOfTheSameKind,
                storageLocations: new RoamingProfileStorageLocation(
                    $"TextEditor.%LANGUAGE%.{nameof(ImplementTypeOptions)}.{nameof(InsertionBehavior)}"));

        public static readonly PerLanguageOption2<ImplementTypePropertyGenerationBehavior> PropertyGenerationBehavior =
            new PerLanguageOption2<ImplementTypePropertyGenerationBehavior>(
                nameof(ImplementTypeOptions),
                nameof(PropertyGenerationBehavior),
                defaultValue: ImplementTypePropertyGenerationBehavior.PreferThrowingProperties,
                storageLocations: new RoamingProfileStorageLocation(
                    $"TextEditor.%LANGUAGE%.{nameof(ImplementTypeOptions)}.{nameof(PropertyGenerationBehavior)}"));

    }
}
