// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract class EmbeddedLanguageCompletionProvider
    {
        public string Name { get; }

        internal EmbeddedLanguageCompletionProvider()
        {
            Name = GetType().FullName!;
        }

        public abstract ImmutableHashSet<char> TriggerCharacters { get; }
        public abstract bool ShouldTriggerCompletion(SourceText text, int caretPosition, CompletionTrigger trigger);
        public abstract Task ProvideCompletionsAsync(CompletionContext context);
        public abstract Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey, CancellationToken cancellationToken);
        public abstract Task<CompletionDescription?> GetDescriptionAsync(Document document, CompletionItem item, CancellationToken cancellationToken);
    }
}
