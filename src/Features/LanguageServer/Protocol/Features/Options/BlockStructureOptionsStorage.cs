// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Structure;

internal static class BlockStructureOptionsStorage
{
    public static BlockStructureOptions GetBlockStructureOptions(this IGlobalOptionService globalOptions, Project project)
        => GetBlockStructureOptions(globalOptions, project.Language, isMetadataAsSource: project.Solution.WorkspaceKind == WorkspaceKind.MetadataAsSource);

    public static BlockStructureOptions GetBlockStructureOptions(this IGlobalOptionService globalOptions, string language, bool isMetadataAsSource)
        => new()
        {
            ShowBlockStructureGuidesForCommentsAndPreprocessorRegions = globalOptions.GetOption(ShowBlockStructureGuidesForCommentsAndPreprocessorRegions, language),
            ShowBlockStructureGuidesForDeclarationLevelConstructs = globalOptions.GetOption(ShowBlockStructureGuidesForDeclarationLevelConstructs, language),
            ShowBlockStructureGuidesForCodeLevelConstructs = globalOptions.GetOption(ShowBlockStructureGuidesForCodeLevelConstructs, language),
            ShowOutliningForCommentsAndPreprocessorRegions = globalOptions.GetOption(ShowOutliningForCommentsAndPreprocessorRegions, language),
            ShowOutliningForDeclarationLevelConstructs = globalOptions.GetOption(ShowOutliningForDeclarationLevelConstructs, language),
            ShowOutliningForCodeLevelConstructs = globalOptions.GetOption(ShowOutliningForCodeLevelConstructs, language),
            CollapseRegionsWhenFirstOpened = globalOptions.GetOption(CollapseRegionsWhenFirstOpened, language),
            CollapseImportsWhenFirstOpened = globalOptions.GetOption(CollapseImportsWhenFirstOpened, language),
            CollapseMetadataImplementationsWhenFirstOpened = globalOptions.GetOption(CollapseSourceLinkEmbeddedDecompiledFilesWhenFirstOpened, language),
            CollapseEmptyMetadataImplementationsWhenFirstOpened = globalOptions.GetOption(CollapseMetadataSignatureFilesWhenFirstOpened, language),
            CollapseLocalFunctionsWhenCollapsingToDefinitions = globalOptions.GetOption(CollapseLocalFunctionsWhenCollapsingToDefinitions, language),
            CollapseRegionsWhenCollapsingToDefinitions = globalOptions.GetOption(CollapseRegionsWhenCollapsingToDefinitions, language),
            MaximumBannerLength = globalOptions.GetOption(MaximumBannerLength, language),
            IsMetadataAsSource = isMetadataAsSource,
        };

    public static readonly PerLanguageOption2<bool> ShowBlockStructureGuidesForCommentsAndPreprocessorRegions = new(
        "dotnet_show_block_structure_guides_for_comments_and_preprocessor_regions", BlockStructureOptions.Default.ShowBlockStructureGuidesForCommentsAndPreprocessorRegions);

    public static readonly PerLanguageOption2<bool> ShowBlockStructureGuidesForDeclarationLevelConstructs = new(
        "dotnet_show_block_structure_guides_for_declaration_level_constructs", BlockStructureOptions.Default.ShowBlockStructureGuidesForDeclarationLevelConstructs);

    public static readonly PerLanguageOption2<bool> ShowBlockStructureGuidesForCodeLevelConstructs = new(
        "dotnet_show_block_structure_guides_for_code_level_constructs", BlockStructureOptions.Default.ShowBlockStructureGuidesForCodeLevelConstructs);

    public static readonly PerLanguageOption2<bool> ShowOutliningForCommentsAndPreprocessorRegions = new(
        "dotnet_show_outlining_for_comments_and_preprocessor_regions", BlockStructureOptions.Default.ShowOutliningForCommentsAndPreprocessorRegions);

    public static readonly PerLanguageOption2<bool> ShowOutliningForDeclarationLevelConstructs = new(
        "dotnet_show_outlining_for_declaration_level_constructs", BlockStructureOptions.Default.ShowOutliningForDeclarationLevelConstructs);

    public static readonly PerLanguageOption2<bool> ShowOutliningForCodeLevelConstructs = new(
        "dotnet_show_outlining_for_code_level_constructs", BlockStructureOptions.Default.ShowOutliningForCodeLevelConstructs);

    public static readonly PerLanguageOption2<bool> CollapseRegionsWhenFirstOpened = new(
        "dotnet_collapse_regions_when_first_opened", BlockStructureOptions.Default.CollapseRegionsWhenFirstOpened);

    public static readonly PerLanguageOption2<bool> CollapseImportsWhenFirstOpened = new(
        "dotnet_collapse_imports_when_first_opened", BlockStructureOptions.Default.CollapseImportsWhenFirstOpened);

    public static readonly PerLanguageOption2<bool> CollapseSourceLinkEmbeddedDecompiledFilesWhenFirstOpened = new(
        "dotnet_collapse_metadata_implementations_when_first_opened", BlockStructureOptions.Default.CollapseMetadataImplementationsWhenFirstOpened);

    public static readonly PerLanguageOption2<bool> CollapseMetadataSignatureFilesWhenFirstOpened = new(
        "dotnet_collapse_empty_metadata_implementations_when_first_opened", BlockStructureOptions.Default.CollapseEmptyMetadataImplementationsWhenFirstOpened);

    public static readonly PerLanguageOption2<bool> CollapseRegionsWhenCollapsingToDefinitions = new(
        "dotnet_collapse_regions_when_collapsing_to_definitions", BlockStructureOptions.Default.CollapseRegionsWhenCollapsingToDefinitions);

    public static readonly PerLanguageOption2<bool> CollapseLocalFunctionsWhenCollapsingToDefinitions = new(
        "dotnet_collapse_local_functions_when_collapsing_to_definitions", BlockStructureOptions.Default.CollapseLocalFunctionsWhenCollapsingToDefinitions);

    public static readonly PerLanguageOption2<int> MaximumBannerLength = new(
        "dotnet_maximum_block_banner_length", BlockStructureOptions.Default.MaximumBannerLength);
}
