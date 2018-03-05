// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract class SingleNamespaceOrTypeDeclaration : Declaration
    {
        private readonly SyntaxReference _syntaxReference;
        private readonly SourceLocation _nameLocation;

        /// <summary>
        /// Any diagnostics reported while converting the Namespace/Type syntax into the Declaration
        /// instance.  Generally, we determine and store some diagnostics here because we don't want 
        /// to have to go back to Syntax when we have our NamespaceSymbol or NamedTypeSymbol.
        /// </summary>
        public readonly ImmutableArray<Diagnostic> Diagnostics;

        protected SingleNamespaceOrTypeDeclaration(
            string name,
            SyntaxReference syntaxReference,
            SourceLocation nameLocation,
            ImmutableArray<Diagnostic> diagnostics)
            : base(name)
        {
            _syntaxReference = syntaxReference;
            _nameLocation = nameLocation;
            Diagnostics = diagnostics;
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
                return _syntaxReference;
            }
        }

        public SourceLocation NameLocation
        {
            get
            {
                return _nameLocation;
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
