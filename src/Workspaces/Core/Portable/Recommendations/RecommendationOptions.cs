// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Recommendations
{
    public static class RecommendationOptions
    {
        internal const string RecommendationsFeatureName = "Recommendations";

        public static readonly PerLanguageOption<bool> HideAdvancedMembers = new PerLanguageOption<bool>(RecommendationsFeatureName, "HideAdvancedMembers", defaultValue: false);

        public static readonly PerLanguageOption<bool> FilterOutOfScopeLocals = new PerLanguageOption<bool>(RecommendationsFeatureName, "FilterOutOfScopeLocals", defaultValue: true);
    }
}
