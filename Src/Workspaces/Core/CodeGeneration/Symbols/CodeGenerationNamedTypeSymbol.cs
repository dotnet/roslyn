// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationNamedTypeSymbol : CodeGenerationAbstractNamedTypeSymbol
    {
        private readonly TypeKind typeKind;
        private readonly IList<ITypeParameterSymbol> typeParameters;
        private readonly INamedTypeSymbol baseType;
        private readonly IList<INamedTypeSymbol> interfaces;
        private readonly IList<ISymbol> members;
        private readonly INamedTypeSymbol enumUnderlyingType;

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
            this.typeKind = typeKind;
            this.typeParameters = typeParameters ?? SpecializedCollections.EmptyList<ITypeParameterSymbol>();
            this.baseType = baseType;
            this.interfaces = interfaces ?? SpecializedCollections.EmptyList<INamedTypeSymbol>();
            this.members = members ?? SpecializedCollections.EmptyList<ISymbol>();
            this.enumUnderlyingType = enumUnderlyingType;

            this.OriginalDefinition = this;
        }

        protected override CodeGenerationSymbol Clone()
        {
            return new CodeGenerationNamedTypeSymbol(
                this.ContainingType, this.GetAttributes(), this.DeclaredAccessibility,
                this.Modifiers, this.TypeKind, this.Name, this.typeParameters, this.baseType,
                this.interfaces, this.SpecialType, this.members, this.TypeMembers,
                this.EnumUnderlyingType);
        }

        public override TypeKind TypeKind
        {
            get
            {
                return this.typeKind;
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
                return enumUnderlyingType;
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
                return this.typeParameters.AsImmutable();
            }
        }

        public override INamedTypeSymbol BaseType
        {
            get
            {
                return this.baseType;
            }
        }

        public override ImmutableArray<INamedTypeSymbol> Interfaces
        {
            get
            {
                return this.interfaces.AsImmutable();
            }
        }

        public override ImmutableArray<ISymbol> GetMembers()
        {
            return this.members.Concat(this.TypeMembers).AsImmutable();
        }

        public override ImmutableArray<INamedTypeSymbol> GetTypeMembers()
        {
            return this.TypeMembers.Cast<INamedTypeSymbol>().AsImmutable();
        }

        public override ImmutableArray<IMethodSymbol> InstanceConstructors
        {
            get
            {
                // NOTE(cyrusn): remember to Construct the result if we implement this.
                return this.GetMembers().OfType<IMethodSymbol>()
                                        .Where(m => m.MethodKind == MethodKind.Constructor && !m.IsStatic)
                                        .AsImmutable();
            }
        }

        public override ImmutableArray<IMethodSymbol> StaticConstructors
        {
            get
            {
                // NOTE(cyrusn): remember to Construct the result if we implement this.
                return this.GetMembers().OfType<IMethodSymbol>()
                                        .Where(m => m.MethodKind == MethodKind.StaticConstructor && m.IsStatic)
                                        .AsImmutable();
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