// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.DocumentationComments;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MetadataAsSource;

internal partial class AbstractMetadataAsSourceService
{
    private class WrappedNamedTypeSymbol : AbstractWrappedNamespaceOrTypeSymbol, INamedTypeSymbol
    {
        private readonly INamedTypeSymbol _symbol;
        private readonly ImmutableArray<ISymbol> _members;

        public WrappedNamedTypeSymbol(INamedTypeSymbol symbol, bool canImplementImplicitly, IDocumentationCommentFormattingService docCommentFormattingService)
            : base(symbol, canImplementImplicitly, docCommentFormattingService)
        {
            _symbol = symbol;

            var allMembers = _symbol.GetMembers();
            var filteredMembers = from m in allMembers
                                  where !m.HasUnsupportedMetadata
                                  where m.DeclaredAccessibility is Accessibility.Public or
                                        Accessibility.Protected or
                                        Accessibility.ProtectedOrInternal
                                  where m.Kind is SymbolKind.Event or
                                        SymbolKind.Field or
                                        SymbolKind.Method or
                                        SymbolKind.NamedType or
                                        SymbolKind.Property
                                  select WrapMember(m, canImplementImplicitly, docCommentFormattingService);

            _members = ImmutableArray.CreateRange(filteredMembers);
        }

        private static ISymbol WrapMember(ISymbol m, bool canImplementImplicitly, IDocumentationCommentFormattingService docCommentFormattingService)
            => m.Kind switch
            {
                SymbolKind.Field => new WrappedFieldSymbol((IFieldSymbol)m, docCommentFormattingService),
                SymbolKind.Event => new WrappedEventSymbol((IEventSymbol)m, canImplementImplicitly, docCommentFormattingService),
                SymbolKind.Method => new WrappedMethodSymbol((IMethodSymbol)m, canImplementImplicitly, docCommentFormattingService),
                SymbolKind.NamedType => new WrappedNamedTypeSymbol((INamedTypeSymbol)m, canImplementImplicitly, docCommentFormattingService),
                SymbolKind.Property => new WrappedPropertySymbol((IPropertySymbol)m, canImplementImplicitly, docCommentFormattingService),
                _ => throw ExceptionUtilities.UnexpectedValue(m.Kind),
            };

        public bool IsAnonymousType => _symbol.IsAnonymousType;
        public bool IsComImport => _symbol.IsComImport;
        public bool IsGenericType => _symbol.IsGenericType;
        public bool IsImplicitClass => _symbol.IsImplicitClass;
        public bool IsReferenceType => _symbol.IsReferenceType;
        public bool IsScriptClass => _symbol.IsScriptClass;
        public bool IsTupleType => _symbol.IsTupleType;
        public bool IsUnboundGenericType => _symbol.IsUnboundGenericType;
        public bool IsValueType => _symbol.IsValueType;
        public bool MightContainExtensionMethods => _symbol.MightContainExtensionMethods;

        public int Arity => _symbol.Arity;

        public TypeKind TypeKind => _symbol.TypeKind;
        public SpecialType SpecialType => _symbol.SpecialType;
        public ISymbol AssociatedSymbol => _symbol.AssociatedSymbol;
        public IMethodSymbol DelegateInvokeMethod => _symbol.DelegateInvokeMethod;

        public INamedTypeSymbol EnumUnderlyingType => _symbol.EnumUnderlyingType;
        public INamedTypeSymbol ConstructedFrom => _symbol.ConstructedFrom;
        public INamedTypeSymbol BaseType => _symbol.BaseType;
        public INamedTypeSymbol TupleUnderlyingType => _symbol.TupleUnderlyingType;

