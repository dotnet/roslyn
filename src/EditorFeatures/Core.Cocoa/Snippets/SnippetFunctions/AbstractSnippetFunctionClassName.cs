// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

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

        protected abstract void GetContainingClassName(Document document, SnapshotSpan subjectBufferFieldSpan, CancellationToken cancellationToken, ref string value, ref bool hasCurrentValue);

        protected override void GetDefaultValue(CancellationToken cancellationToken, out string value, out bool hasDefaultValue)
        {
            hasDefaultValue = false;
            value = string.Empty;
            if (!TryGetDocument(out var document))
            {
                return;
            }

            var surfaceBufferFieldSpan = snippetExpansionClient.ExpansionSession.GetFieldSpan(FieldName);

            if (!snippetExpansionClient.TryGetSubjectBufferSpan(surfaceBufferFieldSpan, out var subjectBufferFieldSpan))
            {
                return;
            }

            GetContainingClassName(document, subjectBufferFieldSpan, cancellationToken, ref value, ref hasDefaultValue);
        }
    }
}
