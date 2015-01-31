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
            private readonly INamedTypeSymbol symbol;
            private readonly ImmutableArray<ISymbol> members;

            public WrappedNamedTypeSymbol(INamedTypeSymbol symbol, bool canImplementImplicitly, IDocumentationCommentFormattingService docCommentFormattingService)
                : base(symbol, canImplementImplicitly, docCommentFormattingService)
            {
                this.symbol = symbol;

                var allMembers = this.symbol.GetMembers();
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

                this.members = ImmutableArray.CreateRange<ISymbol>(filteredMembers);
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
                    return this.symbol.Arity;
                }
            }

            public bool IsGenericType
            {
                get
                {
                    return this.symbol.IsGenericType;
                }
            }

            public bool IsUnboundGenericType
            {
                get
                {
                    return this.symbol.IsUnboundGenericType;
                }
            }

            public bool IsScriptClass
            {
                get
                {
                    return this.symbol.IsScriptClass;
                }
            }

            public bool IsImplicitClass
            {
                get
                {
                    return this.symbol.IsImplicitClass;
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
                    return this.symbol.TypeParameters;
                }
            }

            public ImmutableArray<ITypeSymbol> TypeArguments
            {
                get
                {
                    return this.symbol.TypeArguments;
                }
            }

            public IMethodSymbol DelegateInvokeMethod
            {
                get
                {
                    return this.symbol.DelegateInvokeMethod;
                }
            }

            public INamedTypeSymbol EnumUnderlyingType
            {
                get
                {
                    return this.symbol.EnumUnderlyingType;
                }
            }

            public INamedTypeSymbol ConstructedFrom
            {
                get
                {
                    return this.symbol.ConstructedFrom;
                }
            }

            public INamedTypeSymbol Construct(params ITypeSymbol[] typeArguments)
            {
                return this.symbol.Construct(typeArguments);
            }

            public INamedTypeSymbol ConstructUnboundGenericType()
            {
                return this.symbol.ConstructUnboundGenericType();
            }

            public ImmutableArray<IMethodSymbol> InstanceConstructors
            {
                get
                {
                    return this.symbol.InstanceConstructors;
                }
            }

            public ImmutableArray<IMethodSymbol> StaticConstructors
            {
                get
                {
                    return this.symbol.StaticConstructors;
                }
            }

            public ImmutableArray<IMethodSymbol> Constructors
            {
                get
                {
                    return this.symbol.Constructors;
                }
            }

            public ISymbol AssociatedSymbol
            {
                get
                {
                    return this.symbol.AssociatedSymbol;
                }
            }

            public TypeKind TypeKind
            {
                get
                {
                    return this.symbol.TypeKind;
                }
            }

            public INamedTypeSymbol BaseType
            {
                get
                {
                    return this.symbol.BaseType;
                }
            }

            public ImmutableArray<INamedTypeSymbol> Interfaces
            {
                get
                {
                    return this.symbol.Interfaces;
                }
            }

            public ImmutableArray<INamedTypeSymbol> AllInterfaces
            {
                get { return this.symbol.AllInterfaces; }
            }

            public bool IsReferenceType
            {
                get
                {
                    return this.symbol.IsReferenceType;
                }
            }

            public bool IsValueType
            {
                get
                {
                    return this.symbol.IsValueType;
                }
            }

            public bool IsAnonymousType
            {
                get
                {
                    return this.symbol.IsAnonymousType;
                }
            }

            ITypeSymbol ITypeSymbol.OriginalDefinition
            {
                get
                {
                    return this.symbol.OriginalDefinition;
                }
            }

            public SpecialType SpecialType
            {
                get
                {
                    return this.symbol.SpecialType;
                }
            }

            public ISymbol FindImplementationForInterfaceMember(ISymbol interfaceMember)
            {
                return this.symbol.FindImplementationForInterfaceMember(interfaceMember);
            }

            public override ImmutableArray<ISymbol> GetMembers()
            {
                return this.members;
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
                get { return this.symbol.MightContainExtensionMethods; }
            }
        }
    }
}
