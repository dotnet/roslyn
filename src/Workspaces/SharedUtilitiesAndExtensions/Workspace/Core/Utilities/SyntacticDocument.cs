// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
