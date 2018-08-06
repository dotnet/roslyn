﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A SyntaxReference implementation that lazily translates the result (CSharpSyntaxNode) of the
    /// original syntax reference to a syntax reference for its NamespaceDeclarationSyntax.
    /// </summary>
    internal sealed class NamespaceDeclarationSyntaxReference : TranslationSyntaxReference
    {
        public NamespaceDeclarationSyntaxReference(SyntaxReference reference)
            : base(reference)
        {
        }

        protected override SyntaxNode Translate(SyntaxReference reference, CancellationToken cancellationToken)
        {
            return GetSyntax(reference, cancellationToken);
        }

        internal static SyntaxNode GetSyntax(SyntaxReference reference, CancellationToken cancellationToken)
        {
            var node = (CSharpSyntaxNode)reference.GetSyntax(cancellationToken);

            // If the node is a name syntax, it's something like "X" or "X.Y" in :
            //    namespace X.Y.Z
            // We want to return the full NamespaceDeclarationSyntax.
            while (node is NameSyntax)
            {
                node = node.Parent;
            }

            Debug.Assert(node is CompilationUnitSyntax || node is NamespaceDeclarationSyntax);

            return node;
        }
    }
}
