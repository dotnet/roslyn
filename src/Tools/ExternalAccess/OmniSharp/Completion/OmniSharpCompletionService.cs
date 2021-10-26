// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.Completion
{
    internal static class OmniSharpCompletionService
    {
        public static Task<(CompletionList? completionList, bool expandItemsAvailable)> GetCompletionsAsync(
            this CompletionService completionService,
            Document document,
            int caretPosition,
            CompletionTrigger trigger,
            ImmutableHashSet<string>? roles,
            CancellationToken cancellationToken)
            => completionService.GetCompletionsInternalAsync(document, caretPosition, CompletionOptions.Default, trigger, roles, cancellationToken);

        public static string? GetProviderName(this CompletionItem completionItem) => completionItem.ProviderName;

        public static bool? IncludeItemsFromUnimportedNamespaces(Document document)
            => document.Project.Solution.Options.GetOption(CompletionOptions.Metadata.ShowItemsFromUnimportedNamespaces, document.Project.Language);
    }
}
