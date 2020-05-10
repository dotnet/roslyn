// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Operations;

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
            IBlockOperation body = null,
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
                body,
                containingSymbol);
        }

        public static IMethodSymbol Constructor(
            ImmutableArray<AttributeData> attributes = default,
            Accessibility declaredAccessibility = default,
            SymbolModifiers modifiers = default,
            ImmutableArray<IParameterSymbol> parameters = default,
            IBlockOperation body = null,
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
                body,
                containingSymbol);
        }

        public static IMethodSymbol Destructor(
            ImmutableArray<AttributeData> attributes = default,
            SymbolModifiers modifiers = default,
            IBlockOperation body = null,
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
                body,
                containingSymbol);
        }

        public static IMethodSymbol ImplicitConversion(
            ITypeSymbol returnType,
            IParameterSymbol parameter,
            ImmutableArray<AttributeData> attributes = default,
            Accessibility declaredAccessibility = default,
            SymbolModifiers modifiers = default,
            IBlockOperation body = null,
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
                body,
                containingSymbol);
        }

        public static IMethodSymbol ExplicitConversion(
            ITypeSymbol returnType,
            IParameterSymbol parameter,
            ImmutableArray<AttributeData> attributes = default,
            Accessibility declaredAccessibility = default,
            SymbolModifiers modifiers = default,
            IBlockOperation body = null,
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
                body,
                containingSymbol);
        }

        public static IMethodSymbol Operator(
            ITypeSymbol returnType,
            string name,
            ImmutableArray<AttributeData> attributes = default,
            Accessibility declaredAccessibility = default,
            SymbolModifiers modifiers = default,
            ImmutableArray<IParameterSymbol> parameters = default,
            IBlockOperation body = null,
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
                body,
                containingSymbol);
        }

        public static IMethodSymbol PropertyGet(
            ImmutableArray<AttributeData> attributes = default,
            Accessibility declaredAccessibility = default,
            SymbolModifiers modifiers = default,
            IBlockOperation body = null)
        {
            return new MethodSymbol(
                MethodKind.PropertyGet,
                attributes,
                declaredAccessibility,
                modifiers,
                returnType: null,
                explicitInterfaceImplementations: default,
                name: null,
                typeArguments: default,
                parameters: default,
                body,
                containingSymbol: null);
        }

        public static IMethodSymbol PropertySet(
            ImmutableArray<AttributeData> attributes = default,
            Accessibility declaredAccessibility = default,
            SymbolModifiers modifiers = default,
            ImmutableArray<IParameterSymbol> parameters = default,
            IBlockOperation body = null)
        {
            return new MethodSymbol(
                MethodKind.PropertySet,
                attributes,
                declaredAccessibility,
                modifiers,
                returnType: null,
                explicitInterfaceImplementations: default,
                name: null,
                typeArguments: default,
                parameters,
                body,
                containingSymbol: null);
        }

        public static IMethodSymbol EventAdd(
            ImmutableArray<AttributeData> attributes = default,
            Accessibility declaredAccessibility = default,
            SymbolModifiers modifiers = default,
            IBlockOperation body = null)
        {
            return new MethodSymbol(
                MethodKind.EventAdd,
                attributes,
                declaredAccessibility,
                modifiers,
                returnType: null,
                explicitInterfaceImplementations: default,
                name: null,
                typeArguments: default,
                parameters: default,
                body,
                containingSymbol: null);
        }

        public static IMethodSymbol EventRemove(
            ImmutableArray<AttributeData> attributes = default,
            Accessibility declaredAccessibility = default,
            SymbolModifiers modifiers = default,
            IBlockOperation body = null)
        {
            return new MethodSymbol(
                MethodKind.EventRemove,
                attributes,
                declaredAccessibility,
                modifiers,
                returnType: null,
                explicitInterfaceImplementations: default,
                name: null,
                typeArguments: default,
                parameters: default,
                body,
                containingSymbol: null);
        }

        public static IMethodSymbol EventRaise(
            ImmutableArray<AttributeData> attributes = default,
            Accessibility declaredAccessibility = default,
            SymbolModifiers modifiers = default,
            IBlockOperation body = null)
        {
            return new MethodSymbol(
                MethodKind.EventRaise,
                attributes,
                declaredAccessibility,
                modifiers,
                returnType: null,
                explicitInterfaceImplementations: default,
                name: null,
                typeArguments: default,
                parameters: default,
                body,
                containingSymbol: null);
        }

        public static IMethodSymbol DelegateInvoke(
            ITypeSymbol returnType,
            ImmutableArray<IParameterSymbol> parameters)
        {
            return new MethodSymbol(
                MethodKind.DelegateInvoke,
                attributes: default,
                declaredAccessibility: default,
                modifiers: default,
                returnType,
                explicitInterfaceImplementations: default,
                name: null,
                typeArguments: default,
                parameters,
                body: null,
                containingSymbol: null);
        }

        public static IMethodSymbol WithMethodKind(this IMethodSymbol symbol, MethodKind methodKind)
            => With(symbol, methodKind: ToOptional(methodKind));

        public static IMethodSymbol WithAttributes(this IMethodSymbol symbol, params AttributeData[] attributes)
            => WithAttributes(assymbolsembly, (IEnumerable<AttributeData>)attributes);

        public static IMethodSymbol WithAttributes(this IMethodSymbol symbol, IEnumerable<AttributeData> attributes)
            => WithAttributes(symbol, attributes.ToImmutableArray());

        public static IMethodSymbol WithAttributes(this IMethodSymbol symbol, ImmutableArray<AttributeData> attributes)
            => With(symbol, attributes: ToOptional(attributes));

        public static IMethodSymbol WithDeclaredAccessibility(this IMethodSymbol symbol, Accessibility declaredAccessibility)
            => With(symbol, declaredAccessibility: ToOptional(declaredAccessibility));

        public static IMethodSymbol WithModifiers(this IMethodSymbol symbol, SymbolModifiers modifiers)
            => With(symbol, modifiers: ToOptional(modifiers));

        public static IMethodSymbol WithReturnType(this IMethodSymbol symbol, ITypeSymbol returnType)
            => With(symbol, returnType: ToOptional(returnType));

        public static IMethodSymbol WithExplicitInterfaceImplementations(this IMethodSymbol symbol, ImmutableArray<IMethodSymbol> explicitInterfaceImplementations)
            => With(symbol, explicitInterfaceImplementations: ToOptional(explicitInterfaceImplementations));

        public static IMethodSymbol WithName(this IMethodSymbol symbol, string name)
            => With(symbol, name: ToOptional(name));

        public static IMethodSymbol WithTypeArguments(this IMethodSymbol symbol, params ITypeSymbol[] typeArguments)
            => WithTypeArguments(symbol, (IEnumerable<ITypeSymbol>)typeArguments);

        public static IMethodSymbol WithTypeArguments(this IMethodSymbol symbol, IEnumerable<ITypeSymbol> typeArguments)
            => WithTypeArguments(symbol, typeArguments.ToImmutableArray());

        public static IMethodSymbol WithTypeArguments(this IMethodSymbol symbol, ImmutableArray<ITypeSymbol> typeArguments)
            => With(symbol, typeArguments: ToOptional(typeArguments));

        public static IMethodSymbol WithParameters(this IMethodSymbol symbol, params IParameterSymbol[] parameters)
            => WithParameters(symbol, (IEnumerable<IParameterSymbol>)parameters);

        public static IMethodSymbol WithParameters(this IMethodSymbol symbol, IEnumerable<IParameterSymbol> parameters)
            => WithParameters(symbol, parameters.ToImmutableArray());

        public static IMethodSymbol WithParameters(this IMethodSymbol symbol, ImmutableArray<IParameterSymbol> parameters)
            => With(symbol, parameters: ToOptional(parameters));

        public static IMethodSymbol WithBody(this IMethodSymbol symbol, IBlockOperation body)
            => With(symbol, body: ToOptional(body));

        public static IMethodSymbol WithContainingSymbol(this IMethodSymbol symbol, ISymbol containingSymbol)
            => With(symbol, containingSymbol: ToOptional(containingSymbol));

        private static IMethodSymbol With(
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
            Optional<IBlockOperation> body = default,
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
                body.GetValueOr(method.GetBody()),
                containingSymbol.GetValueOr(method.ContainingSymbol));
        }

        internal static IBlockOperation GetBody(this IMethodSymbol method)
            => method is MethodSymbol m ? m.Body : null;

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
                IBlockOperation body,
                ISymbol containingSymbol)
            {
                MethodKind = methodKind;
                _attributes = attributes.NullToEmpty();
                DeclaredAccessibility = declaredAccessibility;
                Modifiers = modifiers;
                ReturnType = returnType;
                ExplicitInterfaceImplementations = explicitInterfaceImplementations.NullToEmpty();
                Name = name;
                TypeArguments = typeArguments.NullToEmpty();
                Parameters = parameters.NullToEmpty();
                Body = body;
                ContainingSymbol = containingSymbol;
            }

            public override ISymbol ContainingSymbol { get; }
            public override Accessibility DeclaredAccessibility { get; }
            public override ImmutableArray<AttributeData> GetAttributes() => _attributes;
            public override SymbolKind Kind => SymbolKind.Method;
            public override SymbolModifiers Modifiers { get; }
            public override string Name { get; }

            public IBlockOperation Body { get; }
            public ISymbol AssociatedSymbol => null;
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
            public ITypeSymbol GetTypeInferredDuringReduction(ITypeParameterSymbol reducedFromTypeParameter) => throw new NotImplementedException();
            public ITypeSymbol ReceiverType => throw new NotImplementedException();
            public NullableAnnotation ReceiverNullableAnnotation => throw new NotImplementedException();
            public NullableAnnotation ReturnNullableAnnotation => throw new NotImplementedException();
            public RefKind RefKind => throw new NotImplementedException();

            #endregion
        }
    }
}
