// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Syntax
{
    /// <summary>
    /// This is a SyntaxReference implementation that lazily translates the result (SyntaxNode) of the
    /// original syntax reference to another one.
    /// </summary>
    internal abstract class TranslationSyntaxReference : SyntaxReference
    {
        private readonly SyntaxReference reference;

        protected TranslationSyntaxReference(SyntaxReference reference)
        {
            this.reference = reference;
        }

        public sealed override TextSpan Span
        {
            get { return reference.Span; }
        }

        public sealed override SyntaxTree SyntaxTree
        {
            get { return reference.SyntaxTree; }
        }

        public sealed override SyntaxNode GetSyntax(CancellationToken cancellationToken = default(CancellationToken))
        {
            var node = Translate(reference, cancellationToken);
            Debug.Assert(node.SyntaxTree == this.reference.SyntaxTree);
            return node;
        }

        protected abstract SyntaxNode Translate(SyntaxReference reference, CancellationToken cancellationToken);
    }
}
