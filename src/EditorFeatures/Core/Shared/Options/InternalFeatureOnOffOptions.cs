// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.Shared.Options
{
    internal static class InternalFeatureOnOffOptions
    {
        internal const string LocalRegistryPath = @"Roslyn\Internal\OnOff\Features\";

        [ExportOption]
        public static readonly Option<bool> BraceMatching = new Option<bool>(nameof(InternalFeatureOnOffOptions), nameof(BraceMatching), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Brace Matching"));

        [ExportOption]
        public static readonly Option<bool> Classification = new Option<bool>(nameof(InternalFeatureOnOffOptions), nameof(Classification), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Classification"));

        [ExportOption]
        public static readonly Option<bool> SemanticColorizer = new Option<bool>(nameof(InternalFeatureOnOffOptions), nameof(SemanticColorizer), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Semantic Colorizer"));

        [ExportOption]
        public static readonly Option<bool> SyntacticColorizer = new Option<bool>(nameof(InternalFeatureOnOffOptions), nameof(SyntacticColorizer), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Syntactic Colorizer"));

        [ExportOption]
        public static readonly Option<bool> AutomaticPairCompletion = new Option<bool>(nameof(InternalFeatureOnOffOptions), nameof(AutomaticPairCompletion), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Automatic Pair Completion"));

        [ExportOption]
        public static readonly Option<bool> AutomaticLineEnder = new Option<bool>(nameof(InternalFeatureOnOffOptions), nameof(AutomaticLineEnder), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Automatic Line Ender"));

        [ExportOption]
        public static readonly Option<bool> SmartIndenter = new Option<bool>(nameof(InternalFeatureOnOffOptions), nameof(SmartIndenter), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Smart Indenter"));

        [ExportOption]
        public static readonly Option<bool> CompletionSet = new Option<bool>(nameof(InternalFeatureOnOffOptions), nameof(CompletionSet), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Completion Set"));

        [ExportOption]
        public static readonly Option<bool> KeywordHighlight = new Option<bool>(nameof(InternalFeatureOnOffOptions), nameof(KeywordHighlight), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Keyword Highlight"));

        [ExportOption]
        public static readonly Option<bool> QuickInfo = new Option<bool>(nameof(InternalFeatureOnOffOptions), nameof(QuickInfo), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Quick Info"));

        [ExportOption]
        public static readonly Option<bool> Squiggles = new Option<bool>(nameof(InternalFeatureOnOffOptions), nameof(Squiggles), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Squiggles"));

        [ExportOption]
        public static readonly Option<bool> FormatOnSave = new Option<bool>(nameof(InternalFeatureOnOffOptions), nameof(FormatOnSave), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "FormatOnSave"));

        [ExportOption]
        public static readonly Option<bool> RenameTracking = new Option<bool>(nameof(InternalFeatureOnOffOptions), nameof(RenameTracking), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Rename Tracking"));

        [ExportOption]
        public static readonly Option<bool> EventHookup = new Option<bool>(nameof(InternalFeatureOnOffOptions), nameof(EventHookup), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Event Hookup"));

        /// Due to https://github.com/dotnet/roslyn/issues/5393, the name "Snippets" is unusable for serialization.
        /// (Summary: Some builds incorrectly set it without providing a way to clear it so it exists in many registries.)
        [ExportOption]
        public static readonly Option<bool> Snippets = new Option<bool>(nameof(InternalFeatureOnOffOptions), nameof(Snippets), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Snippets2"));

        [ExportOption]
        public static readonly Option<bool> TodoComments = new Option<bool>(nameof(InternalFeatureOnOffOptions), nameof(TodoComments), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Todo Comments"));

        [ExportOption]
        public static readonly Option<bool> DesignerAttributes = new Option<bool>(nameof(InternalFeatureOnOffOptions), nameof(DesignerAttributes), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Designer Attribute"));

        [ExportOption]
        public static readonly Option<bool> FullSolutionAnalysisMemoryMonitor = new Option<bool>(nameof(InternalFeatureOnOffOptions), nameof(FullSolutionAnalysisMemoryMonitor), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Full Solution Analysis Memory Monitor"));

        [ExportOption]
        public static readonly Option<bool> ProjectReferenceConversion = new Option<bool>(nameof(InternalFeatureOnOffOptions), nameof(ProjectReferenceConversion), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Project Reference Conversion"));
    }
}
