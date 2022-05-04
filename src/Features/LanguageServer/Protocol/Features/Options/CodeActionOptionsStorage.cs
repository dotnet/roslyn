﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.ImplementType;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.SymbolSearch;

namespace Microsoft.CodeAnalysis.CodeActions
{
    internal static class CodeActionOptionsStorage
    {
        public static readonly PerLanguageOption2<int> WrappingColumn =
            new("FormattingOptions", "WrappingColumn", CodeActionOptions.DefaultWrappingColumn);

        public static CodeActionOptions GetCodeActionOptions(this IGlobalOptionService globalOptions, HostLanguageServices languageServices)
            => new(
                SearchOptions: globalOptions.GetSymbolSearchOptions(languageServices.Language),
                ImplementTypeOptions: globalOptions.GetImplementTypeOptions(languageServices.Language),
                ExtractMethodOptions: globalOptions.GetExtractMethodOptions(languageServices.Language),
                CleanupOptions: globalOptions.GetCodeCleanupOptions(languageServices),
                CodeGenerationOptions: globalOptions.GetCodeGenerationOptions(languageServices),
                HideAdvancedMembers: globalOptions.GetOption(CompletionOptionsStorage.HideAdvancedMembers, languageServices.Language),
                WrappingColumn: globalOptions.GetOption(WrappingColumn, languageServices.Language));

        internal static CodeActionOptionsProvider GetCodeActionOptionsProvider(this IGlobalOptionService globalOptions)
            => new CachingCodeActionsOptionsProvider(globalOptions);

        private sealed class CachingCodeActionsOptionsProvider : AbstractCodeActionOptionsProvider
        {
            private readonly IGlobalOptionService _globalOptions;
            private ImmutableDictionary<string, CodeActionOptions> _cache = ImmutableDictionary<string, CodeActionOptions>.Empty;

            public CachingCodeActionsOptionsProvider(IGlobalOptionService globalOptions)
            {
                _globalOptions = globalOptions;
            }

            public override CodeActionOptions GetOptions(HostLanguageServices languageService)
                => ImmutableInterlocked.GetOrAdd(ref _cache, languageService.Language, (language, options) => GetCodeActionOptions(options, languageService), _globalOptions);
        }
    }
}
