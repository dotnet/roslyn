// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Structure;

internal static class BlockStructureOptionsStorage
{
    public static BlockStructureOptions GetBlockStructureOptions(this IGlobalOptionService globalOptions, Project project)
        => GetBlockStructureOptions(globalOptions, project.Language, isMetadataAsSource: project.Solution.Workspace.Kind == WorkspaceKind.MetadataAsSource);

    public static BlockStructureOptions GetBlockStructureOptions(this IGlobalOptionService globalOptions, string language, bool isMetadataAsSource)
        => new(
            ShowBlockStructureGuidesForCommentsAndPreprocessorRegions: globalOptions.GetOption(ShowBlockStructureGuidesForCommentsAndPreprocessorRegions, language),
            ShowBlockStructureGuidesForDeclarationLevelConstructs: globalOptions.GetOption(ShowBlockStructureGuidesForDeclarationLevelConstructs, language),
            ShowBlockStructureGuidesForCodeLevelConstructs: globalOptions.GetOption(ShowBlockStructureGuidesForCodeLevelConstructs, language),
            ShowOutliningForCommentsAndPreprocessorRegions: globalOptions.GetOption(ShowOutliningForCommentsAndPreprocessorRegions, language),
            ShowOutliningForDeclarationLevelConstructs: globalOptions.GetOption(ShowOutliningForDeclarationLevelConstructs, language),
            ShowOutliningForCodeLevelConstructs: globalOptions.GetOption(ShowOutliningForCodeLevelConstructs, language),
            CollapseRegionsOnFileOpen: globalOptions.GetOption(CollapseRegionsOnFileOpen, language),
            CollapseUsingsOnFileOpen: globalOptions.GetOption(CollapseUsingsOnFileOpen, language),
            CollapseImplementationsFromMetadataOnFileOpen: globalOptions.GetOption(CollapseImplementationsFromMetadataOnFileOpen, language),
            CollapseRegionsWhenCollapsingToDefinitions: globalOptions.GetOption(CollapseRegionsWhenCollapsingToDefinitions, language),
            MaximumBannerLength: globalOptions.GetOption(MaximumBannerLength, language),
            IsMetadataAsSource: isMetadataAsSource);

    private const string FeatureName = "BlockStructureOptions";

    public static readonly PerLanguageOption2<bool> ShowBlockStructureGuidesForCommentsAndPreprocessorRegions = new(
        FeatureName, "ShowBlockStructureGuidesForCommentsAndPreprocessorRegions", BlockStructureOptions.Default.ShowBlockStructureGuidesForCommentsAndPreprocessorRegions,
        storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ShowBlockStructureGuidesForCommentsAndPreprocessorRegions"));

    public static readonly PerLanguageOption2<bool> ShowBlockStructureGuidesForDeclarationLevelConstructs = new(
        FeatureName, "ShowBlockStructureGuidesForDeclarationLevelConstructs", BlockStructureOptions.Default.ShowBlockStructureGuidesForDeclarationLevelConstructs,
        storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ShowBlockStructureGuidesForDeclarationLevelConstructs"));

    public static readonly PerLanguageOption2<bool> ShowBlockStructureGuidesForCodeLevelConstructs = new(
        FeatureName, "ShowBlockStructureGuidesForCodeLevelConstructs", BlockStructureOptions.Default.ShowBlockStructureGuidesForCodeLevelConstructs,
        storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ShowBlockStructureGuidesForCodeLevelConstructs"));

    public static readonly PerLanguageOption2<bool> ShowOutliningForCommentsAndPreprocessorRegions = new(
        FeatureName, "ShowOutliningForCommentsAndPreprocessorRegions", BlockStructureOptions.Default.ShowOutliningForCommentsAndPreprocessorRegions,
        storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ShowOutliningForCommentsAndPreprocessorRegions"));

    public static readonly PerLanguageOption2<bool> ShowOutliningForDeclarationLevelConstructs = new(
        FeatureName, "ShowOutliningForDeclarationLevelConstructs", BlockStructureOptions.Default.ShowOutliningForDeclarationLevelConstructs,
        storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ShowOutliningForDeclarationLevelConstructs"));

    public static readonly PerLanguageOption2<bool> ShowOutliningForCodeLevelConstructs = new(
        FeatureName, "ShowOutliningForCodeLevelConstructs", BlockStructureOptions.Default.ShowOutliningForCodeLevelConstructs,
        storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ShowOutliningForCodeLevelConstructs"));

    public static readonly PerLanguageOption2<bool> CollapseRegionsOnFileOpen = new(
        FeatureName, "CollapseRegionsOnFileOpen", BlockStructureOptions.Default.CollapseRegionsOnFileOpen,
        storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.CollapseRegionsOnFileOpen"));

    public static readonly PerLanguageOption2<bool> CollapseUsingsOnFileOpen = new(
        FeatureName, "CollapseUsingsOnFileOpen", BlockStructureOptions.Default.CollapseUsingsOnFileOpen,
        storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.CollapseUsingsOnFileOpen"));

    public static readonly PerLanguageOption2<bool> CollapseImplementationsFromMetadataOnFileOpen = new(
        FeatureName, "CollapseImplementationsFromMetadataOnFileOpen", BlockStructureOptions.Default.CollapseImplementationsFromMetadataOnFileOpen,
        storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.CollapseImplementationsFromMetadataOnFileOpen"));

    public static readonly PerLanguageOption2<bool> CollapseRegionsWhenCollapsingToDefinitions = new(
        FeatureName, "CollapseRegionsWhenCollapsingToDefinitions", BlockStructureOptions.Default.CollapseRegionsWhenCollapsingToDefinitions,
        storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.CollapseRegionsWhenCollapsingToDefinitions"));

    public static readonly PerLanguageOption2<int> MaximumBannerLength = new(
        FeatureName, "MaximumBannerLength", BlockStructureOptions.Default.MaximumBannerLength,
        storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.MaximumBannerLength"));
}
