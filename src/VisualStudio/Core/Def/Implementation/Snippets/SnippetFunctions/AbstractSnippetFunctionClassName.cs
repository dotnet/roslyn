// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Snippets
{
    internal abstract class AbstractSnippetFunctionClassName : AbstractSnippetFunction
    {
        protected readonly string FieldName;

        public AbstractSnippetFunctionClassName(AbstractSnippetExpansionClient snippetExpansionClient, ITextView textView, ITextBuffer subjectBuffer, string fieldName)
            : base(snippetExpansionClient, textView, subjectBuffer)
        {
            this.FieldName = fieldName;
        }

        protected abstract int GetContainingClassName(Document document, SnapshotSpan subjectBufferFieldSpan, CancellationToken cancellationToken, ref string value, ref int hasDefaultValue);

        protected override int GetDefaultValue(CancellationToken cancellationToken, out string value, out int hasDefaultValue)
        {
            hasDefaultValue = 0;
            value = string.Empty;

            Document document;
            if (!TryGetDocument(out document))
            {
                return VSConstants.E_FAIL;
            }

            var surfaceBufferFieldSpan = new VsTextSpan[1];
            if (snippetExpansionClient.ExpansionSession.GetFieldSpan(FieldName, surfaceBufferFieldSpan) != VSConstants.S_OK)
            {
                return VSConstants.E_FAIL;
            }

            SnapshotSpan subjectBufferFieldSpan;
            if (!snippetExpansionClient.TryGetSubjectBufferSpan(surfaceBufferFieldSpan[0], out subjectBufferFieldSpan))
            {
                return VSConstants.E_FAIL;
            }

            return GetContainingClassName(document, subjectBufferFieldSpan, cancellationToken, ref value, ref hasDefaultValue);
        }
    }
}
