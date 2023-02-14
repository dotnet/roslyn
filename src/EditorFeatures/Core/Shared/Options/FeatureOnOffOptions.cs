// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.Shared.Options
{
    internal sealed class FeatureOnOffOptions
    {
        public static readonly PerLanguageOption2<bool> Outlining = new("FeatureOnOffOptions_Outlining", defaultValue: true);

        public static readonly PerLanguageOption2<bool> KeywordHighlighting = new("FeatureOnOffOptions_KeywordHighlighting", defaultValue: true);

        public static readonly PerLanguageOption2<bool> ReferenceHighlighting = new("FeatureOnOffOptions_ReferenceHighlighting", defaultValue: true);

        public static readonly PerLanguageOption2<bool> AutoInsertBlockCommentStartString = new("FeatureOnOffOptions_AutoInsertBlockCommentStartString", defaultValue: true);

        public static readonly PerLanguageOption2<bool> PrettyListing = new("FeatureOnOffOptions_PrettyListing", defaultValue: true);

        public static readonly PerLanguageOption2<bool> StringIdentation = new("FeatureOnOffOptions_StringIdentation", defaultValue: true);

        public static readonly PerLanguageOption2<bool> RenameTrackingPreview = new("FeatureOnOffOptions_RenameTrackingPreview", defaultValue: true);

        /// <summary>
        /// This option is not currently used by Roslyn, but we might want to implement it in the
        /// future. Keeping the option while it's unimplemented allows all upgrade paths to
        /// maintain any customized value for this setting, even through versions that have not
        /// implemented this feature yet.
        /// </summary>
        public static readonly PerLanguageOption2<bool> RenameTracking = new("FeatureOnOffOptions_RenameTracking", defaultValue: true);

        /// <summary>
        /// This option is not currently used by Roslyn, but we might want to implement it in the
        /// future. Keeping the option while it's unimplemented allows all upgrade paths to
        /// maintain any customized value for this setting, even through versions that have not
        /// implemented this feature yet.
        /// </summary>
        public static readonly PerLanguageOption2<bool> RefactoringVerification = new("FeatureOnOffOptions_RefactoringVerification", defaultValue: false);

        public static readonly Option2<bool> NavigateAsynchronously = new("FeatureOnOffOptions_NavigateAsynchronously", defaultValue: true);

        public static readonly Option2<bool?> OfferRemoveUnusedReferences = new("FeatureOnOffOptions_OfferRemoveUnusedReferences", defaultValue: true);

        public static readonly Option2<bool> OfferRemoveUnusedReferencesFeatureFlag = new("FeatureOnOffOptions_OfferRemoveUnusedReferencesFeatureFlag", defaultValue: false);

        public static readonly Option2<bool> AutomaticallyCompleteStatementOnSemicolon = new("FeatureOnOffOptions_AutomaticallyCompleteStatementOnSemicolon", defaultValue: true);

        public static readonly PerLanguageOption2<bool> AutomaticallyFixStringContentsOnPaste = new("FeatureOnOffOptions_AutomaticallyFixStringContentsOnPaste", defaultValue: true);

        /// <summary>
        /// Not used by Roslyn but exposed in C# and VB option UI. Used by TestWindow and Project System.
        /// TODO: remove https://github.com/dotnet/roslyn/issues/57253
        /// </summary>
        public static readonly Option2<bool> SkipAnalyzersForImplicitlyTriggeredBuilds = new("FeatureOnOffOptions_SkipAnalyzersForImplicitlyTriggeredBuilds", defaultValue: true);
    }
}
