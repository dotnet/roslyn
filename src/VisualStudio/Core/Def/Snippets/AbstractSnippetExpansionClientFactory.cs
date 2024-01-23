// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Snippets;

internal abstract class AbstractSnippetExpansionClientFactory(IThreadingContext threadingContext) : ISnippetExpansionClientFactory
{
    private readonly IThreadingContext _threadingContext = threadingContext;

    protected abstract AbstractSnippetExpansionClient CreateSnippetExpansionClient(ITextView textView, ITextBuffer subjectBuffer);

    public AbstractSnippetExpansionClient GetSnippetExpansionClient(ITextView textView, ITextBuffer subjectBuffer)
    {
        Contract.ThrowIfFalse(_threadingContext.JoinableTaskContext.IsOnMainThread);

        if (!textView.Properties.TryGetProperty(typeof(AbstractSnippetExpansionClient), out AbstractSnippetExpansionClient expansionClient))
        {
            expansionClient = CreateSnippetExpansionClient(textView, subjectBuffer);
            textView.Properties.AddProperty(typeof(AbstractSnippetExpansionClient), expansionClient);
        }

        return expansionClient;
    }
}
