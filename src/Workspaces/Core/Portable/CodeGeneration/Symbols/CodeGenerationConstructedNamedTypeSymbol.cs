// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationConstructedNamedTypeSymbol : CodeGenerationAbstractNamedTypeSymbol
    {
        private readonly CodeGenerationAbstractNamedTypeSymbol _constructedFrom;
        private readonly IList<ITypeSymbol> _typeArguments;

        public CodeGenerationConstructedNamedTypeSymbol(
            CodeGenerationAbstractNamedTypeSymbol constructedFrom,
            IList<ITypeSymbol> typeArguments,
            IList<CodeGenerationAbstractNamedTypeSymbol> typeMembers)
            : base(constructedFrom.ContainingType, constructedFrom.GetAttributes(),
                   constructedFrom.DeclaredAccessibility, constructedFrom.Modifiers,
                   constructedFrom.Name, constructedFrom.SpecialType, typeMembers)
        {
            _constructedFrom = constructedFrom;
            this.OriginalDefinition = constructedFrom.OriginalDefinition;
            _typeArguments = typeArguments;
        }

        public override ImmutableArray<ITypeSymbol> TypeArguments
        {
            get
            {
                return ImmutableArray.CreateRange(_typeArguments);
            }
        }

        public override int Arity
        {
            get
            {
                return _constructedFrom.Arity;
            }
        }

        public override bool IsGenericType
        {
            get
            {
                return _constructedFrom.IsGenericType;
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
                return _constructedFrom.IsScriptClass;
            }
        }

        public override bool IsImplicitClass
        {
            get
            {
                return _constructedFrom.IsImplicitClass;
            }
        }

        public override IEnumerable<string> MemberNames
        {
            get
            {
                return _constructedFrom.MemberNames;
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
                return _constructedFrom;
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
            return ImmutableArray.CreateRange(_constructedFrom.TypeMembers.Cast<INamedTypeSymbol>());
        }

        public override TypeKind TypeKind
        {
            get
            {
                return _constructedFrom.TypeKind;
            }
        }

        protected override CodeGenerationSymbol Clone()
        {
            return new CodeGenerationConstructedNamedTypeSymbol(_constructedFrom, _typeArguments, this.TypeMembers);
        }

        public override ImmutableArray<ITypeParameterSymbol> TypeParameters
        {
            get
            {
                return _constructedFrom.TypeParameters;
            }
        }
    }
}
