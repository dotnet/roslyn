﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Structure
{
    internal static class BlockStructureOptions
    {
        public static readonly PerLanguageOption2<bool> ShowBlockStructureGuidesForCommentsAndPreprocessorRegions = new(
            nameof(BlockStructureOptions), nameof(ShowBlockStructureGuidesForCommentsAndPreprocessorRegions), defaultValue: false,
             storageLocations: new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{nameof(ShowBlockStructureGuidesForCommentsAndPreprocessorRegions)}"));

        public static readonly PerLanguageOption2<bool> ShowBlockStructureGuidesForDeclarationLevelConstructs = new(
            nameof(BlockStructureOptions), nameof(ShowBlockStructureGuidesForDeclarationLevelConstructs), defaultValue: true,
             storageLocations: new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{nameof(ShowBlockStructureGuidesForDeclarationLevelConstructs)}"));

        public static readonly PerLanguageOption2<bool> ShowBlockStructureGuidesForCodeLevelConstructs = new(
            nameof(BlockStructureOptions), nameof(ShowBlockStructureGuidesForCodeLevelConstructs), defaultValue: true,
             storageLocations: new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{nameof(ShowBlockStructureGuidesForCodeLevelConstructs)}"));

        public static readonly PerLanguageOption2<bool> ShowOutliningForCommentsAndPreprocessorRegions = new(
            nameof(BlockStructureOptions), nameof(ShowOutliningForCommentsAndPreprocessorRegions), defaultValue: true,
             storageLocations: new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{nameof(ShowOutliningForCommentsAndPreprocessorRegions)}"));

        public static readonly PerLanguageOption2<bool> ShowOutliningForDeclarationLevelConstructs = new(
            nameof(BlockStructureOptions), nameof(ShowOutliningForDeclarationLevelConstructs), defaultValue: true,
             storageLocations: new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{nameof(ShowOutliningForDeclarationLevelConstructs)}"));

        public static readonly PerLanguageOption2<bool> ShowOutliningForCodeLevelConstructs = new(
            nameof(BlockStructureOptions), nameof(ShowOutliningForCodeLevelConstructs), defaultValue: true,
             storageLocations: new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{nameof(ShowOutliningForCodeLevelConstructs)}"));

        public static readonly PerLanguageOption2<bool> CollapseRegionsWhenCollapsingToDefinitions = new(
            nameof(BlockStructureOptions), nameof(CollapseRegionsWhenCollapsingToDefinitions), defaultValue: false,
             storageLocations: new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{nameof(CollapseRegionsWhenCollapsingToDefinitions)}"));

        public static readonly PerLanguageOption2<int> MaximumBannerLength = new(
            nameof(BlockStructureOptions),
            nameof(MaximumBannerLength), defaultValue: 80,
            storageLocations: new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{nameof(MaximumBannerLength)}"));

    }
}
