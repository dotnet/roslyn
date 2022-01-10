// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.Structure
{
    internal record struct BlockStructureOptions(
        bool ShowBlockStructureGuidesForCommentsAndPreprocessorRegions,
        bool ShowBlockStructureGuidesForDeclarationLevelConstructs,
        bool ShowBlockStructureGuidesForCodeLevelConstructs,
        bool ShowOutliningForCommentsAndPreprocessorRegions,
        bool ShowOutliningForDeclarationLevelConstructs,
        bool ShowOutliningForCodeLevelConstructs,
        bool CollapseRegionsWhenCollapsingToDefinitions,
        int MaximumBannerLength,
        bool IsMetadataAsSource)
    {
        public static readonly BlockStructureOptions Default =
            new(ShowBlockStructureGuidesForCommentsAndPreprocessorRegions: Metadata.ShowBlockStructureGuidesForCommentsAndPreprocessorRegions.DefaultValue,
                ShowBlockStructureGuidesForDeclarationLevelConstructs: Metadata.ShowBlockStructureGuidesForDeclarationLevelConstructs.DefaultValue,
                ShowBlockStructureGuidesForCodeLevelConstructs: Metadata.ShowBlockStructureGuidesForCodeLevelConstructs.DefaultValue,
                ShowOutliningForCommentsAndPreprocessorRegions: Metadata.ShowOutliningForCommentsAndPreprocessorRegions.DefaultValue,
                ShowOutliningForDeclarationLevelConstructs: Metadata.ShowOutliningForDeclarationLevelConstructs.DefaultValue,
                ShowOutliningForCodeLevelConstructs: Metadata.ShowOutliningForCodeLevelConstructs.DefaultValue,
                CollapseRegionsWhenCollapsingToDefinitions: Metadata.CollapseRegionsWhenCollapsingToDefinitions.DefaultValue,
                MaximumBannerLength: Metadata.MaximumBannerLength.DefaultValue,
                IsMetadataAsSource: false);

        public static BlockStructureOptions From(Project project)
            => From(project.Solution.Options, project.Language, isMetadataAsSource: project.Solution.Workspace.Kind == WorkspaceKind.MetadataAsSource);

        public static BlockStructureOptions From(OptionSet options, string language, bool isMetadataAsSource)
          => new(
                ShowBlockStructureGuidesForCommentsAndPreprocessorRegions: options.GetOption(Metadata.ShowBlockStructureGuidesForCommentsAndPreprocessorRegions, language),
                ShowBlockStructureGuidesForDeclarationLevelConstructs: options.GetOption(Metadata.ShowBlockStructureGuidesForDeclarationLevelConstructs, language),
                ShowBlockStructureGuidesForCodeLevelConstructs: options.GetOption(Metadata.ShowBlockStructureGuidesForCodeLevelConstructs, language),
                ShowOutliningForCommentsAndPreprocessorRegions: options.GetOption(Metadata.ShowOutliningForCommentsAndPreprocessorRegions, language),
                ShowOutliningForDeclarationLevelConstructs: options.GetOption(Metadata.ShowOutliningForDeclarationLevelConstructs, language),
                ShowOutliningForCodeLevelConstructs: options.GetOption(Metadata.ShowOutliningForCodeLevelConstructs, language),
                CollapseRegionsWhenCollapsingToDefinitions: options.GetOption(Metadata.CollapseRegionsWhenCollapsingToDefinitions, language),
                MaximumBannerLength: options.GetOption(Metadata.MaximumBannerLength, language),
                IsMetadataAsSource: isMetadataAsSource);

        [ExportSolutionOptionProvider, Shared]
        internal sealed class Metadata : IOptionProvider
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public Metadata()
            {
            }

            public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
                ShowBlockStructureGuidesForCommentsAndPreprocessorRegions,
                ShowBlockStructureGuidesForDeclarationLevelConstructs,
                ShowBlockStructureGuidesForCodeLevelConstructs,
                ShowOutliningForCommentsAndPreprocessorRegions,
                ShowOutliningForDeclarationLevelConstructs,
                ShowOutliningForCodeLevelConstructs,
                CollapseRegionsWhenCollapsingToDefinitions,
                MaximumBannerLength);

            private const string FeatureName = "BlockStructureOptions";

            public static readonly PerLanguageOption2<bool> ShowBlockStructureGuidesForCommentsAndPreprocessorRegions = new(
                FeatureName, "ShowBlockStructureGuidesForCommentsAndPreprocessorRegions", defaultValue: false,
                storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ShowBlockStructureGuidesForCommentsAndPreprocessorRegions"));

            public static readonly PerLanguageOption2<bool> ShowBlockStructureGuidesForDeclarationLevelConstructs = new(
                FeatureName, "ShowBlockStructureGuidesForDeclarationLevelConstructs", defaultValue: true,
                storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ShowBlockStructureGuidesForDeclarationLevelConstructs"));

            public static readonly PerLanguageOption2<bool> ShowBlockStructureGuidesForCodeLevelConstructs = new(
                FeatureName, "ShowBlockStructureGuidesForCodeLevelConstructs", defaultValue: true,
                storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ShowBlockStructureGuidesForCodeLevelConstructs"));

            public static readonly PerLanguageOption2<bool> ShowOutliningForCommentsAndPreprocessorRegions = new(
                FeatureName, "ShowOutliningForCommentsAndPreprocessorRegions", defaultValue: true,
                storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ShowOutliningForCommentsAndPreprocessorRegions"));

            public static readonly PerLanguageOption2<bool> ShowOutliningForDeclarationLevelConstructs = new(
                FeatureName, "ShowOutliningForDeclarationLevelConstructs", defaultValue: true,
                storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ShowOutliningForDeclarationLevelConstructs"));

            public static readonly PerLanguageOption2<bool> ShowOutliningForCodeLevelConstructs = new(
                FeatureName, "ShowOutliningForCodeLevelConstructs", defaultValue: true,
                storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ShowOutliningForCodeLevelConstructs"));

            public static readonly PerLanguageOption2<bool> CollapseRegionsWhenCollapsingToDefinitions = new(
                FeatureName, "CollapseRegionsWhenCollapsingToDefinitions", defaultValue: false,
                storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.CollapseRegionsWhenCollapsingToDefinitions"));

            public static readonly PerLanguageOption2<int> MaximumBannerLength = new(
                FeatureName, "MaximumBannerLength", defaultValue: 80,
                storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.MaximumBannerLength"));
        }
    }
}
