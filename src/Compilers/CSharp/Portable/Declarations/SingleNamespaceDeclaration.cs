// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal class SingleNamespaceDeclaration : SingleNamespaceOrTypeDeclaration
    {
        private readonly ImmutableArray<SingleNamespaceOrTypeDeclaration> _children;

        /// <summary>
        /// The <see cref="SyntaxReference"/> to the <see cref="CompilationUnitSyntax"/> or
        /// <see cref="NamespaceDeclarationSyntax"/> for this namespace.  This differs from
        /// <see cref="SingleNamespaceOrTypeDeclaration.SyntaxReference"/> which may just 
        /// point to the <see cref="NameSyntax"/> node for this namespace's name.
        /// </summary>
        public readonly SyntaxReference FullDeclarationSyntaxReference;

        protected SingleNamespaceDeclaration(
            string name,
            SyntaxReference syntaxReference,
            SyntaxReference fullDeclarationSyntaxReference,
            SourceLocation nameLocation,
            ImmutableArray<SingleNamespaceOrTypeDeclaration> children,
            ImmutableArray<Diagnostic> diagnostics)
            : base(name, syntaxReference, nameLocation, diagnostics)
        {
            FullDeclarationSyntaxReference = fullDeclarationSyntaxReference;
            _children = children;
        }

        public override DeclarationKind Kind
        {
            get
            {
                return DeclarationKind.Namespace;
            }
        }

        protected override ImmutableArray<SingleNamespaceOrTypeDeclaration> GetNamespaceOrTypeDeclarationChildren()
        {
            return _children;
        }

        public virtual bool HasUsings
        {
            get
            {
                return false;
            }
        }

        public virtual bool HasExternAliases
        {
            get
            {
                return false;
            }
        }

        public static SingleNamespaceDeclaration Create(
            string name,
            bool hasUsings,
            bool hasExternAliases,
            SyntaxReference syntaxReference,
            SyntaxReference fullDeclarationSyntaxReference,
            SourceLocation nameLocation,
            ImmutableArray<SingleNamespaceOrTypeDeclaration> children,
            ImmutableArray<Diagnostic> diagnostics)
        {
            // By far the most common case is "no usings and no extern aliases", so optimize for
            // that to minimize space. The other cases are not frequent enough to warrant their own
            // custom types.
            if (!hasUsings && !hasExternAliases)
            {
                return new SingleNamespaceDeclaration(
                    name, syntaxReference, fullDeclarationSyntaxReference, nameLocation, children, diagnostics);
            }
            else
            {
                return new SingleNamespaceDeclarationEx(
                    name, hasUsings, hasExternAliases, syntaxReference, fullDeclarationSyntaxReference, nameLocation, children, diagnostics);
            }
        }
    }
}
