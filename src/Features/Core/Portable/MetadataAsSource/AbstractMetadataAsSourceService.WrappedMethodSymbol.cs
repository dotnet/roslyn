// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.DocumentationComments;

namespace Microsoft.CodeAnalysis.MetadataAsSource
{
    internal partial class AbstractMetadataAsSourceService
    {
        private class WrappedMethodSymbol : AbstractWrappedSymbol, IMethodSymbol
        {
            private readonly IMethodSymbol _symbol;

            public WrappedMethodSymbol(IMethodSymbol methodSymbol, bool canImplementImplicitly, IDocumentationCommentFormattingService docCommentFormattingService)
                : base(methodSymbol, canImplementImplicitly, docCommentFormattingService)
            {
                _symbol = methodSymbol;
            }

            public int Arity => _symbol.Arity;

            public ISymbol AssociatedSymbol => _symbol.AssociatedSymbol;

            public INamedTypeSymbol AssociatedAnonymousDelegate => _symbol.AssociatedAnonymousDelegate;

            public IMethodSymbol ConstructedFrom => _symbol.ConstructedFrom;

            public bool IsReadOnly => _symbol.IsReadOnly;

            public ImmutableArray<IMethodSymbol> ExplicitInterfaceImplementations
            {
                get
                {
                    return CanImplementImplicitly
                        ? ImmutableArray.Create<IMethodSymbol>()
                        : _symbol.ExplicitInterfaceImplementations;
                }
            }

            public bool HidesBaseMethodsByName => _symbol.HidesBaseMethodsByName;

            public bool IsExtensionMethod => _symbol.IsExtensionMethod;

            public bool IsGenericMethod => _symbol.IsGenericMethod;

            public bool IsAsync => _symbol.IsAsync;

            public MethodKind MethodKind => _symbol.MethodKind;

            public new IMethodSymbol OriginalDefinition
            {
                get
                {
                    return this;
                }
            }

            public IMethodSymbol OverriddenMethod => _symbol.OverriddenMethod;

            public ImmutableArray<IParameterSymbol> Parameters => _symbol.Parameters;

            public IMethodSymbol PartialDefinitionPart => _symbol.PartialDefinitionPart;

            public IMethodSymbol PartialImplementationPart => _symbol.PartialImplementationPart;

            public ITypeSymbol ReceiverType => _symbol.ReceiverType;

            public NullableAnnotation ReceiverNullableAnnotation => _symbol.ReceiverNullableAnnotation;

            public IMethodSymbol ReducedFrom =>
                    // This implementation feels incorrect!
                    _symbol.ReducedFrom;

            public ITypeSymbol GetTypeInferredDuringReduction(ITypeParameterSymbol reducedFromTypeParameter)
            {
                // This implementation feels incorrect, but it follows the pattern that other extension method related APIs are using!
                return _symbol.GetTypeInferredDuringReduction(reducedFromTypeParameter);
            }

            public bool ReturnsVoid => _symbol.ReturnsVoid;

            public bool ReturnsByRef => _symbol.ReturnsByRef;

            public bool ReturnsByRefReadonly => _symbol.ReturnsByRefReadonly;

            public RefKind RefKind => _symbol.RefKind;

            public ITypeSymbol ReturnType => _symbol.ReturnType;

            public NullableAnnotation ReturnNullableAnnotation => _symbol.ReturnNullableAnnotation;

            public ImmutableArray<AttributeData> GetReturnTypeAttributes()
            {
                return _symbol.GetReturnTypeAttributes();
            }

            public ImmutableArray<CustomModifier> RefCustomModifiers => _symbol.RefCustomModifiers;

            public ImmutableArray<CustomModifier> ReturnTypeCustomModifiers => _symbol.ReturnTypeCustomModifiers;

            public ImmutableArray<ITypeSymbol> TypeArguments => _symbol.TypeArguments;

            public ImmutableArray<NullableAnnotation> TypeArgumentsNullableAnnotations => _symbol.TypeArgumentsNullableAnnotations;

            public ImmutableArray<ITypeParameterSymbol> TypeParameters => _symbol.TypeParameters;

            public IMethodSymbol Construct(params ITypeSymbol[] typeArguments)
            {
                return _symbol.Construct(typeArguments);
            }

            public DllImportData GetDllImportData()
            {
                return _symbol.GetDllImportData();
            }

            public IMethodSymbol ReduceExtensionMethod(ITypeSymbol receiverType)
            {
                // This implementation feels incorrect!
                return _symbol.ReduceExtensionMethod(receiverType);
            }

            public bool IsVararg => _symbol.IsVararg;

            public bool IsCheckedBuiltin => _symbol.IsCheckedBuiltin;
        }
    }
}
