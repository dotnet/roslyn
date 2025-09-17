// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

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
        /// If this is a tuple type with element names, returns the symbol for the tuple type without names.
        /// Otherwise, returns null.
        /// </summary>
        INamedTypeSymbol INamedTypeSymbol.TupleUnderlyingType
        {
            get
            {
                var type = UnderlyingNamedTypeSymbol;
                var tupleUnderlyingType = type.TupleUnderlyingType;
                return type.Equals(tupleUnderlyingType, TypeCompareKind.ConsiderEverything) ?
                    null :
                    tupleUnderlyingType.GetPublicSymbol();
            }
        }

        bool INamedTypeSymbol.IsComImport => UnderlyingNamedTypeSymbol.IsComImport;

        bool INamedTypeSymbol.IsGenericType => UnderlyingNamedTypeSymbol.IsGenericType;

        bool INamedTypeSymbol.IsUnboundGenericType => UnderlyingNamedTypeSymbol.IsUnboundGenericType;

        bool INamedTypeSymbol.IsScriptClass => UnderlyingNamedTypeSymbol.IsScriptClass;

        bool INamedTypeSymbol.IsImplicitClass => UnderlyingNamedTypeSymbol.IsImplicitClass;

        bool INamedTypeSymbol.MightContainExtensionMethods => UnderlyingNamedTypeSymbol.MightContainExtensionMethods;

        bool INamedTypeSymbol.IsSerializable => UnderlyingNamedTypeSymbol.IsSerializable;

        bool INamedTypeSymbol.IsFileLocal =>
            // Internally we can treat a metadata type as being a file-local type for EE.
            // For public API, only source types are considered file-local types.
            UnderlyingNamedTypeSymbol.OriginalDefinition is SourceMemberContainerTypeSymbol
                && UnderlyingNamedTypeSymbol.IsFileLocal;

        INamedTypeSymbol INamedTypeSymbol.NativeIntegerUnderlyingType => UnderlyingNamedTypeSymbol.NativeIntegerUnderlyingType.GetPublicSymbol();

#nullable enable
        bool INamedTypeSymbol.IsExtension
        {
            get
            {
                bool isExtension = UnderlyingNamedTypeSymbol.IsExtension;

                Debug.Assert(!isExtension
                    || (!string.IsNullOrEmpty(UnderlyingNamedTypeSymbol.ExtensionGroupingName) && !string.IsNullOrEmpty(UnderlyingNamedTypeSymbol.ExtensionMarkerName)));

                return isExtension;
            }
        }

        string? INamedTypeSymbol.ExtensionGroupingName => UnderlyingNamedTypeSymbol.ExtensionGroupingName;
        string? INamedTypeSymbol.ExtensionMarkerName => UnderlyingNamedTypeSymbol.ExtensionMarkerName;

        IParameterSymbol? INamedTypeSymbol.ExtensionParameter => UnderlyingNamedTypeSymbol.ExtensionParameter?.GetPublicSymbol();
#nullable disable

        #region ISymbol Members

        protected sealed override void Accept(SymbolVisitor visitor)
        {
            visitor.VisitNamedType(this);
        }

        protected sealed override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
            return visitor.VisitNamedType(this);
        }

        protected sealed override TResult Accept<TArgument, TResult>(SymbolVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNamedType(this, argument);
        }

        #endregion
    }
}
