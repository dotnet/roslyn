// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;

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
            IList<AttributeData> attributes,
            Accessibility declaredAccessibility,
            DeclarationModifiers modifiers,
            string name,
            IList<AttributeData> returnTypeAttributes)
            : base(containingType, attributes, declaredAccessibility, modifiers, name)
        {
            _returnTypeAttributes = returnTypeAttributes.AsImmutableOrEmpty();
        }

        public abstract int Arity { get; }
        public abstract bool ReturnsVoid { get; }
        public abstract bool ReturnsByRef { get; }
        public abstract ITypeSymbol ReturnType { get; }
        public abstract ImmutableArray<ITypeSymbol> TypeArguments { get; }
        public abstract ImmutableArray<ITypeParameterSymbol> TypeParameters { get; }
        public abstract ImmutableArray<IParameterSymbol> Parameters { get; }
        public abstract IMethodSymbol ConstructedFrom { get; }
        public abstract IMethodSymbol OverriddenMethod { get; }
        public abstract IMethodSymbol ReducedFrom { get; }
        public abstract ITypeSymbol GetTypeInferredDuringReduction(ITypeParameterSymbol reducedFromTypeParameter);
        public abstract IMethodSymbol ReduceExtensionMethod(ITypeSymbol receiverType);
        public abstract ImmutableArray<IMethodSymbol> ExplicitInterfaceImplementations { get; }
        public abstract IMethodSymbol PartialDefinitionPart { get; }
        public abstract IMethodSymbol PartialImplementationPart { get; }

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

        public virtual MethodKind MethodKind
        {
            get
            {
                return MethodKind.Ordinary;
            }
        }

        public override SymbolKind Kind
        {
            get
            {
                return SymbolKind.Method;
            }
        }

        public virtual bool IsGenericMethod
        {
            get
            {
                return this.Arity > 0;
            }
        }

        public virtual bool IsExtensionMethod
        {
            get
            {
                return false;
            }
        }

        public virtual bool IsAsync
        {
            get
            {
                return this.Modifiers.IsAsync;
            }
        }

        public virtual bool IsVararg
        {
            get
            {
                return false;
            }
        }

        public bool IsCheckedBuiltin
        {
            get
            {
                return false;
            }
        }

        public virtual bool HidesBaseMethodsByName
        {
            get
            {
                return false;
            }
        }

        public virtual ImmutableArray<CustomModifier> ReturnTypeCustomModifiers
        {
            get
            {
                return ImmutableArray.Create<CustomModifier>();
            }
        }

        public virtual ISymbol AssociatedSymbol
        {
            get
            {
                return null;
            }
        }

        public INamedTypeSymbol AssociatedAnonymousDelegate
        {
            get
            {
                return null;
            }
        }

        public IMethodSymbol Construct(params ITypeSymbol[] typeArguments)
        {
            return new CodeGenerationConstructedMethodSymbol(this, typeArguments);
        }

        public DllImportData GetDllImportData()
        {
            return null;
        }
    }
}
