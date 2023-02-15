// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.Shared.Options
{
    internal sealed class InternalFeatureOnOffOptions
    {
        public static readonly Option2<bool> BraceMatching = new("dotnet_internal_feature_on_off_options_brace_matching", defaultValue: true);
        public static readonly Option2<bool> Classification = new("dotnet_internal_feature_on_off_options_classification", defaultValue: true);
        public static readonly Option2<bool> SemanticColorizer = new("dotnet_internal_feature_on_off_options_semantic_colorizer", defaultValue: true);
        public static readonly Option2<bool> SyntacticColorizer = new("dotnet_internal_feature_on_off_options_syntactic_colorizer", defaultValue: true);
        public static readonly Option2<bool> AutomaticLineEnder = new("dotnet_internal_feature_on_off_options_automatic_line_ender", defaultValue: true);
        public static readonly Option2<bool> SmartIndenter = new("dotnet_internal_feature_on_off_options_smart_indenter", defaultValue: true);
        public static readonly Option2<bool> Squiggles = new("dotnet_internal_feature_on_off_options_squiggles", defaultValue: true);
        public static readonly Option2<bool> FormatOnSave = new("visual_basic_internal_feature_on_off_options_format_on_save", defaultValue: true);
        public static readonly Option2<bool> RenameTracking = new("dotnet_internal_feature_on_off_options_rename_tracking", defaultValue: true);
        public static readonly Option2<bool> EventHookup = new("csharp_internal_feature_on_off_options_event_hook_up", defaultValue: true);
        public static readonly Option2<bool> Snippets = new("csharp_internal_feature_on_off_options_snippets", defaultValue: true);
        public static readonly Option2<bool> BackgroundAnalysisMemoryMonitor = new("dotnet_internal_feature_on_off_options_full_solution_analysis_memory_monitor", defaultValue: true);
    }
}
