// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Implementation.Snippets;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.LanguageServices.Snippets;

[Export(typeof(ISemanticSnippetExpander))]
[Shared]
internal sealed class SemanticSnippetExpander : ISemanticSnippetExpander
{
    private readonly RoslynLSPSnippetParser _snippetParser;
    private readonly RoslynLSPSnippetConverter _snippetConverter;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public SemanticSnippetExpander(
        RoslynLSPSnippetParser snippetParser,
        RoslynLSPSnippetConverter snippetConverter)
    {
        _snippetParser = snippetParser;
        _snippetConverter = snippetConverter;
    }

    public bool TryExpand(string lspSnippetText, SnapshotSpan snapshotSpan, ITextView textView)
    {
        var document = textView.TextBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
        if (document is null)
            return false;

        var expansionClientFactory = document.GetLanguageService<ISnippetExpansionClientFactory>();
        if (expansionClientFactory is null)
            return false;

        if (!_snippetParser.TryParse(lspSnippetText, out var lspSnippet))
        {
            return false;
        }

        if (!_snippetConverter.TryConvert(lspSnippet, out var vsSnippet))
        {
            return false;
        }

        var expansionClient = expansionClientFactory.GetSnippetExpansionClient(textView, textView.TextBuffer);
        return expansionClient.TryInsertSpecificExpansion(vsSnippet, snapshotSpan.Span.Start, snapshotSpan.Span.End, CancellationToken.None);
    }
}
