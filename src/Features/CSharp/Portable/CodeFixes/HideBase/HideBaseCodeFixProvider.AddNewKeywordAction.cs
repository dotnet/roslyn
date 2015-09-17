// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using System.Threading;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.HideBase
{
    internal partial class HideBaseCodeFixProvider
    {
        private class AddNewKeywordAction : CodeActions.CodeAction
        {
            private Document _document;
            private SyntaxNode _node;
            private SyntaxNode _newNode;

            public override string Title
            {
                get
                {
                    return CSharpFeaturesResources.HideBase;
                }
            }

            public AddNewKeywordAction(Document document, SyntaxNode node, SyntaxNode newNode)
            {
                _document = document;
                _node = node;
                _newNode = newNode;
            }

            protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                var root = await _document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                var newRoot = root.ReplaceNode(_node, _newNode);
                var newDocument = await Formatter.FormatAsync(_document.WithSyntaxRoot(newRoot)).ConfigureAwait(false);

                return newDocument;
            }
        }
    }
}
