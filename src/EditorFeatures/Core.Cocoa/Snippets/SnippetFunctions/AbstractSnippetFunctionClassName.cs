// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis;
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
