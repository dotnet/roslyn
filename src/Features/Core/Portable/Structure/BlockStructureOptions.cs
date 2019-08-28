// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Structure
{
    internal static class BlockStructureOptions
    {
        public static readonly PerLanguageOption<bool> ShowBlockStructureGuidesForCommentsAndPreprocessorRegions = new PerLanguageOption<bool>(
            nameof(BlockStructureOptions), nameof(ShowBlockStructureGuidesForCommentsAndPreprocessorRegions), defaultValue: false,
             storageLocations: new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{nameof(ShowBlockStructureGuidesForCommentsAndPreprocessorRegions)}"));

        public static readonly PerLanguageOption<bool> ShowBlockStructureGuidesForDeclarationLevelConstructs = new PerLanguageOption<bool>(
            nameof(BlockStructureOptions), nameof(ShowBlockStructureGuidesForDeclarationLevelConstructs), defaultValue: true,
             storageLocations: new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{nameof(ShowBlockStructureGuidesForDeclarationLevelConstructs)}"));

        public static readonly PerLanguageOption<bool> ShowBlockStructureGuidesForCodeLevelConstructs = new PerLanguageOption<bool>(
            nameof(BlockStructureOptions), nameof(ShowBlockStructureGuidesForCodeLevelConstructs), defaultValue: true,
             storageLocations: new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{nameof(ShowBlockStructureGuidesForCodeLevelConstructs)}"));

        public static readonly PerLanguageOption<bool> ShowOutliningForCommentsAndPreprocessorRegions = new PerLanguageOption<bool>(
            nameof(BlockStructureOptions), nameof(ShowOutliningForCommentsAndPreprocessorRegions), defaultValue: true,
             storageLocations: new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{nameof(ShowOutliningForCommentsAndPreprocessorRegions)}"));

        public static readonly PerLanguageOption<bool> ShowOutliningForDeclarationLevelConstructs = new PerLanguageOption<bool>(
            nameof(BlockStructureOptions), nameof(ShowOutliningForDeclarationLevelConstructs), defaultValue: true,
             storageLocations: new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{nameof(ShowOutliningForDeclarationLevelConstructs)}"));

        public static readonly PerLanguageOption<bool> ShowOutliningForCodeLevelConstructs = new PerLanguageOption<bool>(
            nameof(BlockStructureOptions), nameof(ShowOutliningForCodeLevelConstructs), defaultValue: true,
             storageLocations: new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{nameof(ShowOutliningForCodeLevelConstructs)}"));

        public static readonly PerLanguageOption<bool> CollapseRegionsWhenCollapsingToDefinitions = new PerLanguageOption<bool>(
            nameof(BlockStructureOptions), nameof(CollapseRegionsWhenCollapsingToDefinitions), defaultValue: false,
             storageLocations: new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{nameof(CollapseRegionsWhenCollapsingToDefinitions)}"));

        public static readonly PerLanguageOption<int> MaximumBannerLength = new PerLanguageOption<int>(
            nameof(BlockStructureOptions),
            nameof(MaximumBannerLength), defaultValue: 80,
            storageLocations: new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{nameof(MaximumBannerLength)}"));

    }
}
