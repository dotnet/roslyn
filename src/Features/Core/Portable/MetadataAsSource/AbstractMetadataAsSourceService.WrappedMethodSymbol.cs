// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.DocumentationCommentFormatting;

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

            public int Arity
            {
                get
                {
                    return _symbol.Arity;
                }
            }

            public ISymbol AssociatedSymbol
            {
                get
                {
                    return _symbol.AssociatedSymbol;
                }
            }

            public INamedTypeSymbol AssociatedAnonymousDelegate
            {
                get
                {
                    return _symbol.AssociatedAnonymousDelegate;
                }
            }

            public IMethodSymbol ConstructedFrom
            {
                get
                {
                    return _symbol.ConstructedFrom;
                }
            }

            public ImmutableArray<IMethodSymbol> ExplicitInterfaceImplementations
            {
                get
                {
                    return this.CanImplementImplicitly
                        ? ImmutableArray.Create<IMethodSymbol>()
                        : _symbol.ExplicitInterfaceImplementations;
                }
            }

            public bool HidesBaseMethodsByName
            {
                get
                {
                    return _symbol.HidesBaseMethodsByName;
                }
            }

            public bool IsExtensionMethod
            {
                get
                {
                    return _symbol.IsExtensionMethod;
                }
            }

            public bool IsGenericMethod
            {
                get
                {
                    return _symbol.IsGenericMethod;
                }
            }

            public bool IsAsync
            {
                get
                {
                    return _symbol.IsAsync;
                }
            }

            public MethodKind MethodKind
            {
                get
                {
                    return _symbol.MethodKind;
                }
            }

            public new IMethodSymbol OriginalDefinition
            {
                get
                {
                    return this;
                }
            }

            public IMethodSymbol OverriddenMethod
            {
                get
                {
                    return _symbol.OverriddenMethod;
                }
            }

            public ImmutableArray<IParameterSymbol> Parameters
            {
                get
                {
                    return _symbol.Parameters;
                }
            }

            public IMethodSymbol PartialDefinitionPart
            {
                get
                {
                    return _symbol.PartialDefinitionPart;
                }
            }

            public IMethodSymbol PartialImplementationPart
            {
                get
                {
                    return _symbol.PartialImplementationPart;
                }
            }

            public ITypeSymbol ReceiverType
            {
                get
                {
                    return _symbol.ReceiverType;
                }
            }

            public IMethodSymbol ReducedFrom
            {
                get
                {
                    // This implementation feels incorrect!
                    return _symbol.ReducedFrom;
                }
            }

            public ITypeSymbol GetTypeInferredDuringReduction(ITypeParameterSymbol reducedFromTypeParameter)
            {
                // This implementation feels incorrect, but it follows the pattern that other extension method related APIs are using!
                return _symbol.GetTypeInferredDuringReduction(reducedFromTypeParameter);
            }

            public bool ReturnsVoid
            {
                get
                {
                    return _symbol.ReturnsVoid;
                }
            }

            public ITypeSymbol ReturnType
            {
                get
                {
                    return _symbol.ReturnType;
                }
            }

            public ImmutableArray<AttributeData> GetReturnTypeAttributes()
            {
                return _symbol.GetReturnTypeAttributes();
            }

            public ImmutableArray<CustomModifier> ReturnTypeCustomModifiers
            {
                get
                {
                    return _symbol.ReturnTypeCustomModifiers;
                }
            }

            public ImmutableArray<ITypeSymbol> TypeArguments
            {
                get
                {
                    return _symbol.TypeArguments;
                }
            }

            public ImmutableArray<ITypeParameterSymbol> TypeParameters
            {
                get
                {
                    return _symbol.TypeParameters;
                }
            }

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

            public bool IsVararg
            {
                get
                {
                    return _symbol.IsVararg;
                }
            }

            public bool IsCheckedBuiltin
            {
                get
                {
                    return _symbol.IsCheckedBuiltin;
                }
            }
        }
    }
}
