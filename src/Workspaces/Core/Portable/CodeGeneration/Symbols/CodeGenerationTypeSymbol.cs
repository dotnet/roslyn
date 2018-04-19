﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editing;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal abstract class CodeGenerationTypeSymbol : CodeGenerationNamespaceOrTypeSymbol, ITypeSymbol
    {
        public SpecialType SpecialType { get; protected set; }

        protected CodeGenerationTypeSymbol(
            INamedTypeSymbol containingType,
            ImmutableArray<AttributeData> attributes,
            Accessibility declaredAccessibility,
            DeclarationModifiers modifiers,
            string name,
            SpecialType specialType)
            : base(containingType, attributes, declaredAccessibility, modifiers, name)
        {
            this.SpecialType = specialType;
        }

        public abstract TypeKind TypeKind { get; }

        public virtual INamedTypeSymbol BaseType => null;

        public virtual ImmutableArray<INamedTypeSymbol> Interfaces
            => ImmutableArray.Create<INamedTypeSymbol>();

        public ImmutableArray<INamedTypeSymbol> AllInterfaces
            => ImmutableArray.Create<INamedTypeSymbol>();

        public bool IsReferenceType => false;

        public bool IsValueType => false;

        public bool IsAnonymousType => false;

        public bool IsTupleType => false;

        public ImmutableArray<ITypeSymbol> TupleElementTypes => default;

        public ImmutableArray<string> TupleElementNames => default;

        public INamedTypeSymbol TupleUnderlyingType => null;

        public new ITypeSymbol OriginalDefinition => this;

        public ISymbol FindImplementationForInterfaceMember(ISymbol interfaceMember) => null;

        public override bool IsNamespace => false;

        public override bool IsType => true;

        public bool IsSerializable => false;
    }
}
