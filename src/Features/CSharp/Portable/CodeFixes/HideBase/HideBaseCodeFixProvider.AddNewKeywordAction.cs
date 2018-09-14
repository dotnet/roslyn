// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using System.Threading;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.HideBase
{
#pragma warning disable RS1016 // Code fix providers should provide FixAll support. https://github.com/dotnet/roslyn/issues/23528
    internal partial class HideBaseCodeFixProvider
#pragma warning restore RS1016 // Code fix providers should provide FixAll support.
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
