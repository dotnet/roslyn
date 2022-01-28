// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.SymbolSearch;

namespace Microsoft.CodeAnalysis.CodeActions
{
    internal static class CodeActionOptionsFactory
    {
        internal static CodeActionOptions GetCodeActionOptions(Project project, bool isBlocking)
        {
            var options = project.Solution.Options;
            var language = project.Language;

            return new(
                IsBlocking: isBlocking,
                SearchReferenceAssemblies: options.GetOption(SymbolSearchOptions.SuggestForTypesInReferenceAssemblies, language),
                HideAdvancedMembers: options.GetOption(CompletionOptions.Metadata.HideAdvancedMembers, language));
        }
    }
}
