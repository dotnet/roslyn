// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.Shared.Options
{
    internal sealed class InternalFeatureOnOffOptions
    {
        private const string FeatureName = "InternalFeatureOnOffOptions";

        public static readonly Option2<bool> BraceMatching = new(FeatureName, "BraceMatching", defaultValue: true);
        public static readonly Option2<bool> Classification = new(FeatureName, "Classification", defaultValue: true);
        public static readonly Option2<bool> SemanticColorizer = new(FeatureName, "SemanticColorizer", defaultValue: true);
        public static readonly Option2<bool> SyntacticColorizer = new(FeatureName, "SyntacticColorizer", defaultValue: true);
        public static readonly Option2<bool> AutomaticLineEnder = new(FeatureName, "AutomaticLineEnder", defaultValue: true);
        public static readonly Option2<bool> SmartIndenter = new(FeatureName, "SmartIndenter", defaultValue: true);
        public static readonly Option2<bool> Squiggles = new(FeatureName, "Squiggles", defaultValue: true);
        public static readonly Option2<bool> FormatOnSave = new(FeatureName, "FormatOnSave", defaultValue: true);
        public static readonly Option2<bool> RenameTracking = new(FeatureName, "RenameTracking", defaultValue: true);
        public static readonly Option2<bool> EventHookup = new(FeatureName, "EventHookup", defaultValue: true);
        public static readonly Option2<bool> Snippets = new(FeatureName, "Snippets", defaultValue: true);
        public static readonly Option2<bool> BackgroundAnalysisMemoryMonitor = new(FeatureName, "FullSolutionAnalysisMemoryMonitor", defaultValue: true);
    }
}
