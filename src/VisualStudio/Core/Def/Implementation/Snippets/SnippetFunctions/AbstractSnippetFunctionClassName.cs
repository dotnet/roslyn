// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        public AbstractSnippetFunctionClassName(AbstractSnippetExpansionClient snippetExpansionClient, ITextBuffer subjectBuffer, string fieldName)
            : base(snippetExpansionClient, subjectBuffer)
        {
            this.FieldName = fieldName;
        }

        protected abstract int GetContainingClassName(Document document, SnapshotSpan subjectBufferFieldSpan, CancellationToken cancellationToken, ref string value, ref int hasDefaultValue);

        protected override int GetDefaultValue(CancellationToken cancellationToken, out string value, out int hasDefaultValue)
        {
            hasDefaultValue = 0;
            value = string.Empty;
            if (!TryGetDocument(out var document))
            {
                return VSConstants.E_FAIL;
            }

            var surfaceBufferFieldSpan = new VsTextSpan[1];
            if (snippetExpansionClient.ExpansionSession.GetFieldSpan(FieldName, surfaceBufferFieldSpan) != VSConstants.S_OK)
            {
                return VSConstants.E_FAIL;
            }

            if (!snippetExpansionClient.TryGetSubjectBufferSpan(surfaceBufferFieldSpan[0], out var subjectBufferFieldSpan))
            {
                return VSConstants.E_FAIL;
            }

            return GetContainingClassName(document, subjectBufferFieldSpan, cancellationToken, ref value, ref hasDefaultValue);
        }
    }
}
