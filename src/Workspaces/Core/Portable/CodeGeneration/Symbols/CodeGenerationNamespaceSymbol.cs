// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Editing;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationNamespaceSymbol : CodeGenerationNamespaceOrTypeSymbol, INamespaceSymbol
    {
        private readonly IList<INamespaceOrTypeSymbol> _members;

        public CodeGenerationNamespaceSymbol(string name, IList<INamespaceOrTypeSymbol> members)
            : base(null, default, Accessibility.NotApplicable, default, name)
        {
            _members = members ?? SpecializedCollections.EmptyList<INamespaceOrTypeSymbol>();
        }

        public override bool IsNamespace => true;

        public override bool IsType => false;

        protected override CodeGenerationSymbol Clone()
        {
            return new CodeGenerationNamespaceSymbol(this.Name, _members);
        }

        public override SymbolKind Kind => SymbolKind.Namespace;

        public override void Accept(SymbolVisitor visitor)
        {
            visitor.VisitNamespace(this);
        }

        public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
            return visitor.VisitNamespace(this);
        }

        public new IEnumerable<INamespaceOrTypeSymbol> GetMembers()
        {
            return _members;
        }

        IEnumerable<INamespaceOrTypeSymbol> INamespaceSymbol.GetMembers(string name)
        {
            return GetMembers().Where(m => m.Name == name);
        }

        public IEnumerable<INamespaceSymbol> GetNamespaceMembers()
        {
            return GetMembers().OfType<INamespaceSymbol>();
        }

        public bool IsGlobalNamespace
        {
            get
            {
                return this.Name == string.Empty;
            }
        }

        public NamespaceKind NamespaceKind => NamespaceKind.Module;

        public Compilation ContainingCompilation => null;

        public INamedTypeSymbol ImplicitType => null;

        public ImmutableArray<INamespaceSymbol> ConstituentNamespaces
        {
            get
            {
                return ImmutableArray.Create<INamespaceSymbol>(this);
            }
        }
    }
}
