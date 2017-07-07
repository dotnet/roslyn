// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Snippets
{
    internal abstract partial class AbstractSnippetFunction : IVsExpansionFunction
    {
        private readonly ITextView _textView;
        private readonly ITextBuffer _subjectBuffer;

        protected AbstractSnippetExpansionClient snippetExpansionClient;

        public AbstractSnippetFunction(AbstractSnippetExpansionClient snippetExpansionClient, ITextView textView, ITextBuffer subjectBuffer)
        {
            this.snippetExpansionClient = snippetExpansionClient;
            _textView = textView;
            _subjectBuffer = subjectBuffer;
        }

        protected bool TryGetDocument(out Document document)
        {
            document = _subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            return document != null;
        }

        protected virtual int GetDefaultValue(CancellationToken cancellationToken, out string value, out int hasCurrentValue)
        {
            value = string.Empty;
            hasCurrentValue = 0;
            return VSConstants.S_OK;
        }

        protected virtual int GetCurrentValue(CancellationToken cancellationToken, out string value, out int hasCurrentValue)
        {
            value = string.Empty;
            hasCurrentValue = 0;
            return VSConstants.S_OK;
        }

        protected virtual int FieldChanged(string field, out int requeryFunction)
        {
            requeryFunction = 0;
            return VSConstants.S_OK;
        }
    }
}
