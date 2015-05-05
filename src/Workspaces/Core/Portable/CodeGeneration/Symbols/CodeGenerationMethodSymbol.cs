// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal partial class CodeGenerationMethodSymbol : CodeGenerationAbstractMethodSymbol
    {
        private readonly ITypeSymbol _returnType;
        private readonly bool _returnsByRef;
        private readonly ImmutableArray<ITypeParameterSymbol> _typeParameters;
        private readonly ImmutableArray<IParameterSymbol> _parameters;
        private readonly ImmutableArray<IMethodSymbol> _explicitInterfaceImplementations;
        private readonly MethodKind _methodKind;

        public CodeGenerationMethodSymbol(
            INamedTypeSymbol containingType,
            IList<AttributeData> attributes,
            Accessibility declaredAccessibility,
            DeclarationModifiers modifiers,
            ITypeSymbol returnType,
            bool returnsByRef,
            IMethodSymbol explicitInterfaceSymbolOpt,
            string name,
            IList<ITypeParameterSymbol> typeParameters,
            IList<IParameterSymbol> parameters,
            IList<AttributeData> returnTypeAttributes,
            MethodKind methodKind = MethodKind.Ordinary)
            : base(containingType, attributes, declaredAccessibility, modifiers, name, returnTypeAttributes)
        {
            _returnType = returnType;
            _returnsByRef = returnsByRef;
            _typeParameters = typeParameters.AsImmutableOrEmpty();
            _parameters = parameters.AsImmutableOrEmpty();
            _explicitInterfaceImplementations = explicitInterfaceSymbolOpt == null
                ? ImmutableArray.Create<IMethodSymbol>()
                : ImmutableArray.Create(explicitInterfaceSymbolOpt);

            this.OriginalDefinition = this;
            _methodKind = methodKind;
        }

        public override ITypeSymbol ReturnType
        {
            get
            {
                return _returnType;
            }
        }

        public override ImmutableArray<ITypeParameterSymbol> TypeParameters
        {
            get
            {
                return _typeParameters;
            }
        }

        public override ImmutableArray<IParameterSymbol> Parameters
        {
            get
            {
                return _parameters;
            }
        }

        public override ImmutableArray<IMethodSymbol> ExplicitInterfaceImplementations
        {
            get
            {
                return _explicitInterfaceImplementations;
            }
        }

        protected override CodeGenerationSymbol Clone()
        {
            var result = new CodeGenerationMethodSymbol(this.ContainingType,
                this.GetAttributes(), this.DeclaredAccessibility, this.Modifiers,
                this.ReturnType, this.ReturnsByRef, this.ExplicitInterfaceImplementations.FirstOrDefault(),
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

        public override bool ReturnsByRef
        {
            get
            {
                return _returnsByRef;
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
                return _methodKind;
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
