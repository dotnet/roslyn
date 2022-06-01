// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
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
        internal static CodeActionOptions GetCodeActionOptions(this IGlobalOptionService globalOptions, HostLanguageServices languageServices)
            => GetCodeActionOptions(globalOptions, languageServices, isBlocking: false);

        internal static CodeActionOptions GetBlockingCodeActionOptions(this IGlobalOptionService globalOptions, HostLanguageServices languageServices)
            => GetCodeActionOptions(globalOptions, languageServices, isBlocking: true);

        private static CodeActionOptions GetCodeActionOptions(this IGlobalOptionService globalOptions, HostLanguageServices languageServices, bool isBlocking)
            => new(
                SearchOptions: globalOptions.GetSymbolSearchOptions(languageServices.Language),
                ImplementTypeOptions: globalOptions.GetImplementTypeOptions(languageServices.Language),
                ExtractMethodOptions: globalOptions.GetExtractMethodOptions(languageServices.Language),
                SimplifierOptions: globalOptions.GetSimplifierOptions(languageServices),
                HideAdvancedMembers: globalOptions.GetOption(CompletionOptionsStorage.HideAdvancedMembers, languageServices.Language),
                IsBlocking: isBlocking);

        internal static CodeActionOptionsProvider GetCodeActionOptionsProvider(this IGlobalOptionService globalOptions)
        {
            var cache = ImmutableDictionary<string, CodeActionOptions>.Empty;
            return languageService => ImmutableInterlocked.GetOrAdd(ref cache, languageService.Language, (_, options) => GetCodeActionOptions(options, languageService), globalOptions);
        }

        internal static CodeActionOptionsProvider GetBlockingCodeActionOptionsProvider(this IGlobalOptionService globalOptions)
        {
            var cache = ImmutableDictionary<string, CodeActionOptions>.Empty;
            return languageService => ImmutableInterlocked.GetOrAdd(ref cache, languageService.Language, (language, options) => GetBlockingCodeActionOptions(options, languageService), globalOptions);
        }
    }
}
