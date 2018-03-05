// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal abstract class CodeGenerationNamespaceOrTypeSymbol : CodeGenerationSymbol, INamespaceOrTypeSymbol
    {
        protected CodeGenerationNamespaceOrTypeSymbol(
            INamedTypeSymbol containingType,
            ImmutableArray<AttributeData> attributes,
            Accessibility declaredAccessibility,
            DeclarationModifiers modifiers,
            string name)
            : base(containingType, attributes, declaredAccessibility, modifiers, name)
        {
        }

        public virtual ImmutableArray<ISymbol> GetMembers()
        {
            return ImmutableArray.Create<ISymbol>();
        }

        public ImmutableArray<ISymbol> GetMembers(string name)
        {
            return GetMembers().WhereAsArray(s => s.Name == name);
        }

        public virtual ImmutableArray<INamedTypeSymbol> GetTypeMembers()
        {
            return ImmutableArray.Create<INamedTypeSymbol>();
        }

        public ImmutableArray<INamedTypeSymbol> GetTypeMembers(string name)
        {
            return GetTypeMembers().WhereAsArray(s => s.Name == name);
        }

        public ImmutableArray<INamedTypeSymbol> GetTypeMembers(string name, int arity)
        {
            return GetTypeMembers(name).WhereAsArray(n => n.Arity == arity);
        }

        public abstract bool IsNamespace { get; }

        public abstract bool IsType { get; }
    }
}
