// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.LanguageService;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.Completion;

internal static class OmniSharpCompletionService
{
    extension(CompletionService completionService)
    {
        public async ValueTask<bool> ShouldTriggerCompletionAsync(
        Document document,
        int caretPosition,
        CompletionTrigger trigger,
        ImmutableHashSet<string>? roles,
        OmniSharpCompletionOptions options,
        CancellationToken cancellationToken)
        {
            var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            return completionService.ShouldTriggerCompletion(document.Project, document.Project.Services, text, caretPosition, trigger, options.ToCompletionOptions(), document.Project.Solution.Options, roles);
        }

        public Task<CompletionList> GetCompletionsAsync(
           Document document,
           int caretPosition,
           CompletionTrigger trigger,
           ImmutableHashSet<string>? roles,
           OmniSharpCompletionOptions options,
           CancellationToken cancellationToken)
        {
            return completionService.GetCompletionsAsync(document, caretPosition, options.ToCompletionOptions(), document.Project.Solution.Options, trigger, roles, cancellationToken);
        }

        public Task<CompletionDescription?> GetDescriptionAsync(
           Document document,
           CompletionItem item,
           OmniSharpCompletionOptions options,
           CancellationToken cancellationToken)
        {
            return completionService.GetDescriptionAsync(document, item, options.ToCompletionOptions(), SymbolDescriptionOptions.Default, cancellationToken);
        }
    }

    extension(CompletionItem completionItem)
    {
        public string? GetProviderName() => completionItem.ProviderName;
    }
}
