// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationConstructedMethodSymbol : CodeGenerationAbstractMethodSymbol
    {
        private readonly CodeGenerationAbstractMethodSymbol constructedFrom;
        private readonly ITypeSymbol[] typeArguments;

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
            this.constructedFrom = constructedFrom;
            this.OriginalDefinition = this.constructedFrom.OriginalDefinition;
            this.typeArguments = typeArguments;
        }

        public override int Arity
        {
            get
            {
                return this.constructedFrom.Arity;
            }
        }

        public override bool ReturnsVoid
        {
            get
            {
                return this.constructedFrom.ReturnsVoid;
            }
        }

        public override ITypeSymbol ReturnType
        {
            get
            {
                // TODO(cyrusn): Construct this.
                return this.constructedFrom.ReturnType;
            }
        }

        public override ImmutableArray<ITypeSymbol> TypeArguments
        {
            get
            {
                return this.typeArguments.AsImmutable();
            }
        }

        public override ImmutableArray<ITypeParameterSymbol> TypeParameters
        {
            get
            {
                return this.constructedFrom.TypeParameters;
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
                return this.constructedFrom;
            }
        }

        public override IMethodSymbol OverriddenMethod
        {
            get
            {
                // TODO(cyrusn): Construct this.
                return this.constructedFrom.OverriddenMethod;
            }
        }

        public override IMethodSymbol ReducedFrom
        {
            get
            {
                // TODO(cyrusn): Construct this.
                return this.constructedFrom.ReducedFrom;
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
                return this.constructedFrom.ExplicitInterfaceImplementations;
            }
        }

        public override IMethodSymbol PartialDefinitionPart
        {
            get
            {
                // TODO(cyrusn): Construct this.
                return this.constructedFrom.PartialDefinitionPart;
            }
        }

        public override IMethodSymbol PartialImplementationPart
        {
            get
            {
                // TODO(cyrusn): Construct this.
                return this.constructedFrom.PartialImplementationPart;
            }
        }

        protected override CodeGenerationSymbol Clone()
        {
            return new CodeGenerationConstructedMethodSymbol(this.constructedFrom, this.typeArguments);
        }
    }
}