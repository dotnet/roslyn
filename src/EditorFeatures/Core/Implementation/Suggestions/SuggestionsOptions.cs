// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    internal static class SuggestionsOptions
    {
        private const string FeatureName = "SuggestionsOptions";

        public static readonly Option2<bool?> Asynchronous = new(FeatureName, nameof(Asynchronous), defaultValue: null,
            new RoamingProfileStorageLocation("TextEditor.Specific.Suggestions.Asynchronous2"));

        public static readonly Option2<bool> AsynchronousQuickActionsEnableFeatureFlag = new(FeatureName, nameof(AsynchronousQuickActionsEnableFeatureFlag), defaultValue: false,
            new FeatureFlagStorageLocation("Roslyn.AsynchronousQuickActionsEnable2"));
    }
}
