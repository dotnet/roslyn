// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.Shared.Options
{
    internal static class InternalFeatureOnOffOptions
    {
        public const string OptionName = "FeatureManager/Features";
        private const string LocalRegistryPath = @"Roslyn\Internal\OnOff\Features\";

        [ExportOption]
        public static readonly Option<bool> BraceMatching = new Option<bool>(OptionName, "Brace Matching", defaultValue: true,
            persistences: new LocalUserProfilePersistence(LocalRegistryPath + "Brace Matching"));

        [ExportOption]
        public static readonly Option<bool> Classification = new Option<bool>(OptionName, "Classification", defaultValue: true,
            persistences: new LocalUserProfilePersistence(LocalRegistryPath + "Classification"));

        [ExportOption]
        public static readonly Option<bool> SemanticColorizer = new Option<bool>(OptionName, "Semantic Colorizer", defaultValue: true,
            persistences: new LocalUserProfilePersistence(LocalRegistryPath + "Semantic Colorizer"));

        [ExportOption]
        public static readonly Option<bool> SyntacticColorizer = new Option<bool>(OptionName, "Syntactic Colorizer", defaultValue: true,
            persistences: new LocalUserProfilePersistence(LocalRegistryPath + "Syntactic Colorizer"));

        [ExportOption]
        public static readonly Option<bool> AutomaticPairCompletion = new Option<bool>(OptionName, "Automatic Pair Completion", defaultValue: true,
            persistences: new LocalUserProfilePersistence(LocalRegistryPath + "Automatic Pair Completion"));

        [ExportOption]
        public static readonly Option<bool> AutomaticLineEnder = new Option<bool>(OptionName, "Automatic Line Ender", defaultValue: true,
            persistences: new LocalUserProfilePersistence(LocalRegistryPath + "Automatic Line Ender"));

        [ExportOption]
        public static readonly Option<bool> SmartIndenter = new Option<bool>(OptionName, "Smart Indenter", defaultValue: true,
            persistences: new LocalUserProfilePersistence(LocalRegistryPath + "Smart Indenter"));

        [ExportOption]
        public static readonly Option<bool> CompletionSet = new Option<bool>(OptionName, "Completion Set", defaultValue: true,
            persistences: new LocalUserProfilePersistence(LocalRegistryPath + "Completion Set"));

        [ExportOption]
        public static readonly Option<bool> KeywordHighlight = new Option<bool>(OptionName, "Keyword Highlight", defaultValue: true,
            persistences: new LocalUserProfilePersistence(LocalRegistryPath + "Keyword Highlight"));

        [ExportOption]
        public static readonly Option<bool> QuickInfo = new Option<bool>(OptionName, "Quick Info", defaultValue: true,
            persistences: new LocalUserProfilePersistence(LocalRegistryPath + "Quick Info"));

        [ExportOption]
        public static readonly Option<bool> Squiggles = new Option<bool>(OptionName, "Squiggles", defaultValue: true,
            persistences: new LocalUserProfilePersistence(LocalRegistryPath + "Squiggles"));

        [ExportOption]
        public static readonly Option<bool> FormatOnSave = new Option<bool>(OptionName, "FormatOnSave", defaultValue: true,
            persistences: new LocalUserProfilePersistence(LocalRegistryPath + "FormatOnSave"));

        [ExportOption]
        public static readonly Option<bool> RenameTracking = new Option<bool>(OptionName, "Rename Tracking", defaultValue: true,
            persistences: new LocalUserProfilePersistence(LocalRegistryPath + "Rename Tracking"));

        [ExportOption]
        public static readonly Option<bool> EventHookup = new Option<bool>(OptionName, "Event Hookup", defaultValue: true,
            persistences: new LocalUserProfilePersistence(LocalRegistryPath + "Event Hookup"));

        /// Due to https://github.com/dotnet/roslyn/issues/5393, the name "Snippets" is unusable for serialization.
        /// (Summary: Some builds incorrectly set it without providing a way to clear it so it exists in many registries.)
        [ExportOption]
        public static readonly Option<bool> Snippets = new Option<bool>(OptionName, "Snippets2", defaultValue: true,
            persistences: new LocalUserProfilePersistence(LocalRegistryPath + "Snippets2"));

        [ExportOption]
        public static readonly Option<bool> TodoComments = new Option<bool>(OptionName, "Todo Comments", defaultValue: true,
            persistences: new LocalUserProfilePersistence(LocalRegistryPath + "Todo Comments"));

        [ExportOption]
        public static readonly Option<bool> DesignerAttributes = new Option<bool>(OptionName, "Designer Attribute", defaultValue: true,
            persistences: new LocalUserProfilePersistence(LocalRegistryPath + "Designer Attribute"));

        [ExportOption]
        public static readonly Option<bool> FullSolutionAnalysisMemoryMonitor = new Option<bool>(OptionName, "Full Solution Analysis Memory Monitor", defaultValue: true,
            persistences: new LocalUserProfilePersistence(LocalRegistryPath + "Full Solution Analysis Memory Monitor"));

        [ExportOption]
        public static readonly Option<bool> ProjectReferenceConversion = new Option<bool>(OptionName, "Project Reference Conversion", defaultValue: true,
            persistences: new LocalUserProfilePersistence(LocalRegistryPath + "Project Reference Conversion"));
    }
}
