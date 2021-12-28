// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
