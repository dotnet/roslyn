// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.Completion
{
    [ExportOptionProvider, Shared]
    internal class CompletionOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        public CompletionOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            CompletionOptions.HideAdvancedMembers,
            CompletionOptions.TriggerOnTyping,
            CompletionOptions.TriggerOnTypingLetters,
            CompletionOptions.ShowCompletionItemFilters,
            CompletionOptions.HighlightMatchingPortionsOfCompletionListItems,
            CompletionOptions.EnterKeyBehavior,
            CompletionOptions.SnippetsBehavior,
            CompletionOptions.ShowItemsFromUnimportedNamespaces);
    }
}
