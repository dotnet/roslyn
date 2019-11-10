// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.PublicModel
{
    internal abstract class NamedTypeSymbol : TypeSymbol, INamedTypeSymbol
    {
        private ImmutableArray<ITypeSymbol> _lazyTypeArguments;

        public NamedTypeSymbol(CodeAnalysis.NullableAnnotation nullableAnnotation = CodeAnalysis.NullableAnnotation.None)
            : base(nullableAnnotation)
        {
        }

        internal abstract Symbols.NamedTypeSymbol UnderlyingNamedTypeSymbol { get; }

        int INamedTypeSymbol.Arity
        {
            get
            {
                return UnderlyingNamedTypeSymbol.Arity;
            }
        }

        ImmutableArray<IMethodSymbol> INamedTypeSymbol.InstanceConstructors
        {
            get
            {
                return UnderlyingNamedTypeSymbol.InstanceConstructors.GetPublicSymbols();
            }
        }

        ImmutableArray<IMethodSymbol> INamedTypeSymbol.StaticConstructors
        {
            get
            {
                return UnderlyingNamedTypeSymbol.StaticConstructors.GetPublicSymbols();
            }
        }

        ImmutableArray<IMethodSymbol> INamedTypeSymbol.Constructors
        {
            get
            {
                return UnderlyingNamedTypeSymbol.Constructors.GetPublicSymbols();
            }
        }

        IEnumerable<string> INamedTypeSymbol.MemberNames
        {
            get
            {
                return UnderlyingNamedTypeSymbol.MemberNames;
            }
        }

        ImmutableArray<ITypeParameterSymbol> INamedTypeSymbol.TypeParameters
        {
            get
            {
                return UnderlyingNamedTypeSymbol.TypeParameters.GetPublicSymbols();
            }
        }

        ImmutableArray<ITypeSymbol> INamedTypeSymbol.TypeArguments
        {
            get
            {
                if (_lazyTypeArguments.IsDefault)
                {

                    ImmutableInterlocked.InterlockedCompareExchange(ref _lazyTypeArguments, UnderlyingNamedTypeSymbol.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics.GetPublicSymbols(), default);
                }

                return _lazyTypeArguments;
            }
        }

        ImmutableArray<CodeAnalysis.NullableAnnotation> INamedTypeSymbol.TypeArgumentNullableAnnotations
        {
            get
            {
                return UnderlyingNamedTypeSymbol.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics.ToPublicAnnotations();
            }
        }

        ImmutableArray<CustomModifier> INamedTypeSymbol.GetTypeArgumentCustomModifiers(int ordinal)
        {
            return UnderlyingNamedTypeSymbol.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[ordinal].CustomModifiers;
        }

        INamedTypeSymbol INamedTypeSymbol.OriginalDefinition
        {
            get
            {
                return UnderlyingNamedTypeSymbol.OriginalDefinition.GetPublicSymbol();
            }
        }

        IMethodSymbol INamedTypeSymbol.DelegateInvokeMethod
        {
            get
            {
                return UnderlyingNamedTypeSymbol.DelegateInvokeMethod.GetPublicSymbol();
            }
        }

        INamedTypeSymbol INamedTypeSymbol.EnumUnderlyingType
        {
            get
            {
                return UnderlyingNamedTypeSymbol.EnumUnderlyingType.GetPublicSymbol();
            }
        }

        INamedTypeSymbol INamedTypeSymbol.ConstructedFrom
        {
            get
            {
                return UnderlyingNamedTypeSymbol.ConstructedFrom.GetPublicSymbol();
            }
        }

        INamedTypeSymbol INamedTypeSymbol.Construct(params ITypeSymbol[] typeArguments)
        {
            return UnderlyingNamedTypeSymbol.Construct(ConstructTypeArguments(typeArguments), unbound: false).GetPublicSymbol();
        }

        INamedTypeSymbol INamedTypeSymbol.Construct(ImmutableArray<ITypeSymbol> typeArguments, ImmutableArray<CodeAnalysis.NullableAnnotation> typeArgumentNullableAnnotations)
        {
            return UnderlyingNamedTypeSymbol.Construct(ConstructTypeArguments(typeArguments, typeArgumentNullableAnnotations), unbound: false).GetPublicSymbol();
        }

        INamedTypeSymbol INamedTypeSymbol.ConstructUnboundGenericType()
        {
            return UnderlyingNamedTypeSymbol.ConstructUnboundGenericType().GetPublicSymbol();
        }

        ISymbol INamedTypeSymbol.AssociatedSymbol
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Returns fields that represent tuple elements for types that are tuples.
        ///
        /// If this type is not a tuple, then returns default.
        /// </summary>
        ImmutableArray<IFieldSymbol> INamedTypeSymbol.TupleElements
        {
            get
            {
                return UnderlyingNamedTypeSymbol.TupleElements.GetPublicSymbols();
            }
        }

        /// <summary>
        /// If this is a tuple type symbol, returns the symbol for its underlying type.
        /// Otherwise, returns null.
        /// </summary>
        INamedTypeSymbol INamedTypeSymbol.TupleUnderlyingType
        {
            get
            {
                return UnderlyingNamedTypeSymbol.TupleUnderlyingType.GetPublicSymbol();
            }
        }

        bool INamedTypeSymbol.IsComImport => UnderlyingNamedTypeSymbol.IsComImport;

        bool INamedTypeSymbol.IsGenericType => UnderlyingNamedTypeSymbol.IsGenericType;

        bool INamedTypeSymbol.IsUnboundGenericType => UnderlyingNamedTypeSymbol.IsUnboundGenericType;

        bool INamedTypeSymbol.IsScriptClass => UnderlyingNamedTypeSymbol.IsScriptClass;

        bool INamedTypeSymbol.IsImplicitClass => UnderlyingNamedTypeSymbol.IsImplicitClass;

        bool INamedTypeSymbol.MightContainExtensionMethods => UnderlyingNamedTypeSymbol.MightContainExtensionMethods;

        bool INamedTypeSymbol.IsSerializable => UnderlyingNamedTypeSymbol.IsSerializable;

        #region ISymbol Members

        protected sealed override void Accept(SymbolVisitor visitor)
        {
            visitor.VisitNamedType(this);
        }

        protected sealed override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
            return visitor.VisitNamedType(this);
        }

        #endregion
    }
}
