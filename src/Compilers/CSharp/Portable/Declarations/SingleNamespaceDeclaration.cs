// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal class SingleNamespaceDeclaration : SingleNamespaceOrTypeDeclaration
    {
        private readonly ImmutableArray<SingleNamespaceOrTypeDeclaration> _children;

        protected SingleNamespaceDeclaration(
            string name,
            SyntaxReference syntaxReference,
            SourceLocation nameLocation,
            ImmutableArray<SingleNamespaceOrTypeDeclaration> children,
            ImmutableArray<Diagnostic> diagnostics)
            : base(name, syntaxReference, nameLocation, diagnostics)
        {
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

        public virtual bool HasGlobalUsings
        {
            get
            {
                return false;
            }
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
                    name, syntaxReference, nameLocation, children, diagnostics);
            }
            else
            {
                return new SingleNamespaceDeclarationEx(
                    name, hasUsings, hasExternAliases, syntaxReference, nameLocation, children, diagnostics);
            }
        }
    }
}
