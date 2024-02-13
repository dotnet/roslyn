// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion
{
    [Export(typeof(ILanguageServerSnippetExpander))]
    [Shared]
    internal sealed class LanguageServerSnippetExpanderAdapter : ILanguageServerSnippetExpander
    {
        private readonly ISemanticSnippetExpander _semanticSnippetExpander;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public LanguageServerSnippetExpanderAdapter(ISemanticSnippetExpander semanticSnippetExpander)
        {
            _semanticSnippetExpander = semanticSnippetExpander;
        }

        public bool TryExpand(string lspSnippetText, SnapshotSpan snapshotSpan, ITextView textView)
            => _semanticSnippetExpander.TryExpand(lspSnippetText, snapshotSpan, textView);
    }
}
