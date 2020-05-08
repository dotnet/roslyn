// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.SourceGeneration
{
    internal static partial class CodeGenerator
    {
        public static IMethodSymbol Method(
            ITypeSymbol returnType,
            string name,
            ImmutableArray<AttributeData> attributes = default,
            Accessibility declaredAccessibility = default,
            SymbolModifiers modifiers = default,
            ImmutableArray<IMethodSymbol> explicitInterfaceImplementations = default,
            ImmutableArray<ITypeSymbol> typeArguments = default,
            ImmutableArray<IParameterSymbol> parameters = default,
            ISymbol containingSymbol = null)
        {
            return new MethodSymbol(
                MethodKind.Ordinary,
                attributes,
                declaredAccessibility,
                modifiers,
                returnType,
                explicitInterfaceImplementations,
                name,
                typeArguments,
                parameters,
                containingSymbol);
        }

        public static IMethodSymbol Constructor(
            ImmutableArray<AttributeData> attributes = default,
            Accessibility declaredAccessibility = default,
            SymbolModifiers modifiers = default,
            ImmutableArray<IParameterSymbol> parameters = default,
            ISymbol containingSymbol = null)
        {
            return new MethodSymbol(
                MethodKind.Constructor,
                attributes,
                declaredAccessibility,
                modifiers,
                returnType: null,
                explicitInterfaceImplementations: default,
                name: null,
                typeArguments: default,
                parameters,
                containingSymbol);
        }

        public static IMethodSymbol Destructor(
            ImmutableArray<AttributeData> attributes = default,
            SymbolModifiers modifiers = default,
            ISymbol containingSymbol = null)
        {
            return new MethodSymbol(
                MethodKind.Destructor,
                attributes,
                declaredAccessibility: default,
                modifiers,
                returnType: null,
                explicitInterfaceImplementations: default,
                name: null,
                typeArguments: default,
                parameters: default,
                containingSymbol);
        }

        public static IMethodSymbol ImplicitConversion(
            ITypeSymbol returnType,
            IParameterSymbol parameter,
            ImmutableArray<AttributeData> attributes = default,
            Accessibility declaredAccessibility = default,
            SymbolModifiers modifiers = default,
            ISymbol containingSymbol = null)
        {
            return new MethodSymbol(
                MethodKind.Conversion,
                attributes,
                declaredAccessibility,
                modifiers,
                returnType,
                explicitInterfaceImplementations: default,
                WellKnownMemberNames.ImplicitConversionName,
                typeArguments: default,
                ImmutableArray.Create(parameter),
                containingSymbol);
        }

        public static IMethodSymbol ExplicitConversion(
            ITypeSymbol returnType,
            IParameterSymbol parameter,
            ImmutableArray<AttributeData> attributes = default,
            Accessibility declaredAccessibility = default,
            SymbolModifiers modifiers = default,
            ISymbol containingSymbol = null)
        {
            return new MethodSymbol(
                MethodKind.Conversion,
                attributes,
                declaredAccessibility,
                modifiers,
                returnType,
                explicitInterfaceImplementations: default,
                WellKnownMemberNames.ExplicitConversionName,
                typeArguments: default,
                ImmutableArray.Create(parameter),
                containingSymbol);
        }

        public static IMethodSymbol Operator(
            ITypeSymbol returnType,
            string name,
            ImmutableArray<IParameterSymbol> parameters,
            ImmutableArray<AttributeData> attributes = default,
            Accessibility declaredAccessibility = default,
            SymbolModifiers modifiers = default,
            ISymbol containingSymbol = null)
        {
            return new MethodSymbol(
                MethodKind.UserDefinedOperator,
                attributes,
                declaredAccessibility,
                modifiers,
                returnType,
                explicitInterfaceImplementations: default,
                name,
                typeArguments: default,
                parameters,
                containingSymbol);
        }

        public static IMethodSymbol With(
            this IMethodSymbol method,
            Optional<MethodKind> methodKind = default,
            Optional<ImmutableArray<AttributeData>> attributes = default,
            Optional<Accessibility> declaredAccessibility = default,
            Optional<SymbolModifiers> modifiers = default,
            Optional<ITypeSymbol> returnType = default,
            Optional<ImmutableArray<IMethodSymbol>> explicitInterfaceImplementations = default,
            Optional<string> name = default,
            Optional<ImmutableArray<ITypeSymbol>> typeArguments = default,
            Optional<ImmutableArray<IParameterSymbol>> parameters = default,
            Optional<ISymbol> containingSymbol = default)
        {
            return new MethodSymbol(
                methodKind.GetValueOr(method.MethodKind),
                attributes.GetValueOr(method.GetAttributes()),
                declaredAccessibility.GetValueOr(method.DeclaredAccessibility),
                modifiers.GetValueOr(method.GetModifiers()),
                returnType.GetValueOr(method.ReturnType),
                explicitInterfaceImplementations.GetValueOr(method.ExplicitInterfaceImplementations),
                name.GetValueOr(method.Name),
                typeArguments.GetValueOr(method.TypeArguments),
                parameters.GetValueOr(method.Parameters),
                containingSymbol.GetValueOr(method.ContainingSymbol));
        }

        private class MethodSymbol : Symbol, IMethodSymbol
        {
            private readonly ImmutableArray<AttributeData> _attributes;

            public MethodSymbol(
                MethodKind methodKind,
                ImmutableArray<AttributeData> attributes,
                Accessibility declaredAccessibility,
                SymbolModifiers modifiers,
                ITypeSymbol returnType,
                ImmutableArray<IMethodSymbol> explicitInterfaceImplementations,
                string name,
                ImmutableArray<ITypeSymbol> typeArguments,
                ImmutableArray<IParameterSymbol> parameters,
                ISymbol containingSymbol)
            {
                MethodKind = methodKind;
                _attributes = attributes;
                DeclaredAccessibility = declaredAccessibility;
                Modifiers = modifiers;
                ReturnType = returnType;
                ExplicitInterfaceImplementations = explicitInterfaceImplementations.NullToEmpty();
                Name = name;
                TypeArguments = typeArguments.NullToEmpty();
                Parameters = parameters.NullToEmpty();
                ContainingSymbol = containingSymbol;
            }

            public override ISymbol ContainingSymbol { get; }
            public override Accessibility DeclaredAccessibility { get; }
            public override ImmutableArray<AttributeData> GetAttributes() => _attributes;
            public override SymbolKind Kind => SymbolKind.Method;
            public override SymbolModifiers Modifiers { get; }
            public override string Name { get; }

            public ImmutableArray<IMethodSymbol> ExplicitInterfaceImplementations { get; }
            public ImmutableArray<IParameterSymbol> Parameters { get; }
            public ImmutableArray<ITypeSymbol> TypeArguments { get; }
            public ITypeSymbol ReturnType { get; }
            public MethodKind MethodKind { get; }

            public override void Accept(SymbolVisitor visitor)
                => visitor.VisitMethod(this);

            public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
                => visitor.VisitMethod(this);

            #region default implementation

            IMethodSymbol IMethodSymbol.OriginalDefinition => throw new NotImplementedException();
            public bool HidesBaseMethodsByName => throw new NotImplementedException();
            public bool IsAsync => throw new NotImplementedException();
            public bool IsCheckedBuiltin => throw new NotImplementedException();
            public bool IsConditional => throw new NotImplementedException();
            public bool IsExtensionMethod => throw new NotImplementedException();
            public bool IsGenericMethod => throw new NotImplementedException();
            public bool IsReadOnly => throw new NotImplementedException();
            public bool IsVararg => throw new NotImplementedException();
            public bool ReturnsByRef => throw new NotImplementedException();
            public bool ReturnsByRefReadonly => throw new NotImplementedException();
            public bool ReturnsVoid => throw new NotImplementedException();
            public DllImportData GetDllImportData() => throw new NotImplementedException();
            public IMethodSymbol Construct(ImmutableArray<ITypeSymbol> typeArguments, ImmutableArray<NullableAnnotation> typeArgumentNullableAnnotations) => throw new NotImplementedException();
            public IMethodSymbol Construct(params ITypeSymbol[] typeArguments) => throw new NotImplementedException();
            public IMethodSymbol ConstructedFrom => throw new NotImplementedException();
            public IMethodSymbol OverriddenMethod => throw new NotImplementedException();
            public IMethodSymbol PartialDefinitionPart => throw new NotImplementedException();
            public IMethodSymbol PartialImplementationPart => throw new NotImplementedException();
            public IMethodSymbol ReducedFrom => throw new NotImplementedException();
            public IMethodSymbol ReduceExtensionMethod(ITypeSymbol receiverType) => throw new NotImplementedException();
            public ImmutableArray<AttributeData> GetReturnTypeAttributes() => throw new NotImplementedException();
            public ImmutableArray<CustomModifier> RefCustomModifiers => throw new NotImplementedException();
            public ImmutableArray<CustomModifier> ReturnTypeCustomModifiers => throw new NotImplementedException();
            public ImmutableArray<ITypeParameterSymbol> TypeParameters => throw new NotImplementedException();
            public ImmutableArray<NullableAnnotation> TypeArgumentNullableAnnotations => throw new NotImplementedException();
            public INamedTypeSymbol AssociatedAnonymousDelegate => throw new NotImplementedException();
            public int Arity => throw new NotImplementedException();
            public ISymbol AssociatedSymbol => throw new NotImplementedException();
            public ITypeSymbol GetTypeInferredDuringReduction(ITypeParameterSymbol reducedFromTypeParameter) => throw new NotImplementedException();
            public ITypeSymbol ReceiverType => throw new NotImplementedException();
            public NullableAnnotation ReceiverNullableAnnotation => throw new NotImplementedException();
            public NullableAnnotation ReturnNullableAnnotation => throw new NotImplementedException();
            public RefKind RefKind => throw new NotImplementedException();

            #endregion
        }
    }
}
