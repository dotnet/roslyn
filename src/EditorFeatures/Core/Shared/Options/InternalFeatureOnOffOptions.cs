// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.Editor.Shared.Options
{
    internal static class InternalFeatureOnOffOptions
    {
        internal const string LocalRegistryPath = @"Roslyn\Internal\OnOff\Features\";

        public static readonly Option<bool> BraceMatching = new Option<bool>(nameof(InternalFeatureOnOffOptions), nameof(BraceMatching), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Brace Matching"));

        public static readonly Option<bool> Classification = new Option<bool>(nameof(InternalFeatureOnOffOptions), nameof(Classification), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Classification"));

        public static readonly Option<bool> SemanticColorizer = new Option<bool>(nameof(InternalFeatureOnOffOptions), nameof(SemanticColorizer), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Semantic Colorizer"));

        public static readonly Option<bool> SyntacticColorizer = new Option<bool>(nameof(InternalFeatureOnOffOptions), nameof(SyntacticColorizer), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Syntactic Colorizer"));

        public static readonly Option<bool> AutomaticPairCompletion = new Option<bool>(nameof(InternalFeatureOnOffOptions), nameof(AutomaticPairCompletion), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Automatic Pair Completion"));

        public static readonly Option<bool> AutomaticLineEnder = new Option<bool>(nameof(InternalFeatureOnOffOptions), nameof(AutomaticLineEnder), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Automatic Line Ender"));

        public static readonly Option<bool> SmartIndenter = new Option<bool>(nameof(InternalFeatureOnOffOptions), nameof(SmartIndenter), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Smart Indenter"));

        public static readonly Option<bool> CompletionSet = new Option<bool>(nameof(InternalFeatureOnOffOptions), nameof(CompletionSet), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Completion Set"));

        public static readonly Option<bool> KeywordHighlight = new Option<bool>(nameof(InternalFeatureOnOffOptions), nameof(KeywordHighlight), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Keyword Highlight"));

        public static readonly Option<bool> QuickInfo = new Option<bool>(nameof(InternalFeatureOnOffOptions), nameof(QuickInfo), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Quick Info"));

        public static readonly Option<bool> Squiggles = new Option<bool>(nameof(InternalFeatureOnOffOptions), nameof(Squiggles), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Squiggles"));

        public static readonly Option<bool> FormatOnSave = new Option<bool>(nameof(InternalFeatureOnOffOptions), nameof(FormatOnSave), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "FormatOnSave"));

        public static readonly Option<bool> RenameTracking = new Option<bool>(nameof(InternalFeatureOnOffOptions), nameof(RenameTracking), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Rename Tracking"));

        public static readonly Option<bool> EventHookup = new Option<bool>(nameof(InternalFeatureOnOffOptions), nameof(EventHookup), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Event Hookup"));

        /// Due to https://github.com/dotnet/roslyn/issues/5393, the name "Snippets" is unusable for serialization.
        /// (Summary: Some builds incorrectly set it without providing a way to clear it so it exists in many registries.)
        public static readonly Option<bool> Snippets = new Option<bool>(nameof(InternalFeatureOnOffOptions), nameof(Snippets), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Snippets2"));

        public static readonly Option<bool> TodoComments = new Option<bool>(nameof(InternalFeatureOnOffOptions), nameof(TodoComments), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Todo Comments"));

        public static readonly Option<bool> DesignerAttributes = new Option<bool>(nameof(InternalFeatureOnOffOptions), nameof(DesignerAttributes), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Designer Attribute"));

        public static readonly Option<bool> FullSolutionAnalysisMemoryMonitor = new Option<bool>(nameof(InternalFeatureOnOffOptions), nameof(FullSolutionAnalysisMemoryMonitor), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Full Solution Analysis Memory Monitor"));

        public static readonly Option<bool> ProjectReferenceConversion = new Option<bool>(nameof(InternalFeatureOnOffOptions), nameof(ProjectReferenceConversion), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Project Reference Conversion"));
    }

    [ExportOptionProvider, Shared]
    internal class InternalFeatureOnOffOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        public InternalFeatureOnOffOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            InternalFeatureOnOffOptions.BraceMatching,
            InternalFeatureOnOffOptions.Classification,
            InternalFeatureOnOffOptions.SemanticColorizer,
            InternalFeatureOnOffOptions.SyntacticColorizer,
            InternalFeatureOnOffOptions.AutomaticPairCompletion,
            InternalFeatureOnOffOptions.AutomaticLineEnder,
            InternalFeatureOnOffOptions.SmartIndenter,
            InternalFeatureOnOffOptions.CompletionSet,
            InternalFeatureOnOffOptions.KeywordHighlight,
            InternalFeatureOnOffOptions.QuickInfo,
            InternalFeatureOnOffOptions.Squiggles,
            InternalFeatureOnOffOptions.FormatOnSave,
            InternalFeatureOnOffOptions.RenameTracking,
            InternalFeatureOnOffOptions.EventHookup,
            InternalFeatureOnOffOptions.Snippets,
            InternalFeatureOnOffOptions.TodoComments,
            InternalFeatureOnOffOptions.DesignerAttributes,
            InternalFeatureOnOffOptions.FullSolutionAnalysisMemoryMonitor,
            InternalFeatureOnOffOptions.ProjectReferenceConversion);
    }
}