        public ImmutableArray<ITypeParameterSymbol> TypeParameters => _symbol.TypeParameters;
        public ImmutableArray<ITypeSymbol> TypeArguments => _symbol.TypeArguments;
        public ImmutableArray<NullableAnnotation> TypeArgumentNullableAnnotations => _symbol.TypeArgumentNullableAnnotations;
        public ImmutableArray<IMethodSymbol> InstanceConstructors => _symbol.InstanceConstructors;
        public ImmutableArray<IMethodSymbol> StaticConstructors => _symbol.StaticConstructors;
        public ImmutableArray<IMethodSymbol> Constructors => _symbol.Constructors;
        public ImmutableArray<INamedTypeSymbol> Interfaces => _symbol.Interfaces;
        public ImmutableArray<INamedTypeSymbol> AllInterfaces => _symbol.AllInterfaces;
        public ImmutableArray<IFieldSymbol> TupleElements => _symbol.TupleElements;

        public ImmutableArray<CustomModifier> GetTypeArgumentCustomModifiers(int ordinal)
            => _symbol.GetTypeArgumentCustomModifiers(ordinal);

        public INamedTypeSymbol Construct(params ITypeSymbol[] typeArguments)
            => _symbol.Construct(typeArguments);

        public INamedTypeSymbol Construct(ImmutableArray<ITypeSymbol> typeArguments, ImmutableArray<NullableAnnotation> typeArgumentNullableAnnotations)
            => _symbol.Construct(typeArguments, typeArgumentNullableAnnotations);

        public INamedTypeSymbol ConstructUnboundGenericType()
            => _symbol.ConstructUnboundGenericType();

        public ISymbol FindImplementationForInterfaceMember(ISymbol interfaceMember)
            => _symbol.FindImplementationForInterfaceMember(interfaceMember);

        public override ImmutableArray<ISymbol> GetMembers()
            => _members;

        public IEnumerable<string> MemberNames => throw new NotImplementedException();

        public override ImmutableArray<ISymbol> GetMembers(string name)
            => throw new NotImplementedException();

        public override ImmutableArray<INamedTypeSymbol> GetTypeMembers()
            => throw new NotImplementedException();

        public override ImmutableArray<INamedTypeSymbol> GetTypeMembers(string name)
            => throw new NotImplementedException();

        public override ImmutableArray<INamedTypeSymbol> GetTypeMembers(string name, int arity)
            => throw new NotImplementedException();

        public string ToDisplayString(NullableFlowState topLevelNullability, SymbolDisplayFormat format = null)
            => throw new NotImplementedException();

        public ImmutableArray<SymbolDisplayPart> ToDisplayParts(NullableFlowState topLevelNullability, SymbolDisplayFormat format = null)
            => throw new NotImplementedException();

        public string ToMinimalDisplayString(SemanticModel semanticModel, NullableFlowState topLevelNullability, int position, SymbolDisplayFormat format = null)
            => throw new NotImplementedException();

        public ImmutableArray<SymbolDisplayPart> ToMinimalDisplayParts(SemanticModel semanticModel, NullableFlowState topLevelNullability, int position, SymbolDisplayFormat format = null)
            => throw new NotImplementedException();

        ITypeSymbol ITypeSymbol.OriginalDefinition => _symbol.OriginalDefinition;
        public new INamedTypeSymbol OriginalDefinition => this;

        public bool IsSerializable => throw new NotImplementedException();

        public bool IsRefLikeType => _symbol.IsRefLikeType;

        public bool IsUnmanagedType => throw new NotImplementedException();

        public bool IsReadOnly => _symbol.IsReadOnly;

        public bool IsRecord => _symbol.IsRecord;

        public bool IsNativeIntegerType => _symbol.IsNativeIntegerType;

        public bool IsFileLocal => _symbol.IsFileLocal;

        public INamedTypeSymbol NativeIntegerUnderlyingType => _symbol.NativeIntegerUnderlyingType;

        NullableAnnotation ITypeSymbol.NullableAnnotation => throw new NotImplementedException();

        ITypeSymbol ITypeSymbol.WithNullableAnnotation(NullableAnnotation nullableAnnotation)
            => throw new NotImplementedException();
    }
}
