// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using TextSpan = Microsoft.CodeAnalysis.Text.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Snippets.SnippetFunctions
{
    internal abstract class AbstractSnippetFunctionSimpleTypeName : AbstractSnippetFunction
    {
        private readonly string _fieldName;
        private readonly string _fullyQualifiedName;

        public AbstractSnippetFunctionSimpleTypeName(AbstractSnippetExpansionClient snippetExpansionClient, ITextView textView, ITextBuffer subjectBuffer, string fieldName, string fullyQualifiedName)
            : base(snippetExpansionClient, textView, subjectBuffer)
        {
            _fieldName = fieldName;
            _fullyQualifiedName = fullyQualifiedName;
        }

        protected abstract bool TryGetSimplifiedTypeName(Document documentWithFullyQualifiedTypeName, TextSpan updatedTextSpan, CancellationToken cancellationToken, out string simplifiedTypeName);

        protected override void GetDefaultValue(CancellationToken cancellationToken, out string value, out bool hasCurrentValue)
        {
            value = _fullyQualifiedName;
            hasCurrentValue = true;
            if (!TryGetDocument(out var document))
            {
                throw new System.Exception();
            }

            if (!TryGetDocumentWithFullyQualifiedTypeName(document, out var updatedTextSpan, out var documentWithFullyQualifiedTypeName))
            {
                throw new System.Exception();
            }

            if (!TryGetSimplifiedTypeName(documentWithFullyQualifiedTypeName, updatedTextSpan, cancellationToken, out var simplifiedName))
            {
                throw new System.Exception();
            }

            value = simplifiedName;
            hasCurrentValue = true;
        }

        private bool TryGetDocumentWithFullyQualifiedTypeName(Document document, out TextSpan updatedTextSpan, out Document documentWithFullyQualifiedTypeName)
        {
            documentWithFullyQualifiedTypeName = null;
            updatedTextSpan = default;

            var surfaceBufferFieldSpan = snippetExpansionClient.ExpansionSession.GetFieldSpan(_fieldName);

            if (!snippetExpansionClient.TryGetSubjectBufferSpan(surfaceBufferFieldSpan, out var subjectBufferFieldSpan))
            {
                return false;
            }

            var originalTextSpan = new TextSpan(subjectBufferFieldSpan.Start, subjectBufferFieldSpan.Length);
            updatedTextSpan = new TextSpan(subjectBufferFieldSpan.Start, _fullyQualifiedName.Length);

            var textChange = new TextChange(originalTextSpan, _fullyQualifiedName);
            var newText = document.GetTextSynchronously(CancellationToken.None).WithChanges(textChange);

            documentWithFullyQualifiedTypeName = document.WithText(newText);
            return true;
        }
    }
}
