// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationConstructedNamedTypeSymbol : CodeGenerationAbstractNamedTypeSymbol
    {
        private readonly CodeGenerationAbstractNamedTypeSymbol _constructedFrom;
        private readonly ImmutableArray<ITypeSymbol> _typeArguments;

        public CodeGenerationConstructedNamedTypeSymbol(
            CodeGenerationAbstractNamedTypeSymbol constructedFrom,
            ImmutableArray<ITypeSymbol> typeArguments,
            ImmutableArray<CodeGenerationAbstractNamedTypeSymbol> typeMembers)
            : base(constructedFrom.ContainingType, constructedFrom.GetAttributes(),
                   constructedFrom.DeclaredAccessibility, constructedFrom.Modifiers,
                   constructedFrom.Name, constructedFrom.SpecialType, typeMembers)
        {
            _constructedFrom = constructedFrom;
            this.OriginalDefinition = constructedFrom.OriginalDefinition;
            _typeArguments = typeArguments;
        }

        public override ImmutableArray<ITypeSymbol> TypeArguments => _typeArguments;

        public override ImmutableArray<NullableAnnotation> TypeArgumentNullableAnnotations => _typeArguments.SelectAsArray(t => t.GetNullability());

        public override int Arity => _constructedFrom.Arity;

        public override bool IsGenericType => _constructedFrom.IsGenericType;

        public override bool IsUnboundGenericType => false;

        public override bool IsScriptClass => _constructedFrom.IsScriptClass;

        public override bool IsImplicitClass => _constructedFrom.IsImplicitClass;

        public override IEnumerable<string> MemberNames => _constructedFrom.MemberNames;

        public override IMethodSymbol DelegateInvokeMethod =>
                // NOTE(cyrusn): remember to Construct the result if we implement this.
                null;

        public override INamedTypeSymbol EnumUnderlyingType =>
                // NOTE(cyrusn): remember to Construct the result if we implement this.
                null;

        public override INamedTypeSymbol ConstructedFrom => _constructedFrom;

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
            return ImmutableArray.CreateRange(_constructedFrom.TypeMembers.Cast<INamedTypeSymbol>());
        }

        public override TypeKind TypeKind => _constructedFrom.TypeKind;

        protected override CodeGenerationSymbol Clone()
        {
            return new CodeGenerationConstructedNamedTypeSymbol(_constructedFrom, _typeArguments, this.TypeMembers);
        }

        public override ImmutableArray<ITypeParameterSymbol> TypeParameters => _constructedFrom.TypeParameters;
    }
}
