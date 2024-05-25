// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.ImplementType;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SymbolSearch;

namespace Microsoft.CodeAnalysis.CodeActions
{
    internal static class CodeActionOptionsStorage
    {
        public static readonly PerLanguageOption2<int> WrappingColumn =
            new("FormattingOptions_WrappingColumn", CodeActionOptions.DefaultWrappingColumn);

        public static CodeActionOptions GetCodeActionOptions(this IGlobalOptionService globalOptions, LanguageServices languageServices)
            => new()
            {
                CleanupOptions = globalOptions.GetCodeCleanupOptions(languageServices),
                CodeGenerationOptions = globalOptions.GetCodeGenerationOptions(languageServices),
                CodeStyleOptions = globalOptions.GetCodeStyleOptions(languageServices),
                SearchOptions = globalOptions.GetSymbolSearchOptions(languageServices.Language),
                ImplementTypeOptions = globalOptions.GetImplementTypeOptions(languageServices.Language),
                ExtractMethodOptions = globalOptions.GetExtractMethodOptions(languageServices.Language),
                HideAdvancedMembers = globalOptions.GetOption(CompletionOptionsStorage.HideAdvancedMembers, languageServices.Language),
                WrappingColumn = globalOptions.GetOption(WrappingColumn, languageServices.Language),
                ConditionalExpressionWrappingLength = globalOptions.GetOption(ConditionalExpressionWrappingLength, languageServices.Language),
                CollectionExpressionWrappingLength = globalOptions.GetOption(CollectionExpressionWrappingLength, languageServices.Language),
            };

        internal static CodeActionOptionsProvider GetCodeActionOptionsProvider(this IGlobalOptionService globalOptions)
        {
            var cache = ImmutableDictionary<string, CodeActionOptions>.Empty;
            return new DelegatingCodeActionOptionsProvider(languageService => ImmutableInterlocked.GetOrAdd(ref cache, languageService.Language, (_, options) => GetCodeActionOptions(options, languageService), globalOptions));
        }

        public static readonly PerLanguageOption2<int> ConditionalExpressionWrappingLength = new(
            "dotnet_conditional_expression_wrapping_length", CodeActionOptions.DefaultConditionalExpressionWrappingLength);

        public static readonly PerLanguageOption2<int> CollectionExpressionWrappingLength = new(
            "dotnet_collection_expression_wrapping_length", CodeActionOptions.DefaultCollectionExpressionWrappingLength);
    }
}
