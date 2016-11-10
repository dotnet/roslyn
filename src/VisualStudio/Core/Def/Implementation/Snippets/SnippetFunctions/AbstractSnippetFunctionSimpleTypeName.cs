// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;
using TextSpan = Microsoft.CodeAnalysis.Text.TextSpan;
using VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

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

        protected override int GetDefaultValue(CancellationToken cancellationToken, out string value, out int hasDefaultValue)
        {
            value = _fullyQualifiedName;
            hasDefaultValue = 1;

            Document document;
            if (!TryGetDocument(out document))
            {
                return VSConstants.E_FAIL;
            }

            Document documentWithFullyQualifiedTypeName;
            TextSpan updatedTextSpan;
            if (!TryGetDocumentWithFullyQualifiedTypeName(document, out updatedTextSpan, out documentWithFullyQualifiedTypeName))
            {
                return VSConstants.E_FAIL;
            }

            string simplifiedName;
            if (!TryGetSimplifiedTypeName(documentWithFullyQualifiedTypeName, updatedTextSpan, cancellationToken, out simplifiedName))
            {
                return VSConstants.E_FAIL;
            }

            value = simplifiedName;
            hasDefaultValue = 1;
            return VSConstants.S_OK;
        }

        private bool TryGetDocumentWithFullyQualifiedTypeName(Document document, out TextSpan updatedTextSpan, out Document documentWithFullyQualifiedTypeName)
        {
            documentWithFullyQualifiedTypeName = null;
            updatedTextSpan = default(TextSpan);

            var surfaceBufferFieldSpan = new VsTextSpan[1];
            if (snippetExpansionClient.ExpansionSession.GetFieldSpan(_fieldName, surfaceBufferFieldSpan) != VSConstants.S_OK)
            {
                return false;
            }

            SnapshotSpan subjectBufferFieldSpan;
            if (!snippetExpansionClient.TryGetSubjectBufferSpan(surfaceBufferFieldSpan[0], out subjectBufferFieldSpan))
            {
                return false;
            }

            var originalTextSpan = new TextSpan(subjectBufferFieldSpan.Start, subjectBufferFieldSpan.Length);
            updatedTextSpan = new TextSpan(subjectBufferFieldSpan.Start, _fullyQualifiedName.Length);

            var textChange = new TextChange(originalTextSpan, _fullyQualifiedName);
            var newText = document.GetTextAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None).WithChanges(textChange);

            documentWithFullyQualifiedTypeName = document.WithText(newText);
            return true;
        }
    }
}