// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.ImplementType;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SymbolSearch;

namespace Microsoft.CodeAnalysis.CodeActions
{
    internal static class CodeActionOptionsStorage
    {
        public static CodeActionOptions GetCodeActionOptions(this IGlobalOptionService globalOptions, LanguageServices languageServices)
            => new();

        internal static CodeActionOptionsProvider GetCodeActionOptionsProvider(this IGlobalOptionService globalOptions)
        {
            var cache = ImmutableDictionary<string, CodeActionOptions>.Empty;
            return new DelegatingCodeActionOptionsProvider(languageService => ImmutableInterlocked.GetOrAdd(ref cache, languageService.Language, (_, options) => GetCodeActionOptions(options, languageService), globalOptions));
        }
    }
}
