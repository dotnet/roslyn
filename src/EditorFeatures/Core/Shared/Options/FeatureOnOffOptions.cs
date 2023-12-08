// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.Shared.Options
{
    internal sealed class FeatureOnOffOptions
    {
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

        public static readonly Option2<bool?> OfferRemoveUnusedReferences = new("dotnet_offer_remove_unused_references", defaultValue: true);

        public static readonly Option2<bool> OfferRemoveUnusedReferencesFeatureFlag = new("dotnet_offer_remove_unused_references_feature_flag", defaultValue: false);

        /// <summary>
        /// Not used by Roslyn but exposed in C# and VB option UI. Used by TestWindow and Project System.
        /// TODO: remove https://github.com/dotnet/roslyn/issues/57253
        /// </summary>
        public static readonly Option2<bool> SkipAnalyzersForImplicitlyTriggeredBuilds = new("dotnet_skip_analyzers_for_implicitly_triggered_builds", defaultValue: true);
    }
}
