// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Completion.Rules;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Completion
{
    internal static class CompletionService
    {
        public static IEnumerable<ICompletionProvider> GetDefaultCompletionProviders(Document document)
        {
            return document.GetLanguageService<ICompletionService>().GetDefaultCompletionProviders();
        }

        public static ICompletionRules GetDefaultCompletionRules(Document document)
        {
            return document.GetLanguageService<ICompletionService>().GetDefaultCompletionRules();
        }

        /// <summary>
        /// Returns the CompletionItemGroups for the specified position in the document.
        /// </summary>
        public static Task<IEnumerable<CompletionItemGroup>> GetCompletionItemGroupsAsync(Document document, int position, CompletionTriggerInfo triggerInfo, IEnumerable<ICompletionProvider> completionProviders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return document.GetLanguageService<ICompletionService>().GetGroupsAsync(document, position, triggerInfo, completionProviders, cancellationToken);
        }

        /// <summary>
        /// Returns true if the character at the specific position in the text snapshot should
        /// trigger completion. Implementers of this will be called on the main UI thread and should
        /// only do minimal textual checks to determine if they should be presented.
        /// </summary>
        public static async Task<bool> IsCompletionTriggerCharacterAsync(Document document, int characterPosition, IEnumerable<ICompletionProvider> completionProviders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var options = document.Project.Solution.Workspace.Options;
            return document.GetLanguageService<ICompletionService>().IsTriggerCharacter(text, characterPosition, completionProviders, options);
        }
    }
}
