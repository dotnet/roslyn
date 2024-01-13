// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    internal class SyntacticDocument
    {
        public readonly Document Document;
        public readonly SourceText Text;
        public readonly SyntaxNode Root;

        protected SyntacticDocument(Document document, SourceText text, SyntaxNode root)
        {
            Document = document;
            Text = text;
            Root = root;
        }

        public Project Project => Document.Project;
        public SyntaxTree SyntaxTree => Root.SyntaxTree;

        public static async ValueTask<SyntacticDocument> CreateAsync(Document document, CancellationToken cancellationToken)
        {
            var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return new SyntacticDocument(document, text, root);
        }

        public ValueTask<SyntacticDocument> WithSyntaxRootAsync(SyntaxNode root, CancellationToken cancellationToken)
        {
            var newDocument = this.Document.WithSyntaxRoot(root);
            return CreateAsync(newDocument, cancellationToken);
        }
    }
}
