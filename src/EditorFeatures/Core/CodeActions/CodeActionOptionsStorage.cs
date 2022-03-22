// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.ImplementType;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.CodeAnalysis.ExtractMethod;

namespace Microsoft.CodeAnalysis.CodeActions
{
    internal static class CodeActionOptionsStorage
    {
        internal static CodeActionOptions GetCodeActionOptions(this IGlobalOptionService globalOptions, string language)
            => GetCodeActionOptions(globalOptions, language, isBlocking: false);

        internal static CodeActionOptions GetBlockingCodeActionOptions(this IGlobalOptionService globalOptions, string language)
            => GetCodeActionOptions(globalOptions, language, isBlocking: true);

        private static CodeActionOptions GetCodeActionOptions(this IGlobalOptionService globalOptions, string language, bool isBlocking)
            => new(
                SearchOptions: globalOptions.GetSymbolSearchOptions(language),
                ImplementTypeOptions: globalOptions.GetImplementTypeOptions(language),
                ExtractMethodOptions: globalOptions.GetExtractMethodOptions(language),
                HideAdvancedMembers: globalOptions.GetOption(CompletionOptionsStorage.HideAdvancedMembers, language),
                IsBlocking: isBlocking);

        internal static CodeActionOptionsProvider GetCodeActionOptionsProvider(this IGlobalOptionService globalOptions)
        {
            var cache = ImmutableDictionary<string, CodeActionOptions>.Empty;
            return language => ImmutableInterlocked.GetOrAdd(ref cache, language, (language, options) => GetCodeActionOptions(options, language), globalOptions);
        }
    }
}
