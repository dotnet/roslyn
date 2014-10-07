// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal partial class CodeGenerationMethodSymbol : CodeGenerationAbstractMethodSymbol
    {
        private readonly ITypeSymbol returnType;
        private readonly ImmutableArray<ITypeParameterSymbol> typeParameters;
        private readonly ImmutableArray<IParameterSymbol> parameters;
        private readonly ImmutableArray<IMethodSymbol> explicitInterfaceImplementations;
        private readonly MethodKind methodKind;

        public CodeGenerationMethodSymbol(
            INamedTypeSymbol containingType,
            IList<AttributeData> attributes,
            Accessibility declaredAccessibility,
            DeclarationModifiers modifiers,
            ITypeSymbol returnType,
            IMethodSymbol explicitInterfaceSymbolOpt,
            string name,
            IList<ITypeParameterSymbol> typeParameters,
            IList<IParameterSymbol> parameters,
            IList<AttributeData> returnTypeAttributes,
            MethodKind methodKind = MethodKind.Ordinary)
            : base(containingType, attributes, declaredAccessibility, modifiers, name, returnTypeAttributes)
        {
            this.returnType = returnType;
            this.typeParameters = typeParameters.AsImmutableOrEmpty();
            this.parameters = parameters.AsImmutableOrEmpty();
            this.explicitInterfaceImplementations = explicitInterfaceSymbolOpt == null
                ? ImmutableArray.Create<IMethodSymbol>()
                : ImmutableArray.Create(explicitInterfaceSymbolOpt);

            this.OriginalDefinition = this;
            this.methodKind = methodKind;
        }

        public override ITypeSymbol ReturnType
        {
            get
            {
                return this.returnType;
            }
        }

        public override ImmutableArray<ITypeParameterSymbol> TypeParameters
        {
            get
            {
                return this.typeParameters;
            }
        }

        public override ImmutableArray<IParameterSymbol> Parameters
        {
            get
            {
                return this.parameters;
            }
        }

        public override ImmutableArray<IMethodSymbol> ExplicitInterfaceImplementations
        {
            get
            {
                return this.explicitInterfaceImplementations;
            }
        }

        protected override CodeGenerationSymbol Clone()
        {
            var result = new CodeGenerationMethodSymbol(this.ContainingType,
                this.GetAttributes(), this.DeclaredAccessibility, this.Modifiers,
                this.ReturnType, this.ExplicitInterfaceImplementations.FirstOrDefault(),
                this.Name, this.TypeParameters, this.Parameters, this.GetReturnTypeAttributes());

            CodeGenerationMethodInfo.Attach(result,
                CodeGenerationMethodInfo.GetIsNew(this),
                CodeGenerationMethodInfo.GetIsUnsafe(this),
                CodeGenerationMethodInfo.GetIsPartial(this),
                CodeGenerationMethodInfo.GetIsAsync(this),
                CodeGenerationMethodInfo.GetStatements(this),
                CodeGenerationMethodInfo.GetHandlesExpressions(this));

            return result;
        }

        public override int Arity
        {
            get
            {
                return this.TypeParameters.Length;
            }
        }

        public override bool ReturnsVoid
        {
            get
            {
                return this.ReturnType == null || this.ReturnType.SpecialType == SpecialType.System_Void;
            }
        }

        public override ImmutableArray<ITypeSymbol> TypeArguments
        {
            get
            {
                return this.TypeParameters.As<ITypeSymbol>();
            }
        }

        public override IMethodSymbol ConstructedFrom
        {
            get
            {
                return this;
            }
        }

        public override IMethodSymbol OverriddenMethod
        {
            get
            {
                return null;
            }
        }

        public override IMethodSymbol ReducedFrom
        {
            get
            {
                return null;
            }
        }

        public override MethodKind MethodKind
        {
            get
            {
                return this.methodKind;
            }
        }

        public override ITypeSymbol GetTypeInferredDuringReduction(ITypeParameterSymbol reducedFromTypeParameter)
        {
            throw new InvalidOperationException();
        }

        public override IMethodSymbol ReduceExtensionMethod(ITypeSymbol receiverType)
        {
            return null;
        }

        public override IMethodSymbol PartialImplementationPart
        {
            get
            {
                return null;
            }
        }

        public override IMethodSymbol PartialDefinitionPart
        {
            get
            {
                return null;
            }
        }
    }
}