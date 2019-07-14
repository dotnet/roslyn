// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationNamedTypeSymbol : CodeGenerationAbstractNamedTypeSymbol
    {
        private readonly TypeKind _typeKind;
        private readonly ImmutableArray<ITypeParameterSymbol> _typeParameters;
        private readonly INamedTypeSymbol _baseType;
        private readonly ImmutableArray<INamedTypeSymbol> _interfaces;
        private readonly ImmutableArray<ISymbol> _members;
        private readonly INamedTypeSymbol _enumUnderlyingType;

        public CodeGenerationNamedTypeSymbol(
            INamedTypeSymbol containingType,
            ImmutableArray<AttributeData> attributes,
            Accessibility declaredAccessibility,
            DeclarationModifiers modifiers,
            TypeKind typeKind,
            string name,
            ImmutableArray<ITypeParameterSymbol> typeParameters,
            INamedTypeSymbol baseType,
            ImmutableArray<INamedTypeSymbol> interfaces,
            SpecialType specialType,
            ImmutableArray<ISymbol> members,
            ImmutableArray<CodeGenerationAbstractNamedTypeSymbol> typeMembers,
            INamedTypeSymbol enumUnderlyingType)
            : base(containingType, attributes, declaredAccessibility, modifiers, name, specialType, typeMembers)
        {
            _typeKind = typeKind;
            _typeParameters = typeParameters.NullToEmpty();
            _baseType = baseType;
            _interfaces = interfaces.NullToEmpty();
            _members = members.NullToEmpty();
            _enumUnderlyingType = enumUnderlyingType;

            this.OriginalDefinition = this;
        }

        protected override CodeGenerationSymbol Clone()
        {
            return new CodeGenerationNamedTypeSymbol(
                this.ContainingType, this.GetAttributes(), this.DeclaredAccessibility,
                this.Modifiers, this.TypeKind, this.Name, _typeParameters, _baseType,
                _interfaces, this.SpecialType, _members, this.TypeMembers,
                this.EnumUnderlyingType);
        }

        public override TypeKind TypeKind => _typeKind;

        public override SymbolKind Kind => SymbolKind.NamedType;

        public override int Arity => this.TypeParameters.Length;

        public override bool IsGenericType
        {
            get
            {
                return this.Arity > 0;
            }
        }

        public override bool IsUnboundGenericType => false;

        public override bool IsScriptClass => false;

        public override bool IsImplicitClass => false;

        public override IEnumerable<string> MemberNames
        {
            get
            {
                return this.GetMembers().Select(m => m.Name).ToList();
            }
        }

        public override IMethodSymbol DelegateInvokeMethod
        {
            get
            {
                return this.TypeKind == TypeKind.Delegate
                    ? this.GetMembers(WellKnownMemberNames.DelegateInvokeName).OfType<IMethodSymbol>().FirstOrDefault()
                    : null;
            }
        }

        public override INamedTypeSymbol EnumUnderlyingType => _enumUnderlyingType;

        public override INamedTypeSymbol ConstructedFrom
        {
            get
            {
                return this;
            }
        }

        public override INamedTypeSymbol ConstructUnboundGenericType()
        {
            return null;
        }

        public ImmutableArray<ISymbol> CandidateSymbols
        {
            get
            {
                return ImmutableArray.Create<ISymbol>();
            }
        }

        public override ImmutableArray<ITypeSymbol> TypeArguments
        {
            get
            {
                return this.TypeParameters.As<ITypeSymbol>();
            }
        }

        public override ImmutableArray<NullableAnnotation> TypeArgumentNullableAnnotations
        {
            get
            {
                // TODO: what should this be?
                return this.TypeParameters.SelectAsArray(t => NullableAnnotation.NotAnnotated);
            }
        }

        public override ImmutableArray<ITypeParameterSymbol> TypeParameters
        {
            get
            {
                return ImmutableArray.CreateRange(_typeParameters);
            }
        }

        public override INamedTypeSymbol BaseType => _baseType;

        public override ImmutableArray<INamedTypeSymbol> Interfaces
        {
            get
            {
                return ImmutableArray.CreateRange(_interfaces);
            }
        }

        public override ImmutableArray<ISymbol> GetMembers()
        {
            return ImmutableArray.CreateRange(_members.Concat(this.TypeMembers));
        }

        public override ImmutableArray<INamedTypeSymbol> GetTypeMembers()
        {
            return ImmutableArray.CreateRange(this.TypeMembers.Cast<INamedTypeSymbol>());
        }

        public override ImmutableArray<IMethodSymbol> InstanceConstructors
        {
            get
            {
                // NOTE(cyrusn): remember to Construct the result if we implement this.
                return ImmutableArray.CreateRange(
                    this.GetMembers().OfType<IMethodSymbol>().Where(m => m.MethodKind == MethodKind.Constructor && !m.IsStatic));
            }
        }

        public override ImmutableArray<IMethodSymbol> StaticConstructors
        {
            get
            {
                // NOTE(cyrusn): remember to Construct the result if we implement this.
                return ImmutableArray.CreateRange(
                    this.GetMembers().OfType<IMethodSymbol>().Where(m => m.MethodKind == MethodKind.StaticConstructor && m.IsStatic));
            }
        }

        public override ImmutableArray<IMethodSymbol> Constructors
        {
            get
            {
                return InstanceConstructors.AddRange(StaticConstructors);
            }
        }
    }
}
