// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.Completion
{
    internal sealed class CompletionViewOptions
    {
        private const string FeatureName = "CompletionOptions";

        public static readonly PerLanguageOption2<bool> HighlightMatchingPortionsOfCompletionListItems =
            new(FeatureName, nameof(HighlightMatchingPortionsOfCompletionListItems), defaultValue: true,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.HighlightMatchingPortionsOfCompletionListItems"));

        public static readonly PerLanguageOption2<bool> ShowCompletionItemFilters =
            new(FeatureName, nameof(ShowCompletionItemFilters), defaultValue: true,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ShowCompletionItemFilters"));

        // Use tri-value so the default state can be used to turn on the feature with experimentation service.
        public static readonly PerLanguageOption2<bool?> EnableArgumentCompletionSnippets =
            new(FeatureName, nameof(EnableArgumentCompletionSnippets), defaultValue: null,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.EnableArgumentCompletionSnippets"));

        public static readonly PerLanguageOption2<bool> BlockForCompletionItems =
            new(FeatureName, nameof(BlockForCompletionItems), defaultValue: true,
            storageLocation: new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.BlockForCompletionItems"));
    }
}
