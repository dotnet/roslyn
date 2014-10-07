// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationConstructedNamedTypeSymbol : CodeGenerationAbstractNamedTypeSymbol
    {
        private readonly CodeGenerationAbstractNamedTypeSymbol constructedFrom;
        private readonly IList<ITypeSymbol> typeArguments;

        public CodeGenerationConstructedNamedTypeSymbol(
            CodeGenerationAbstractNamedTypeSymbol constructedFrom,
            IList<ITypeSymbol> typeArguments,
            IList<CodeGenerationAbstractNamedTypeSymbol> typeMembers)
            : base(constructedFrom.ContainingType, constructedFrom.GetAttributes(),
                   constructedFrom.DeclaredAccessibility, constructedFrom.Modifiers,
                   constructedFrom.Name, constructedFrom.SpecialType, typeMembers)
        {
            this.constructedFrom = constructedFrom;
            this.OriginalDefinition = constructedFrom.OriginalDefinition;
            this.typeArguments = typeArguments;
        }

        public override ImmutableArray<ITypeSymbol> TypeArguments
        {
            get
            {
                return this.typeArguments.AsImmutable();
            }
        }

        public override int Arity
        {
            get
            {
                return this.constructedFrom.Arity;
            }
        }

        public override bool IsGenericType
        {
            get
            {
                return this.constructedFrom.IsGenericType;
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
                return this.constructedFrom.IsScriptClass;
            }
        }

        public override bool IsImplicitClass
        {
            get
            {
                return this.constructedFrom.IsImplicitClass;
            }
        }

        public override IEnumerable<string> MemberNames
        {
            get
            {
                return this.constructedFrom.MemberNames;
            }
        }

        public override IMethodSymbol DelegateInvokeMethod
        {
            get
            {
                // NOTE(cyrusn): remember to Construct the result if we implement this.
                return null;
            }
        }

        public override INamedTypeSymbol EnumUnderlyingType
        {
            get
            {
                // NOTE(cyrusn): remember to Construct the result if we implement this.
                return null;
            }
        }

        public override INamedTypeSymbol ConstructedFrom
        {
            get
            {
                return constructedFrom;
            }
        }

        public override INamedTypeSymbol ConstructUnboundGenericType()
        {
            return null;
        }

        public override ImmutableArray<IMethodSymbol> InstanceConstructors
        {
            get
            {
                // TODO(cyrusn): construct these.
                return this.OriginalDefinition.InstanceConstructors;
            }
        }

        public override ImmutableArray<IMethodSymbol> StaticConstructors
        {
            get
            {
                // TODO(cyrusn): construct these.
                return this.OriginalDefinition.StaticConstructors;
            }
        }

        public override ImmutableArray<IMethodSymbol> Constructors
        {
            get
            {
                // TODO(cyrusn): construct these.
                return this.OriginalDefinition.Constructors;
            }
        }

        public override ImmutableArray<INamedTypeSymbol> GetTypeMembers()
        {
            // TODO(cyrusn): construct these.
            return this.constructedFrom.TypeMembers.Cast<INamedTypeSymbol>().AsImmutable();
        }

        public override TypeKind TypeKind
        {
            get
            {
                return this.constructedFrom.TypeKind;
            }
        }

        protected override CodeGenerationSymbol Clone()
        {
            return new CodeGenerationConstructedNamedTypeSymbol(this.constructedFrom, this.typeArguments, this.TypeMembers);
        }

        public override ImmutableArray<ITypeParameterSymbol> TypeParameters
        {
            get
            {
                return constructedFrom.TypeParameters;
            }
        }
    }
}