// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Snippets
{
    internal abstract partial class AbstractSnippetFunction
    {
#pragma warning disable IDE0052 // Remove unread private members
        private readonly ITextView _textView;
#pragma warning restore IDE0052 // Remove unread private members
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
