// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Progression
{
    internal static class ProgressionOptions
    {
        private const string FeatureName = "ProgressionOptions";
        private const string LocalRegistryPath = @"Roslyn\Internal\OnOff\Components\Progression\";

        public static readonly Option2<bool> SearchUsingNavigateToEngine = new(
            FeatureName, nameof(SearchUsingNavigateToEngine), defaultValue: true,
            new LocalUserProfileStorageLocation(LocalRegistryPath + "SearchUsingNavigateToEngine"));

        public static readonly Option<bool> LegacySearchFeatureFlag = new(
            FeatureName, nameof(LegacySearchFeatureFlag), defaultValue: false,
            new FeatureFlagStorageLocation("Roslyn.ProgressionForceLegacySearch"));
    }
}
