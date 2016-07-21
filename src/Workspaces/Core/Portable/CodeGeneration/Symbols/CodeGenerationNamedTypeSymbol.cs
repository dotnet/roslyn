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
        private readonly IList<ITypeParameterSymbol> _typeParameters;
        private readonly INamedTypeSymbol _baseType;
        private readonly IList<INamedTypeSymbol> _interfaces;
        private readonly IList<ISymbol> _members;
        private readonly INamedTypeSymbol _enumUnderlyingType;

        public CodeGenerationNamedTypeSymbol(
            INamedTypeSymbol containingType,
            IList<AttributeData> attributes,
            Accessibility declaredAccessibility,
            DeclarationModifiers modifiers,
            TypeKind typeKind,
            string name,
            IList<ITypeParameterSymbol> typeParameters,
            INamedTypeSymbol baseType,
            IList<INamedTypeSymbol> interfaces,
            SpecialType specialType,
            IList<ISymbol> members,
            IList<CodeGenerationAbstractNamedTypeSymbol> typeMembers,
            INamedTypeSymbol enumUnderlyingType)
            : base(containingType, attributes, declaredAccessibility, modifiers, name, specialType, typeMembers)
        {
            _typeKind = typeKind;
            _typeParameters = typeParameters ?? SpecializedCollections.EmptyList<ITypeParameterSymbol>();
            _baseType = baseType;
            _interfaces = interfaces ?? SpecializedCollections.EmptyList<INamedTypeSymbol>();
            _members = members ?? SpecializedCollections.EmptyList<ISymbol>();
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

        public override TypeKind TypeKind
        {
            get
            {
                return _typeKind;
            }
        }

        public override SymbolKind Kind
        {
            get
            {
                return SymbolKind.NamedType;
            }
        }

        public override int Arity
        {
            get
            {
                return this.TypeParameters.Length;
            }
        }

        public override bool IsGenericType
        {
            get
            {
                return this.Arity > 0;
            }
        }

        public override bool IsUnboundGenericType
        {
            get
            {
                return false;
            }
        }

        public override bool IsScriptClass
        {
            get
            {
                return false;
            }
        }

        public override bool IsImplicitClass
        {
            get
            {
                return false;
            }
        }

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

        public override INamedTypeSymbol EnumUnderlyingType
        {
            get
            {
                return _enumUnderlyingType;
            }
        }

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

        public override ImmutableArray<ITypeParameterSymbol> TypeParameters
        {
            get
            {
                return ImmutableArray.CreateRange(_typeParameters);
            }
        }

        public override INamedTypeSymbol BaseType
        {
            get
            {
                return _baseType;
            }
        }

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
