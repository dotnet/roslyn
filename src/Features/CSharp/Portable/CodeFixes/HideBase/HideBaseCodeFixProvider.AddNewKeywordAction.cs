// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using System.Threading;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.HideBase
{
    internal partial class HideBaseCodeFixProvider
    {
        private class AddNewKeywordAction : CodeActions.CodeAction
        {
            private Document _document;
            private SyntaxNode _node;

            public override string Title => CSharpFeaturesResources.Hide_base_member;

            public AddNewKeywordAction(Document document, SyntaxNode node)
            {
                _document = document;
                _node = node;
            }

            protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                var root = await _document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                var newNode = GetNewNode(_document, _node, cancellationToken);
                var newRoot = root.ReplaceNode(_node, newNode);

                return _document.WithSyntaxRoot(newRoot);
            }

            private SyntaxNode GetNewNode(Document document, SyntaxNode node, CancellationToken cancellationToken)
            {
                var generator = SyntaxGenerator.GetGenerator(_document);
                return generator.WithModifiers(node, generator.GetModifiers(node).WithIsNew(true));
            }
        }
    }
}
