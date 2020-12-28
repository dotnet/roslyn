// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;
using Microsoft.CodeAnalysis.Storage;

namespace Microsoft.CodeAnalysis.Editor.Shared.Options
{
    internal static class InternalFeatureOnOffOptions
    {
        internal const string LocalRegistryPath = StorageOptions.LocalRegistryPath;

        public static readonly Option2<bool> BraceMatching = new(nameof(InternalFeatureOnOffOptions), nameof(BraceMatching), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Brace Matching"));

        public static readonly Option2<bool> Classification = new(nameof(InternalFeatureOnOffOptions), nameof(Classification), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Classification"));

        public static readonly Option2<bool> SemanticColorizer = new(nameof(InternalFeatureOnOffOptions), nameof(SemanticColorizer), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Semantic Colorizer"));

        public static readonly Option2<bool> SyntacticColorizer = new(nameof(InternalFeatureOnOffOptions), nameof(SyntacticColorizer), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Syntactic Colorizer"));

        public static readonly Option2<bool> AutomaticPairCompletion = new(nameof(InternalFeatureOnOffOptions), nameof(AutomaticPairCompletion), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Automatic Pair Completion"));

        public static readonly Option2<bool> AutomaticLineEnder = new(nameof(InternalFeatureOnOffOptions), nameof(AutomaticLineEnder), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Automatic Line Ender"));

        public static readonly Option2<bool> SmartIndenter = new(nameof(InternalFeatureOnOffOptions), nameof(SmartIndenter), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Smart Indenter"));

        public static readonly Option2<bool> CompletionSet = new(nameof(InternalFeatureOnOffOptions), nameof(CompletionSet), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Completion Set"));

        public static readonly Option2<bool> KeywordHighlight = new(nameof(InternalFeatureOnOffOptions), nameof(KeywordHighlight), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Keyword Highlight"));

        public static readonly Option2<bool> QuickInfo = new(nameof(InternalFeatureOnOffOptions), nameof(QuickInfo), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Quick Info"));

        public static readonly Option2<bool> Squiggles = new(nameof(InternalFeatureOnOffOptions), nameof(Squiggles), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Squiggles"));

        public static readonly Option2<bool> FormatOnSave = new(nameof(InternalFeatureOnOffOptions), nameof(FormatOnSave), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "FormatOnSave"));

        public static readonly Option2<bool> RenameTracking = new(nameof(InternalFeatureOnOffOptions), nameof(RenameTracking), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Rename Tracking"));

        public static readonly Option2<bool> EventHookup = new(nameof(InternalFeatureOnOffOptions), nameof(EventHookup), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Event Hookup"));

        /// Due to https://github.com/dotnet/roslyn/issues/5393, the name "Snippets" is unusable for serialization.
        /// (Summary: Some builds incorrectly set it without providing a way to clear it so it exists in many registries.)
        public static readonly Option2<bool> Snippets = new(nameof(InternalFeatureOnOffOptions), nameof(Snippets), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Snippets2"));

        public static readonly Option2<bool> TodoComments = new(nameof(InternalFeatureOnOffOptions), nameof(TodoComments), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Todo Comments"));

        public static readonly Option2<bool> DesignerAttributes = new(nameof(InternalFeatureOnOffOptions), nameof(DesignerAttributes), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Designer Attribute"));

        public static readonly Option2<bool> BackgroundAnalysisMemoryMonitor = new(nameof(InternalFeatureOnOffOptions), "FullSolutionAnalysisMemoryMonitor", defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Full Solution Analysis Memory Monitor"));

        public static readonly Option2<bool> ProjectReferenceConversion = new(nameof(InternalFeatureOnOffOptions), nameof(ProjectReferenceConversion), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Project Reference Conversion"));
    }

    [ExportOptionProvider, Shared]
    internal class InternalFeatureOnOffOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
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
            InternalFeatureOnOffOptions.BackgroundAnalysisMemoryMonitor,
            InternalFeatureOnOffOptions.ProjectReferenceConversion);
    }
}
