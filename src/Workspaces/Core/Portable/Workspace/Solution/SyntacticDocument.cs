// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    internal class SyntacticDocument
    {
        public readonly Document Document;
        public readonly SourceText Text;
        public readonly SyntaxTree SyntaxTree;
        public readonly SyntaxNode Root;

        protected SyntacticDocument(Document document, SourceText text, SyntaxTree tree, SyntaxNode root)
        {
            this.Document = document;
            this.Text = text;
            this.SyntaxTree = tree;
            this.Root = root;
        }

        public Project Project => this.Document.Project;

        public static async Task<SyntacticDocument> CreateAsync(
            Document document, CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return new SyntacticDocument(document, text, root.SyntaxTree, root);
        }
    }
}
