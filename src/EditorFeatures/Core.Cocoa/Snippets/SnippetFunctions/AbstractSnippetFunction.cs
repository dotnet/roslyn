// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Expansion;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Snippets
{
    internal abstract partial class AbstractSnippetFunction
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

        protected virtual void GetDefaultValue(CancellationToken cancellationToken, out string value, out bool hasDefaultValue)
        {
            value = string.Empty;
            hasDefaultValue = false;
        }

        protected virtual void GetCurrentValue(CancellationToken cancellationToken, out string value, out bool hasCurrentValue)
        {
            value = string.Empty;
            hasCurrentValue = false;
        }

        protected virtual bool FieldChanged(string fieldName)
        {
            return false;
        }

        public uint GetFunctionType()
        {
            throw new System.NotImplementedException();
        }

        public int GetListCount()
        {
            throw new System.NotImplementedException();
        }

        public string GetListText(int index)
        {
            throw new System.NotImplementedException();
        }

        public void ReleaseFunction()
        {
            throw new System.NotImplementedException();
        }
    }
}
