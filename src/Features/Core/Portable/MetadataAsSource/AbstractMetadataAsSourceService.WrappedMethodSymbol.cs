// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.DocumentationComments;

namespace Microsoft.CodeAnalysis.MetadataAsSource;

internal abstract partial class AbstractMetadataAsSourceService
{
    private sealed class WrappedMethodSymbol(IMethodSymbol methodSymbol, bool canImplementImplicitly, IDocumentationCommentFormattingService docCommentFormattingService) : AbstractWrappedSymbol(methodSymbol, canImplementImplicitly, docCommentFormattingService), IMethodSymbol
    {
        private readonly IMethodSymbol _symbol = methodSymbol;

        public int Arity => _symbol.Arity;

        public ISymbol AssociatedSymbol => _symbol.AssociatedSymbol;

        public INamedTypeSymbol AssociatedAnonymousDelegate => _symbol.AssociatedAnonymousDelegate;

        public IMethodSymbol ConstructedFrom => _symbol.ConstructedFrom;

        public bool IsReadOnly => _symbol.IsReadOnly;
        public bool IsInitOnly => _symbol.IsInitOnly;

        public System.Reflection.MethodImplAttributes MethodImplementationFlags => _symbol.MethodImplementationFlags;

        public ImmutableArray<IMethodSymbol> ExplicitInterfaceImplementations
        {
            get
            {
                return CanImplementImplicitly
                    ? []
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

        public bool IsPartialDefinition => _symbol.IsPartialDefinition;

        public ITypeSymbol ReceiverType => _symbol.ReceiverType;

        public NullableAnnotation ReceiverNullableAnnotation => _symbol.ReceiverNullableAnnotation;

        public IMethodSymbol ReducedFrom
                // This implementation feels incorrect!
                => _symbol.ReducedFrom;

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
            => _symbol.GetReturnTypeAttributes();

        public ImmutableArray<CustomModifier> RefCustomModifiers => _symbol.RefCustomModifiers;

        public ImmutableArray<CustomModifier> ReturnTypeCustomModifiers => _symbol.ReturnTypeCustomModifiers;

        public ImmutableArray<ITypeSymbol> TypeArguments => _symbol.TypeArguments;

        public ImmutableArray<NullableAnnotation> TypeArgumentNullableAnnotations => _symbol.TypeArgumentNullableAnnotations;

        public ImmutableArray<ITypeParameterSymbol> TypeParameters => _symbol.TypeParameters;

        public IMethodSymbol Construct(params ITypeSymbol[] typeArguments)
            => _symbol.Construct(typeArguments);

        public IMethodSymbol Construct(ImmutableArray<ITypeSymbol> typeArguments, ImmutableArray<NullableAnnotation> typeArgumentNullableAnnotations)
            => _symbol.Construct(typeArguments, typeArgumentNullableAnnotations);

        public DllImportData GetDllImportData()
            => _symbol.GetDllImportData();

        public IMethodSymbol ReduceExtensionMethod(ITypeSymbol receiverType)
            => throw new System.NotImplementedException();

        public IMethodSymbol ReduceExtensionMember(ITypeSymbol receiverType)
            => throw new System.NotImplementedException();

        public IMethodSymbol AssociatedExtensionImplementation => null;

        public bool IsVararg => _symbol.IsVararg;

        public bool IsCheckedBuiltin => _symbol.IsCheckedBuiltin;

        public bool IsConditional => _symbol.IsConditional;

        public bool IsIterator => _symbol.IsIterator;

        public SignatureCallingConvention CallingConvention => _symbol.CallingConvention;

        public ImmutableArray<INamedTypeSymbol> UnmanagedCallingConventionTypes => _symbol.UnmanagedCallingConventionTypes;
    }
}
