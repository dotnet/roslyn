// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.DocumentationComments;

namespace Microsoft.CodeAnalysis.MetadataAsSource
{
    internal partial class AbstractMetadataAsSourceService
    {
        private class WrappedMethodSymbol(IMethodSymbol methodSymbol, bool canImplementImplicitly, IDocumentationCommentFormattingService docCommentFormattingService) : AbstractWrappedSymbol(methodSymbol, canImplementImplicitly, docCommentFormattingService), IMethodSymbol
        {
            public int Arity => methodSymbol.Arity;

            public ISymbol AssociatedSymbol => methodSymbol.AssociatedSymbol;

            public INamedTypeSymbol AssociatedAnonymousDelegate => methodSymbol.AssociatedAnonymousDelegate;

            public IMethodSymbol ConstructedFrom => methodSymbol.ConstructedFrom;

            public bool IsReadOnly => methodSymbol.IsReadOnly;
            public bool IsInitOnly => methodSymbol.IsInitOnly;

            public System.Reflection.MethodImplAttributes MethodImplementationFlags => methodSymbol.MethodImplementationFlags;

            public ImmutableArray<IMethodSymbol> ExplicitInterfaceImplementations
            {
                get
                {
                    return CanImplementImplicitly
                        ? ImmutableArray.Create<IMethodSymbol>()
                        : methodSymbol.ExplicitInterfaceImplementations;
                }
            }

            public bool HidesBaseMethodsByName => methodSymbol.HidesBaseMethodsByName;

            public bool IsExtensionMethod => methodSymbol.IsExtensionMethod;

            public bool IsGenericMethod => methodSymbol.IsGenericMethod;

            public bool IsAsync => methodSymbol.IsAsync;

            public MethodKind MethodKind => methodSymbol.MethodKind;

            public new IMethodSymbol OriginalDefinition
            {
                get
                {
                    return this;
                }
            }

            public IMethodSymbol OverriddenMethod => methodSymbol.OverriddenMethod;

            public ImmutableArray<IParameterSymbol> Parameters => methodSymbol.Parameters;

            public IMethodSymbol PartialDefinitionPart => methodSymbol.PartialDefinitionPart;

            public IMethodSymbol PartialImplementationPart => methodSymbol.PartialImplementationPart;

            public bool IsPartialDefinition => methodSymbol.IsPartialDefinition;

            public ITypeSymbol ReceiverType => methodSymbol.ReceiverType;

            public NullableAnnotation ReceiverNullableAnnotation => methodSymbol.ReceiverNullableAnnotation;

            public IMethodSymbol ReducedFrom
                    // This implementation feels incorrect!
                    => methodSymbol.ReducedFrom;

            public ITypeSymbol GetTypeInferredDuringReduction(ITypeParameterSymbol reducedFromTypeParameter)
            {
                // This implementation feels incorrect, but it follows the pattern that other extension method related APIs are using!
                return methodSymbol.GetTypeInferredDuringReduction(reducedFromTypeParameter);
            }

            public bool ReturnsVoid => methodSymbol.ReturnsVoid;

            public bool ReturnsByRef => methodSymbol.ReturnsByRef;

            public bool ReturnsByRefReadonly => methodSymbol.ReturnsByRefReadonly;

            public RefKind RefKind => methodSymbol.RefKind;

            public ITypeSymbol ReturnType => methodSymbol.ReturnType;

            public NullableAnnotation ReturnNullableAnnotation => methodSymbol.ReturnNullableAnnotation;

            public ImmutableArray<AttributeData> GetReturnTypeAttributes()
                => methodSymbol.GetReturnTypeAttributes();

            public ImmutableArray<CustomModifier> RefCustomModifiers => methodSymbol.RefCustomModifiers;

            public ImmutableArray<CustomModifier> ReturnTypeCustomModifiers => methodSymbol.ReturnTypeCustomModifiers;

            public ImmutableArray<ITypeSymbol> TypeArguments => methodSymbol.TypeArguments;

            public ImmutableArray<NullableAnnotation> TypeArgumentNullableAnnotations => methodSymbol.TypeArgumentNullableAnnotations;

            public ImmutableArray<ITypeParameterSymbol> TypeParameters => methodSymbol.TypeParameters;

            public IMethodSymbol Construct(params ITypeSymbol[] typeArguments)
                => methodSymbol.Construct(typeArguments);

            public IMethodSymbol Construct(ImmutableArray<ITypeSymbol> typeArguments, ImmutableArray<NullableAnnotation> typeArgumentNullableAnnotations)
                => methodSymbol.Construct(typeArguments, typeArgumentNullableAnnotations);

            public DllImportData GetDllImportData()
                => methodSymbol.GetDllImportData();

            public IMethodSymbol ReduceExtensionMethod(ITypeSymbol receiverType)
            {
                // This implementation feels incorrect!
                return methodSymbol.ReduceExtensionMethod(receiverType);
            }

            public bool IsVararg => methodSymbol.IsVararg;

            public bool IsCheckedBuiltin => methodSymbol.IsCheckedBuiltin;

            public bool IsConditional => methodSymbol.IsConditional;

            public SignatureCallingConvention CallingConvention => methodSymbol.CallingConvention;

            public ImmutableArray<INamedTypeSymbol> UnmanagedCallingConventionTypes => methodSymbol.UnmanagedCallingConventionTypes;
        }
    }
}
