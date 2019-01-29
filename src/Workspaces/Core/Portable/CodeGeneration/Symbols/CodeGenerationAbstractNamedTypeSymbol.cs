﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal abstract class CodeGenerationAbstractNamedTypeSymbol : CodeGenerationTypeSymbol, INamedTypeSymbol
    {
        public new INamedTypeSymbol OriginalDefinition { get; protected set; }

        public ImmutableArray<IFieldSymbol> TupleElements { get; protected set; }

        internal readonly ImmutableArray<CodeGenerationAbstractNamedTypeSymbol> TypeMembers;

        protected CodeGenerationAbstractNamedTypeSymbol(
            INamedTypeSymbol containingType,
            ImmutableArray<AttributeData> attributes,
            Accessibility declaredAccessibility,
            DeclarationModifiers modifiers,
            string name,
            SpecialType specialType,
            ImmutableArray<CodeGenerationAbstractNamedTypeSymbol> typeMembers)
            : base(containingType, attributes, declaredAccessibility, modifiers, name, specialType)
        {
            this.TypeMembers = typeMembers;

            foreach (var member in typeMembers)
            {
                member.ContainingType = this;
            }
        }

        public override SymbolKind Kind => SymbolKind.NamedType;

        public override void Accept(SymbolVisitor visitor)
        {
            visitor.VisitNamedType(this);
        }

        public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
            return visitor.VisitNamedType(this);
        }

        public INamedTypeSymbol Construct(params ITypeSymbol[] typeArguments)
        {
            if (typeArguments.Length == 0)
            {
                return this;
            }

            return new CodeGenerationConstructedNamedTypeSymbol(
                this, typeArguments.ToImmutableArray(), this.TypeMembers);
        }

        public abstract int Arity { get; }
        public abstract bool IsGenericType { get; }
        public abstract bool IsUnboundGenericType { get; }
        public abstract bool IsScriptClass { get; }
        public abstract bool IsImplicitClass { get; }
        public abstract IEnumerable<string> MemberNames { get; }
        public abstract IMethodSymbol DelegateInvokeMethod { get; }
        public abstract INamedTypeSymbol EnumUnderlyingType { get; }
        public abstract INamedTypeSymbol ConstructedFrom { get; }
        public abstract INamedTypeSymbol ConstructUnboundGenericType();
        public abstract ImmutableArray<IMethodSymbol> InstanceConstructors { get; }
        public abstract ImmutableArray<IMethodSymbol> StaticConstructors { get; }
        public abstract ImmutableArray<IMethodSymbol> Constructors { get; }
        public abstract ImmutableArray<ITypeSymbol> TypeArguments { get; }

        public ImmutableArray<CustomModifier> GetTypeArgumentCustomModifiers(int ordinal)
        {
            if (ordinal < 0 || ordinal >= Arity)
            {
                throw new IndexOutOfRangeException();
            }

            return ImmutableArray.Create<CustomModifier>();
        }

        public abstract ImmutableArray<ITypeParameterSymbol> TypeParameters { get; }

        public override string MetadataName
        {
            get
            {
                return this.Arity > 0
                    ? this.Name + "`" + Arity
                    : base.MetadataName;
            }
        }

        public ISymbol AssociatedSymbol { get; internal set; }

        public bool MightContainExtensionMethods => false;

        public bool IsComImport => false;

        public bool IsUnmanagedType => throw new NotImplementedException();

        public bool IsRefLikeType => throw new NotImplementedException();
    }
}
