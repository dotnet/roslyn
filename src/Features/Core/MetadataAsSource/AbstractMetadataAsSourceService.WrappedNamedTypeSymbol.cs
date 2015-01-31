// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.DocumentationCommentFormatting;
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
                }

                throw ExceptionUtilities.Unreachable;
            }

            public int Arity
            {
                get
                {
                    return _symbol.Arity;
                }
            }

            public bool IsGenericType
            {
                get
                {
                    return _symbol.IsGenericType;
                }
            }

            public bool IsUnboundGenericType
            {
                get
                {
                    return _symbol.IsUnboundGenericType;
                }
            }

            public bool IsScriptClass
            {
                get
                {
                    return _symbol.IsScriptClass;
                }
            }

            public bool IsImplicitClass
            {
                get
                {
                    return _symbol.IsImplicitClass;
                }
            }

            public IEnumerable<string> MemberNames
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public ImmutableArray<ITypeParameterSymbol> TypeParameters
            {
                get
                {
                    return _symbol.TypeParameters;
                }
            }

            public ImmutableArray<ITypeSymbol> TypeArguments
            {
                get
                {
                    return _symbol.TypeArguments;
                }
            }

            public IMethodSymbol DelegateInvokeMethod
            {
                get
                {
                    return _symbol.DelegateInvokeMethod;
                }
            }

            public INamedTypeSymbol EnumUnderlyingType
            {
                get
                {
                    return _symbol.EnumUnderlyingType;
                }
            }

            public INamedTypeSymbol ConstructedFrom
            {
                get
                {
                    return _symbol.ConstructedFrom;
                }
            }

            public INamedTypeSymbol Construct(params ITypeSymbol[] typeArguments)
            {
                return _symbol.Construct(typeArguments);
            }

            public INamedTypeSymbol ConstructUnboundGenericType()
            {
                return _symbol.ConstructUnboundGenericType();
            }

            public ImmutableArray<IMethodSymbol> InstanceConstructors
            {
                get
                {
                    return _symbol.InstanceConstructors;
                }
            }

            public ImmutableArray<IMethodSymbol> StaticConstructors
            {
                get
                {
                    return _symbol.StaticConstructors;
                }
            }

            public ImmutableArray<IMethodSymbol> Constructors
            {
                get
                {
                    return _symbol.Constructors;
                }
            }

            public ISymbol AssociatedSymbol
            {
                get
                {
                    return _symbol.AssociatedSymbol;
                }
            }

            public TypeKind TypeKind
            {
                get
                {
                    return _symbol.TypeKind;
                }
            }

            public INamedTypeSymbol BaseType
            {
                get
                {
                    return _symbol.BaseType;
                }
            }

            public ImmutableArray<INamedTypeSymbol> Interfaces
            {
                get
                {
                    return _symbol.Interfaces;
                }
            }

            public ImmutableArray<INamedTypeSymbol> AllInterfaces
            {
                get { return _symbol.AllInterfaces; }
            }

            public bool IsReferenceType
            {
                get
                {
                    return _symbol.IsReferenceType;
                }
            }

            public bool IsValueType
            {
                get
                {
                    return _symbol.IsValueType;
                }
            }

            public bool IsAnonymousType
            {
                get
                {
                    return _symbol.IsAnonymousType;
                }
            }

            ITypeSymbol ITypeSymbol.OriginalDefinition
            {
                get
                {
                    return _symbol.OriginalDefinition;
                }
            }

            public SpecialType SpecialType
            {
                get
                {
                    return _symbol.SpecialType;
                }
            }

            public ISymbol FindImplementationForInterfaceMember(ISymbol interfaceMember)
            {
                return _symbol.FindImplementationForInterfaceMember(interfaceMember);
            }

            public override ImmutableArray<ISymbol> GetMembers()
            {
                return _members;
            }

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

            public new INamedTypeSymbol OriginalDefinition
            {
                get
                {
                    return this;
                }
            }

            public bool MightContainExtensionMethods
            {
                get { return _symbol.MightContainExtensionMethods; }
            }
        }
    }
}
