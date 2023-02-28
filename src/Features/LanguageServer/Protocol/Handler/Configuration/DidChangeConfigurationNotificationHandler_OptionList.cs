// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.ImplementType;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.SymbolSearch;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Configuration
{
    internal partial class DidChangeConfigurationNotificationHandler
    {
        public static readonly ImmutableArray<IOption2> SupportedOptions = ImmutableArray.Create<IOption2>(
            // Code Action
            SymbolSearchOptionsStorage.SearchReferenceAssemblies,
            // Implement Type
            ImplementTypeOptionsStorage.InsertionBehavior,
            ImplementTypeOptionsStorage.PropertyGenerationBehavior,
            // Completion
            CompletionOptionsStorage.ShowNameSuggestions,
            CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces,
            CompletionOptionsStorage.ProvideRegexCompletions,
            QuickInfoOptionsStorage.ShowRemarksInQuickInfo,
            // Go to definition
            MetadataAsSourceOptionsStorage.NavigateToDecompiledSources,
            // Format
            AutoFormattingOptionsStorage.FormatOnReturn,
            AutoFormattingOptionsStorage.FormatOnSemicolon,
            AutoFormattingOptionsStorage.FormatOnCloseBrace,
            // Document highlighting
            HighlightingOptionsStorage.HighlightRelatedJsonComponentsUnderCursor,
            HighlightingOptionsStorage.HighlightRelatedRegexComponentsUnderCursor);
    }
}
