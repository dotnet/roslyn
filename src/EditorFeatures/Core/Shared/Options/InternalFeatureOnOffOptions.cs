// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.Shared.Options
{
    internal static class InternalFeatureOnOffOptions
    {
        public const string OptionName = "FeatureManager/Features";

        [ExportOption]
        public static readonly Option<bool> BraceMatching = new Option<bool>(OptionName, "Brace Matching", defaultValue: true);

        [ExportOption]
        public static readonly Option<bool> Classification = new Option<bool>(OptionName, "Classification", defaultValue: true);

        [ExportOption]
        public static readonly Option<bool> SemanticColorizer = new Option<bool>(OptionName, "Semantic Colorizer", defaultValue: true);

        [ExportOption]
        public static readonly Option<bool> SyntacticColorizer = new Option<bool>(OptionName, "Syntactic Colorizer", defaultValue: true);

        [ExportOption]
        public static readonly Option<bool> AutomaticPairCompletion = new Option<bool>(OptionName, "Automatic Pair Completion", defaultValue: true);

        [ExportOption]
        public static readonly Option<bool> AutomaticLineEnder = new Option<bool>(OptionName, "Automatic Line Ender", defaultValue: true);

        [ExportOption]
        public static readonly Option<bool> SmartIndenter = new Option<bool>(OptionName, "Smart Indenter", defaultValue: true);

        [ExportOption]
        public static readonly Option<bool> CompletionSet = new Option<bool>(OptionName, "Completion Set", defaultValue: true);

        [ExportOption]
        public static readonly Option<bool> KeywordHighlight = new Option<bool>(OptionName, "Keyword Highlight", defaultValue: true);

        [ExportOption]
        public static readonly Option<bool> QuickInfo = new Option<bool>(OptionName, "Quick Info", defaultValue: true);

        [ExportOption]
        public static readonly Option<bool> Squiggles = new Option<bool>(OptionName, "Squiggles", defaultValue: true);

        [ExportOption]
        public static readonly Option<bool> FormatOnSave = new Option<bool>(OptionName, "FormatOnSave", defaultValue: true);

        [ExportOption]
        public static readonly Option<bool> RenameTracking = new Option<bool>(OptionName, "Rename Tracking", defaultValue: true);

        [ExportOption]
        public static readonly Option<bool> EventHookup = new Option<bool>(OptionName, "Event Hookup", defaultValue: true);

        /// <remarks>
        /// Due to https://github.com/dotnet/roslyn/issues/5393, the name "Snippets" is unusable.
        /// (Summary: Some builds incorrectly set it without providing a way to clear it so it exists in many registries.)
        /// </remarks>
        [ExportOption]
        public static readonly Option<bool> Snippets = new Option<bool>(OptionName, "Snippets2", defaultValue: true);

        [ExportOption]
        public static readonly Option<bool> TodoComments = new Option<bool>(OptionName, "Todo Comments", defaultValue: true);

        [ExportOption]
        public static readonly Option<bool> DesignerAttributes = new Option<bool>(OptionName, "Designer Attribute", defaultValue: true);

        [ExportOption]
        public static readonly Option<bool> EsentPerformanceMonitor = new Option<bool>(OptionName, "Esent PerfMon", defaultValue: false);
    }
}
