// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.DocumentationCommentFormatting;

namespace Microsoft.CodeAnalysis.MetadataAsSource
{
    internal partial class AbstractMetadataAsSourceService
    {
        private class WrappedMethodSymbol : AbstractWrappedSymbol, IMethodSymbol
        {
            private IMethodSymbol symbol;

            public WrappedMethodSymbol(IMethodSymbol methodSymbol, bool canImplementImplicitly, IDocumentationCommentFormattingService docCommentFormattingService)
                : base(methodSymbol, canImplementImplicitly, docCommentFormattingService)
            {
                this.symbol = methodSymbol;
            }

            public int Arity
            {
                get
                {
                    return this.symbol.Arity;
                }
            }

            public ISymbol AssociatedSymbol
            {
                get
                {
                    return this.symbol.AssociatedSymbol;
                }
            }

            public INamedTypeSymbol AssociatedAnonymousDelegate
            {
                get
                {
                    return this.symbol.AssociatedAnonymousDelegate;
                }
            }

            public IMethodSymbol ConstructedFrom
            {
                get
                {
                    return this.symbol.ConstructedFrom;
                }
            }

            public ImmutableArray<IMethodSymbol> ExplicitInterfaceImplementations
            {
                get
                {
                    return this.CanImplementImplicitly
                        ? ImmutableArray.Create<IMethodSymbol>()
                        : this.symbol.ExplicitInterfaceImplementations;
                }
            }

            public bool HidesBaseMethodsByName
            {
                get
                {
                    return this.symbol.HidesBaseMethodsByName;
                }
            }

            public bool IsExtensionMethod
            {
                get
                {
                    return this.symbol.IsExtensionMethod;
                }
            }

            public bool IsGenericMethod
            {
                get
                {
                    return this.symbol.IsGenericMethod;
                }
            }

            public bool IsAsync
            {
                get
                {
                    return this.symbol.IsAsync;
                }
            }

            public MethodKind MethodKind
            {
                get
                {
                    return this.symbol.MethodKind;
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
                    return this.symbol.OverriddenMethod;
                }
            }

            public ImmutableArray<IParameterSymbol> Parameters
            {
                get
                {
                    return this.symbol.Parameters;
                }
            }

            public IMethodSymbol PartialDefinitionPart
            {
                get
                {
                    return this.symbol.PartialDefinitionPart;
                }
            }

            public IMethodSymbol PartialImplementationPart
            {
                get
                {
                    return this.symbol.PartialImplementationPart;
                }
            }

            public ITypeSymbol ReceiverType
            {
                get
                {
                    return this.symbol.ReceiverType;
                }
            }

            public IMethodSymbol ReducedFrom
            {
                get
                {
                    // This implementation feels incorrect!
                    return this.symbol.ReducedFrom;
                }
            }

            public ITypeSymbol GetTypeInferredDuringReduction(ITypeParameterSymbol reducedFromTypeParameter)
            {
                // This implementation feels incorrect, but it follows the pattern that other extension method related APIs are using!
                return this.symbol.GetTypeInferredDuringReduction(reducedFromTypeParameter);
            }

            public bool ReturnsVoid
            {
                get
                {
                    return this.symbol.ReturnsVoid;
                }
            }

            public ITypeSymbol ReturnType
            {
                get
                {
                    return this.symbol.ReturnType;
                }
            }

            public ImmutableArray<AttributeData> GetReturnTypeAttributes()
            {
                return this.symbol.GetReturnTypeAttributes();
            }

            public ImmutableArray<CustomModifier> ReturnTypeCustomModifiers
            {
                get
                {
                    return this.symbol.ReturnTypeCustomModifiers;
                }
            }

            public ImmutableArray<ITypeSymbol> TypeArguments
            {
                get
                {
                    return this.symbol.TypeArguments;
                }
            }

            public ImmutableArray<ITypeParameterSymbol> TypeParameters
            {
                get
                {
                    return this.symbol.TypeParameters;
                }
            }

            public IMethodSymbol Construct(params ITypeSymbol[] typeArguments)
            {
                return this.symbol.Construct(typeArguments);
            }

            public DllImportData GetDllImportData()
            {
                return this.symbol.GetDllImportData();
            }

            public IMethodSymbol ReduceExtensionMethod(ITypeSymbol receiverType)
            {
                // This implementation feels incorrect!
                return this.symbol.ReduceExtensionMethod(receiverType);
            }

            public bool IsVararg
            {
                get
                {
                    return this.symbol.IsVararg;
                }
            }

            public bool IsCheckedBuiltin
            {
                get
                {
                    return this.symbol.IsCheckedBuiltin;
                }
            }
        }
    }
}
