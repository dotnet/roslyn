// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Completion
{
    internal static class CompletionService
    {
        private static Task<CompletionList> s_emptyCompletionListTask;

        public static IEnumerable<CompletionListProvider> GetDefaultCompletionListProviders(Document document)
        {
            return document.GetLanguageService<ICompletionService>().GetDefaultCompletionProviders();
        }

        public static CompletionRules GetCompletionRules(Document document)
        {
            return document.GetLanguageService<ICompletionService>().GetCompletionRules();
        }

        /// <summary>
        /// Returns the <see cref="CompletionList"/> for the specified <paramref name="position"/>
        /// in the <paramref name="document"/>.
        /// </summary>
        public static Task<CompletionList> GetCompletionListAsync(
            Document document,
            int position,
            CompletionTriggerInfo triggerInfo,
            OptionSet options = null,
            IEnumerable<CompletionListProvider> providers = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            var completionService = document.GetLanguageService<ICompletionService>();
            if (completionService != null)
            {
                options = options ?? document.Project.Solution.Workspace.Options;
                providers = providers ?? GetDefaultCompletionListProviders(document);

                return completionService.GetCompletionListAsync(document, position, triggerInfo, options, providers, cancellationToken);
            }
            else
            {
                if (s_emptyCompletionListTask == null)
                {
                    var value = Task.FromResult(new CompletionList(ImmutableArray<CompletionItem>.Empty));
                    Interlocked.CompareExchange(ref s_emptyCompletionListTask, value, null);
                }

                return s_emptyCompletionListTask;
            }
        }

        /// <summary>
        /// Returns true if the character at the specific position in the text snapshot should
        /// trigger completion. Implementers of this will be called on the main UI thread and should
        /// only do minimal textual checks to determine if they should be presented.
        /// </summary>
        public static async Task<bool> IsCompletionTriggerCharacterAsync(Document document, int characterPosition, IEnumerable<CompletionListProvider> completionProviders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var options = document.Project.Solution.Workspace.Options;
            return document.GetLanguageService<ICompletionService>().IsTriggerCharacter(text, characterPosition, completionProviders, options);
        }
    }
}
