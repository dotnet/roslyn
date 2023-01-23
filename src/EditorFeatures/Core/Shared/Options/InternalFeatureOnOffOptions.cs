// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.Shared.Options
{
    internal sealed class InternalFeatureOnOffOptions
    {
        public static readonly Option2<bool> BraceMatching = new("InternalFeatureOnOffOptions_BraceMatching", defaultValue: true);
        public static readonly Option2<bool> Classification = new("InternalFeatureOnOffOptions_Classification", defaultValue: true);
        public static readonly Option2<bool> SemanticColorizer = new("InternalFeatureOnOffOptions_SemanticColorizer", defaultValue: true);
        public static readonly Option2<bool> SyntacticColorizer = new("InternalFeatureOnOffOptions_SyntacticColorizer", defaultValue: true);
        public static readonly Option2<bool> AutomaticLineEnder = new("InternalFeatureOnOffOptions_AutomaticLineEnder", defaultValue: true);
        public static readonly Option2<bool> SmartIndenter = new("InternalFeatureOnOffOptions_SmartIndenter", defaultValue: true);
        public static readonly Option2<bool> Squiggles = new("InternalFeatureOnOffOptions_Squiggles", defaultValue: true);
        public static readonly Option2<bool> FormatOnSave = new("InternalFeatureOnOffOptions_FormatOnSave", defaultValue: true);
        public static readonly Option2<bool> RenameTracking = new("InternalFeatureOnOffOptions_RenameTracking", defaultValue: true);
        public static readonly Option2<bool> EventHookup = new("InternalFeatureOnOffOptions_EventHookup", defaultValue: true);
        public static readonly Option2<bool> Snippets = new("InternalFeatureOnOffOptions_Snippets", defaultValue: true);
        public static readonly Option2<bool> BackgroundAnalysisMemoryMonitor = new("InternalFeatureOnOffOptions_FullSolutionAnalysisMemoryMonitor", defaultValue: true);
    }
}
