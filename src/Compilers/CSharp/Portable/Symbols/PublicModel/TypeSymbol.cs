// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.PublicModel
{
    internal abstract class TypeSymbol : NamespaceOrTypeSymbol, ISymbol, ITypeSymbol
    {
        protected TypeSymbol(CodeAnalysis.NullableAnnotation nullableAnnotation)
        {
            NullableAnnotation = nullableAnnotation;
        }

        protected CodeAnalysis.NullableAnnotation NullableAnnotation { get; }

        protected abstract ITypeSymbol WithNullableAnnotation(CodeAnalysis.NullableAnnotation nullableAnnotation);

        internal abstract Symbols.TypeSymbol UnderlyingTypeSymbol { get; }

        CodeAnalysis.NullableAnnotation ITypeSymbol.NullableAnnotation => NullableAnnotation;

        ITypeSymbol ITypeSymbol.WithNullableAnnotation(CodeAnalysis.NullableAnnotation nullableAnnotation)
        {
            if (NullableAnnotation == nullableAnnotation)
            {
                return this;
            }
            else if (nullableAnnotation == UnderlyingTypeSymbol.DefaultNullableAnnotation)
            {
                return (ITypeSymbol)UnderlyingSymbol.ISymbol;
            }

            return WithNullableAnnotation(nullableAnnotation);
        }

        bool ISymbol.IsDefinition
        {
            get
            {
                return (object)this == ((ISymbol)this).OriginalDefinition;
            }
        }

        bool ISymbol.Equals(ISymbol other, CodeAnalysis.SymbolEqualityComparer equalityComparer)
        {
            return this.Equals(other as TypeSymbol, equalityComparer);
        }

        protected bool Equals(TypeSymbol otherType, CodeAnalysis.SymbolEqualityComparer equalityComparer)
        {
            if (otherType is null)
            {
                return false;
            }
            else if ((object)otherType == this)
            {
                return true;
            }

            var compareKind = equalityComparer.CompareKind;

            if (NullableAnnotation != otherType.NullableAnnotation && (compareKind & TypeCompareKind.IgnoreNullableModifiersForReferenceTypes) == 0 &&
                ((compareKind & TypeCompareKind.ObliviousNullableModifierMatchesAny) == 0 ||
                    (NullableAnnotation != CodeAnalysis.NullableAnnotation.None && otherType.NullableAnnotation != CodeAnalysis.NullableAnnotation.None)) &&
                !(UnderlyingTypeSymbol.IsValueType && !UnderlyingTypeSymbol.IsNullableType()))
            {
                return false;
            }

            return UnderlyingTypeSymbol.Equals(otherType.UnderlyingTypeSymbol, compareKind);
        }

        ITypeSymbol ITypeSymbol.OriginalDefinition
        {
            get
            {
                return UnderlyingTypeSymbol.OriginalDefinition.GetPublicSymbol();
            }
        }

        INamedTypeSymbol ITypeSymbol.BaseType
        {
            get
            {
                return UnderlyingTypeSymbol.BaseTypeNoUseSiteDiagnostics.GetPublicSymbol();
            }
        }

        ImmutableArray<INamedTypeSymbol> ITypeSymbol.Interfaces
        {
            get
            {
                return UnderlyingTypeSymbol.InterfacesNoUseSiteDiagnostics().GetPublicSymbols();
            }
        }

        ImmutableArray<INamedTypeSymbol> ITypeSymbol.AllInterfaces
        {
            get
            {
                return UnderlyingTypeSymbol.AllInterfacesNoUseSiteDiagnostics.GetPublicSymbols();
            }
        }

        ISymbol ITypeSymbol.FindImplementationForInterfaceMember(ISymbol interfaceMember)
        {
            return interfaceMember is Symbol symbol
                ? UnderlyingTypeSymbol.FindImplementationForInterfaceMember(symbol.UnderlyingSymbol).GetPublicSymbol()
                : null;
        }

        bool ITypeSymbol.IsUnmanagedType => !UnderlyingTypeSymbol.IsManagedTypeNoUseSiteDiagnostics;

        bool ITypeSymbol.IsReferenceType
        {
            get
            {
                return UnderlyingTypeSymbol.IsReferenceType;
            }
        }

        bool ITypeSymbol.IsValueType
        {
            get
            {
                return UnderlyingTypeSymbol.IsValueType;
            }
        }

        TypeKind ITypeSymbol.TypeKind
        {
            get
            {
                return UnderlyingTypeSymbol.TypeKind;
            }
        }

        bool ITypeSymbol.IsTupleType => UnderlyingTypeSymbol.IsTupleType;

        bool ITypeSymbol.IsNativeIntegerType => UnderlyingTypeSymbol.IsNativeIntegerType;

        string ITypeSymbol.ToDisplayString(CodeAnalysis.NullableFlowState topLevelNullability, SymbolDisplayFormat format)
        {
            return SymbolDisplay.ToDisplayString(this, topLevelNullability, format);
        }

        ImmutableArray<SymbolDisplayPart> ITypeSymbol.ToDisplayParts(CodeAnalysis.NullableFlowState topLevelNullability, SymbolDisplayFormat format)
        {
            return SymbolDisplay.ToDisplayParts(this, topLevelNullability, format);
        }

        string ITypeSymbol.ToMinimalDisplayString(SemanticModel semanticModel, CodeAnalysis.NullableFlowState topLevelNullability, int position, SymbolDisplayFormat format)
        {
            return SymbolDisplay.ToMinimalDisplayString(this, topLevelNullability, semanticModel, position, format);
        }

        ImmutableArray<SymbolDisplayPart> ITypeSymbol.ToMinimalDisplayParts(SemanticModel semanticModel, CodeAnalysis.NullableFlowState topLevelNullability, int position, SymbolDisplayFormat format)
        {
            return SymbolDisplay.ToMinimalDisplayParts(this, topLevelNullability, semanticModel, position, format);
        }

        bool ITypeSymbol.IsAnonymousType => UnderlyingTypeSymbol.IsAnonymousType;

        SpecialType ITypeSymbol.SpecialType => UnderlyingTypeSymbol.SpecialType;

        bool ITypeSymbol.IsRefLikeType => UnderlyingTypeSymbol.IsRefLikeType;

        bool ITypeSymbol.IsReadOnly => UnderlyingTypeSymbol.IsReadOnly;
    }
}
