// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SymbolSearch;

namespace Microsoft.CodeAnalysis.CodeActions
{
    internal static class CodeActionOptionsStorage
    {
        internal static CodeActionOptions GetCodeActionOptions(this IGlobalOptionService globalOptions, string language, bool isBlocking)
            => new(
                SearchOptions: globalOptions.GetSymbolSearchOptions(language),
                HideAdvancedMembers: globalOptions.GetOption(CompletionOptionsStorage.HideAdvancedMembers, language),
                IsBlocking: isBlocking);
    }
}
