// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.ImplementType;
using Microsoft.CodeAnalysis.InlineHints;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.SolutionCrawler;
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
            CompletionOptionsStorage.ProvideRegexCompletions,
            CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces,
            QuickInfoOptionsStorage.ShowRemarksInQuickInfo,
            // Go to definition
            MetadataAsSourceOptionsStorage.NavigateToDecompiledSources,
            // Document highlighting
            HighlightingOptionsStorage.HighlightRelatedJsonComponentsUnderCursor,
            HighlightingOptionsStorage.HighlightRelatedRegexComponentsUnderCursor,
            // Inline hints
            InlineHintsOptionsStorage.EnabledForParameters,
            InlineHintsOptionsStorage.ForLiteralParameters,
            InlineHintsOptionsStorage.ForIndexerParameters,
            InlineHintsOptionsStorage.ForObjectCreationParameters,
            InlineHintsOptionsStorage.ForOtherParameters,
            InlineHintsOptionsStorage.SuppressForParametersThatDifferOnlyBySuffix,
            InlineHintsOptionsStorage.SuppressForParametersThatMatchMethodIntent,
            InlineHintsOptionsStorage.SuppressForParametersThatMatchArgumentName,
            InlineHintsOptionsStorage.EnabledForTypes,
            InlineHintsOptionsStorage.ForImplicitVariableTypes,
            InlineHintsOptionsStorage.ForLambdaParameterTypes,
            InlineHintsOptionsStorage.ForImplicitObjectCreation,
            // EditorConfig
            FormattingOptions2.TabSize,
            FormattingOptions2.IndentationSize,
            FormattingOptions2.UseTabs,
            FormattingOptions2.NewLine,
            FormattingOptions2.InsertFinalNewLine,
            // Background analysis
            SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption,
            SolutionCrawlerOptionsStorage.CompilerDiagnosticsScopeOption,
            // Code lens options
            LspOptionsStorage.LspEnableReferencesCodeLens,
            LspOptionsStorage.LspEnableTestsCodeLens,
            // Project system
            LanguageServerProjectSystemOptionsStorage.BinaryLogPath,
            LanguageServerProjectSystemOptionsStorage.LoadInProcess);
    }
}
