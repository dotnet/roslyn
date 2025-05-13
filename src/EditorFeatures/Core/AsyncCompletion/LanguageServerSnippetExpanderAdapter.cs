// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServer.Client.Snippets;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion;

[Export(typeof(ILanguageServerSnippetExpander))]
[Shared]
internal sealed class LanguageServerSnippetExpanderAdapter : ILanguageServerSnippetExpander
{
    private readonly LanguageServerSnippetExpander _languageServerSnippetExpander;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public LanguageServerSnippetExpanderAdapter(LanguageServerSnippetExpander languageServerSnippetExpander)
    {
        _languageServerSnippetExpander = languageServerSnippetExpander;
    }

    public bool TryExpand(string lspSnippetText, SnapshotSpan snapshotSpan, ITextView textView)
        => _languageServerSnippetExpander.TryExpand(lspSnippetText, snapshotSpan, textView);
}
