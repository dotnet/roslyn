// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationConstructedMethodSymbol : CodeGenerationAbstractMethodSymbol
    {
        private readonly CodeGenerationAbstractMethodSymbol _constructedFrom;
        private readonly ITypeSymbol[] _typeArguments;

        public CodeGenerationConstructedMethodSymbol(
            CodeGenerationAbstractMethodSymbol constructedFrom,
            ITypeSymbol[] typeArguments)
            : base(constructedFrom.ContainingType,
                   constructedFrom.GetAttributes(),
                   constructedFrom.DeclaredAccessibility,
                   constructedFrom.Modifiers,
                   constructedFrom.Name,
                   constructedFrom.GetReturnTypeAttributes())
        {
            _constructedFrom = constructedFrom;
            this.OriginalDefinition = _constructedFrom.OriginalDefinition;
            _typeArguments = typeArguments;
        }

        public override int Arity
        {
            get
            {
                return _constructedFrom.Arity;
            }
        }

        public override bool ReturnsVoid
        {
            get
            {
                return _constructedFrom.ReturnsVoid;
            }
        }

        public override bool ReturnsByRef
        {
            get
            {
                return _constructedFrom.ReturnsByRef;
            }
        }

        public override ITypeSymbol ReturnType
        {
            get
            {
                // TODO(cyrusn): Construct this.
                return _constructedFrom.ReturnType;
            }
        }

        public override ImmutableArray<ITypeSymbol> TypeArguments
        {
            get
            {
                return ImmutableArray.CreateRange(_typeArguments);
            }
        }

        public override ImmutableArray<ITypeParameterSymbol> TypeParameters
        {
            get
            {
                return _constructedFrom.TypeParameters;
            }
        }

        public override ImmutableArray<IParameterSymbol> Parameters
        {
            get
            {
                // TODO(cyrusn): Construct this.
                return this.OriginalDefinition.Parameters;
            }
        }

        public override IMethodSymbol ConstructedFrom
        {
            get
            {
                return _constructedFrom;
            }
        }

        public override IMethodSymbol OverriddenMethod
        {
            get
            {
                // TODO(cyrusn): Construct this.
                return _constructedFrom.OverriddenMethod;
            }
        }

        public override IMethodSymbol ReducedFrom
        {
            get
            {
                // TODO(cyrusn): Construct this.
                return _constructedFrom.ReducedFrom;
            }
        }

        public override ITypeSymbol GetTypeInferredDuringReduction(ITypeParameterSymbol reducedFromTypeParameter)
        {
            throw new System.InvalidOperationException();
        }

        public override IMethodSymbol ReduceExtensionMethod(ITypeSymbol receiverType)
        {
            // TODO(cyrusn): support this properly.
            return null;
        }

        public override ImmutableArray<IMethodSymbol> ExplicitInterfaceImplementations
        {
            get
            {
                // TODO(cyrusn): Construct this.
                return _constructedFrom.ExplicitInterfaceImplementations;
            }
        }

        public override IMethodSymbol PartialDefinitionPart
        {
            get
            {
                // TODO(cyrusn): Construct this.
                return _constructedFrom.PartialDefinitionPart;
            }
        }

        public override IMethodSymbol PartialImplementationPart
        {
            get
            {
                // TODO(cyrusn): Construct this.
                return _constructedFrom.PartialImplementationPart;
            }
        }

        protected override CodeGenerationSymbol Clone()
        {
            return new CodeGenerationConstructedMethodSymbol(_constructedFrom, _typeArguments);
        }
    }
}
