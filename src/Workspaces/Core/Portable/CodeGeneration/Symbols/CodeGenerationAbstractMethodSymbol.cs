// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal abstract class CodeGenerationAbstractMethodSymbol : CodeGenerationSymbol, IMethodSymbol
    {
        public new IMethodSymbol OriginalDefinition { get; protected set; }

        private readonly ImmutableArray<AttributeData> _returnTypeAttributes;

        public virtual ImmutableArray<AttributeData> GetReturnTypeAttributes()
        {
            return _returnTypeAttributes;
        }

        protected CodeGenerationAbstractMethodSymbol(
            INamedTypeSymbol containingType,
            ImmutableArray<AttributeData> attributes,
            Accessibility declaredAccessibility,
            DeclarationModifiers modifiers,
            string name,
            ImmutableArray<AttributeData> returnTypeAttributes)
            : base(containingType, attributes, declaredAccessibility, modifiers, name)
        {
            _returnTypeAttributes = returnTypeAttributes.NullToEmpty();
        }

        public abstract int Arity { get; }
        public abstract bool ReturnsVoid { get; }
        public abstract bool ReturnsByRef { get; }
        public abstract bool ReturnsByRefReadonly { get; }
        public abstract RefKind RefKind { get; }
        public abstract ITypeSymbol ReturnType { get; }
        public abstract ImmutableArray<ITypeSymbol> TypeArguments { get; }
        public abstract ImmutableArray<ITypeParameterSymbol> TypeParameters { get; }
        public abstract ImmutableArray<IParameterSymbol> Parameters { get; }
        public abstract IMethodSymbol ConstructedFrom { get; }
        public abstract bool IsReadOnly { get; }
        public abstract IMethodSymbol OverriddenMethod { get; }
        public abstract IMethodSymbol ReducedFrom { get; }
        public abstract ITypeSymbol GetTypeInferredDuringReduction(ITypeParameterSymbol reducedFromTypeParameter);
        public abstract IMethodSymbol ReduceExtensionMethod(ITypeSymbol receiverType);
        public abstract ImmutableArray<IMethodSymbol> ExplicitInterfaceImplementations { get; }
        public abstract IMethodSymbol PartialDefinitionPart { get; }
        public abstract IMethodSymbol PartialImplementationPart { get; }

        public NullableAnnotation ReceiverNullableAnnotation => ReceiverType.GetNullability();
        public NullableAnnotation ReturnNullableAnnotation => ReturnType.GetNullability();
        public ImmutableArray<NullableAnnotation> TypeArgumentNullableAnnotations => TypeArguments.SelectAsArray(a => a.GetNullability());

        public virtual ITypeSymbol ReceiverType
        {
            get
            {
                return this.ContainingType;
            }
        }

        public override void Accept(SymbolVisitor visitor)
        {
            visitor.VisitMethod(this);
        }

        public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
            return visitor.VisitMethod(this);
        }

        public virtual MethodKind MethodKind => MethodKind.Ordinary;

        public override SymbolKind Kind => SymbolKind.Method;

        public virtual bool IsGenericMethod
        {
            get
            {
                return this.Arity > 0;
            }
        }

        public virtual bool IsExtensionMethod => false;

        public virtual bool IsAsync
        {
            get
            {
                return this.Modifiers.IsAsync;
            }
        }

        public virtual bool IsVararg => false;

        public bool IsCheckedBuiltin => false;

        public virtual bool HidesBaseMethodsByName => false;

        public ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get
            {
                return ImmutableArray.Create<CustomModifier>();
            }
        }

        public virtual ImmutableArray<CustomModifier> ReturnTypeCustomModifiers
        {
            get
            {
                return ImmutableArray.Create<CustomModifier>();
            }
        }

        public virtual ISymbol AssociatedSymbol => null;

        public INamedTypeSymbol AssociatedAnonymousDelegate => null;

        public IMethodSymbol Construct(params ITypeSymbol[] typeArguments)
        {
            return new CodeGenerationConstructedMethodSymbol(this, typeArguments.ToImmutableArray());
        }

        public IMethodSymbol Construct(ImmutableArray<ITypeSymbol> typeArguments, ImmutableArray<CodeAnalysis.NullableAnnotation> typeArgumentNullableAnnotations)
        {
            return new CodeGenerationConstructedMethodSymbol(this, typeArguments);
        }

        public DllImportData GetDllImportData()
        {
            return null;
        }
    }
}
