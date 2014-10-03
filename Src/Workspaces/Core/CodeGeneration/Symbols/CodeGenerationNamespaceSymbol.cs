// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationNamespaceSymbol : CodeGenerationNamespaceOrTypeSymbol, INamespaceSymbol
    {
        private readonly IList<INamespaceOrTypeSymbol> members;

        public CodeGenerationNamespaceSymbol(string name, IList<INamespaceOrTypeSymbol> members)
            : base(null, null, Accessibility.NotApplicable, default(DeclarationModifiers), name)
        {
            this.members = members ?? SpecializedCollections.EmptyList<INamespaceOrTypeSymbol>();
        }

        public override bool IsNamespace
        {
            get
            {
                return true;
            }
        }

        public override bool IsType
        {
            get
            {
                return false;
            }
        }

        protected override CodeGenerationSymbol Clone()
        {
            return new CodeGenerationNamespaceSymbol(this.Name, this.members);
        }

        public override SymbolKind Kind
        {
            get
            {
                return SymbolKind.Namespace;
            }
        }

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
            return members;
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

        public NamespaceKind NamespaceKind
        {
            get { return NamespaceKind.Module; }
        }

        public Compilation ContainingCompilation
        {
            get { return null; }
        }

        public INamedTypeSymbol ImplicitType
        {
            get
            {
                return null;
            }
        }

        public ImmutableArray<INamespaceSymbol> ConstituentNamespaces
        {
            get
            {
                return ImmutableArray.Create<INamespaceSymbol>(this);
            }
        }
    }
}