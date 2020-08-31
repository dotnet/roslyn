// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Snippets
{
    internal abstract partial class AbstractSnippetFunction : IVsExpansionFunction
    {
        private readonly ITextBuffer _subjectBuffer;

        protected AbstractSnippetExpansionClient snippetExpansionClient;

        public AbstractSnippetFunction(AbstractSnippetExpansionClient snippetExpansionClient, ITextBuffer subjectBuffer)
        {
            this.snippetExpansionClient = snippetExpansionClient;
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
