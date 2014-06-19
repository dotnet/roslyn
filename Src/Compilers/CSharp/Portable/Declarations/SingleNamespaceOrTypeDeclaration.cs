// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract class SingleNamespaceOrTypeDeclaration : Declaration
    {
        private readonly SyntaxReference syntaxReference;
        private readonly SourceLocation nameLocation;

        protected SingleNamespaceOrTypeDeclaration(
            string name,
            SyntaxReference syntaxReference,
            SourceLocation nameLocation)
            : base(name)
        {
            this.syntaxReference = syntaxReference;
            this.nameLocation = nameLocation;
        }

        public SourceLocation Location
        {
            get
            {
                return new SourceLocation(this.SyntaxReference);
            }
        }

        public SyntaxReference SyntaxReference
        {
            get
            {
                return syntaxReference;
            }
        }

        public SourceLocation NameLocation
        {
            get
            {
                return nameLocation;
            }
        }

        protected override ImmutableArray<Declaration> GetDeclarationChildren()
        {
            return StaticCast<Declaration>.From(this.GetNamespaceOrTypeDeclarationChildren());
        }

        public new ImmutableArray<SingleNamespaceOrTypeDeclaration> Children
        {
            get
            {
                return this.GetNamespaceOrTypeDeclarationChildren();
            }
        }

        protected abstract ImmutableArray<SingleNamespaceOrTypeDeclaration> GetNamespaceOrTypeDeclarationChildren();
    }
}