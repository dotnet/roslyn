// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.DocumentationComments;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MetadataAsSource
{
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
                                      where m.DeclaredAccessibility == Accessibility.Public ||
                                            m.DeclaredAccessibility == Accessibility.Protected ||
                                            m.DeclaredAccessibility == Accessibility.ProtectedOrInternal
                                      where m.Kind == SymbolKind.Event ||
                                            m.Kind == SymbolKind.Field ||
                                            m.Kind == SymbolKind.Method ||
                                            m.Kind == SymbolKind.NamedType ||
                                            m.Kind == SymbolKind.Property
                                      select WrapMember(m, canImplementImplicitly, docCommentFormattingService);

                _members = ImmutableArray.CreateRange<ISymbol>(filteredMembers);
            }

            private static ISymbol WrapMember(ISymbol m, bool canImplementImplicitly, IDocumentationCommentFormattingService docCommentFormattingService)
            {
                switch (m.Kind)
                {
                    case SymbolKind.Field:
                        return new WrappedFieldSymbol((IFieldSymbol)m, docCommentFormattingService);

                    case SymbolKind.Event:
                        return new WrappedEventSymbol((IEventSymbol)m, canImplementImplicitly, docCommentFormattingService);

                    case SymbolKind.Method:
                        return new WrappedMethodSymbol((IMethodSymbol)m, canImplementImplicitly, docCommentFormattingService);

                    case SymbolKind.NamedType:
                        return new WrappedNamedTypeSymbol((INamedTypeSymbol)m, canImplementImplicitly, docCommentFormattingService);

                    case SymbolKind.Property:
                        return new WrappedPropertySymbol((IPropertySymbol)m, canImplementImplicitly, docCommentFormattingService);

                    default:
                        throw ExceptionUtilities.UnexpectedValue(m.Kind);
                }
            }

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
            {
                return _symbol.GetTypeArgumentCustomModifiers(ordinal);
            }

            public INamedTypeSymbol Construct(params ITypeSymbol[] typeArguments)
            {
                return _symbol.Construct(typeArguments);
            }

            public INamedTypeSymbol Construct(ImmutableArray<ITypeSymbol> typeArguments, ImmutableArray<NullableAnnotation> typeArgumentNullableAnnotations)
            {
                return _symbol.Construct(typeArguments, typeArgumentNullableAnnotations);
            }

            public INamedTypeSymbol ConstructUnboundGenericType()
            {
                return _symbol.ConstructUnboundGenericType();
            }

            public ISymbol FindImplementationForInterfaceMember(ISymbol interfaceMember)
            {
                return _symbol.FindImplementationForInterfaceMember(interfaceMember);
            }

            public override ImmutableArray<ISymbol> GetMembers()
            {
                return _members;
            }

            public IEnumerable<string> MemberNames => throw new NotImplementedException();

            public override ImmutableArray<ISymbol> GetMembers(string name)
            {
                throw new NotImplementedException();
            }

            public override ImmutableArray<INamedTypeSymbol> GetTypeMembers()
            {
                throw new NotImplementedException();
            }

            public override ImmutableArray<INamedTypeSymbol> GetTypeMembers(string name)
            {
                throw new NotImplementedException();
            }

            public override ImmutableArray<INamedTypeSymbol> GetTypeMembers(string name, int arity)
            {
                throw new NotImplementedException();
            }

            public string ToDisplayString(NullableFlowState topLevelNullability, SymbolDisplayFormat format = null)
            {
                throw new NotImplementedException();
            }

            public ImmutableArray<SymbolDisplayPart> ToDisplayParts(NullableFlowState topLevelNullability, SymbolDisplayFormat format = null)
            {
                throw new NotImplementedException();
            }

            public string ToMinimalDisplayString(SemanticModel semanticModel, NullableFlowState topLevelNullability, int position, SymbolDisplayFormat format = null)
            {
                throw new NotImplementedException();
            }

            public ImmutableArray<SymbolDisplayPart> ToMinimalDisplayParts(SemanticModel semanticModel, NullableFlowState topLevelNullability, int position, SymbolDisplayFormat format = null)
            {
                throw new NotImplementedException();
            }

            ITypeSymbol ITypeSymbol.OriginalDefinition => _symbol.OriginalDefinition;
            public new INamedTypeSymbol OriginalDefinition => this;

            public bool IsSerializable => throw new NotImplementedException();

            public bool IsRefLikeType => _symbol.IsRefLikeType;

            public bool IsUnmanagedType => throw new NotImplementedException();

            public bool IsReadOnly => _symbol.IsReadOnly;

            NullableAnnotation ITypeSymbol.NullableAnnotation => throw new System.NotImplementedException();

            ITypeSymbol ITypeSymbol.WithNullableAnnotation(NullableAnnotation nullableAnnotation)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}
