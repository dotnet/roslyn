// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    internal sealed class SuggestionsOptions
    {
        private const string FeatureName = "SuggestionsOptions";

        public static readonly Option2<bool?> Asynchronous = new(FeatureName, "Asynchronous", defaultValue: null);
        public static readonly Option2<bool> AsynchronousQuickActionsDisableFeatureFlag = new(FeatureName, "AsynchronousQuickActionsDisableFeatureFlag", defaultValue: false);
    }
}
