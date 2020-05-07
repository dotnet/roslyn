// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.SourceGeneration
{
    internal static partial class CodeGenerator
    {
        //public static INamedTypeSymbol ClassType(ITypeSymbol type = null)
        //    => new DiscardSymbol(type);

        //public static INamedTypeSymbol StructType(ITypeSymbol type = null)
        //    => new DiscardSymbol(type);

        //public static INamedTypeSymbol InterfaceType(ITypeSymbol type = null)
        //    => new DiscardSymbol(type);

        //public static INamedTypeSymbol EnumType(ITypeSymbol type = null)
        //    => new DiscardSymbol(type);

        public static INamedTypeSymbol TupleType(
            ImmutableArray<IFieldSymbol> tupleElements,
            NullableAnnotation nullableAnnotation = NullableAnnotation.None)
        {
            return new NamedTypeSymbol(
                CodeAnalysis.SpecialType.None,
                tupleElements,
                nullableAnnotation,
                containingSymbol: null);
        }

        //public static INamedTypeSymbol DelegateType(ITypeSymbol type = null)
        //    => new DiscardSymbol(type);

        //public static INamedTypeSymbol AnonymousType(ITypeSymbol type = null)
        //    => new DiscardSymbol(type);

        public static INamedTypeSymbol SpecialType(
            SpecialType specialType,
            NullableAnnotation nullableAnnotation = NullableAnnotation.None)
        {
            return new NamedTypeSymbol(
                specialType,
                tupleElements: default,
                nullableAnnotation,
                containingSymbol: null);
        }

        public static INamedTypeSymbol With(
            this INamedTypeSymbol type,
            Optional<ImmutableArray<IFieldSymbol>> tupleElements = default,
            Optional<NullableAnnotation> nullableAnnotation = default,
            Optional<ISymbol> containingSymbol = default)
        {
            return new NamedTypeSymbol(
                type.SpecialType,
                tupleElements.GetValueOr(type.TupleElements),
                nullableAnnotation.GetValueOr(type.NullableAnnotation),
                containingSymbol.GetValueOr(type.ContainingSymbol));
        }

        private class NamedTypeSymbol : TypeSymbol, INamedTypeSymbol
        {
            public NamedTypeSymbol(
                SpecialType specialType,
                ImmutableArray<IFieldSymbol> tupleElements,
                NullableAnnotation nullableAnnotation,
                ISymbol containingSymbol)
            {
                SpecialType = specialType;
                TupleElements = tupleElements;
                NullableAnnotation = nullableAnnotation;
                ContainingSymbol = containingSymbol;
            }

            public override ISymbol ContainingSymbol { get; }
            public override SymbolKind Kind => SymbolKind.NamedType;
            public override NullableAnnotation NullableAnnotation { get; }
            public override SpecialType SpecialType { get; }
            public override TypeKind TypeKind { get; }

            public ImmutableArray<ITypeParameterSymbol> TypeParameters => throw new NotImplementedException();

            public ImmutableArray<ITypeSymbol> TypeArguments => throw new NotImplementedException();

            public IMethodSymbol DelegateInvokeMethod => throw new NotImplementedException();

            public INamedTypeSymbol EnumUnderlyingType => throw new NotImplementedException();

            public ImmutableArray<IMethodSymbol> Constructors => throw new NotImplementedException();

            public ImmutableArray<IFieldSymbol> TupleElements { get; }

            public override void Accept(SymbolVisitor visitor)
                => visitor.VisitNamedType(this);

            public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
                => visitor.VisitNamedType(this);

            public INamedTypeSymbol Construct(params ITypeSymbol[] typeArguments)
            {
                throw new NotImplementedException();
            }

            #region default implementation

            public int Arity => throw new NotImplementedException();
            public bool IsGenericType => throw new NotImplementedException();
            public bool IsUnboundGenericType => throw new NotImplementedException();
            public bool IsScriptClass => throw new NotImplementedException();
            public bool IsImplicitClass => throw new NotImplementedException();
            public bool IsComImport => throw new NotImplementedException();
            public IEnumerable<string> MemberNames => throw new NotImplementedException();

            public ImmutableArray<NullableAnnotation> TypeArgumentNullableAnnotations => throw new NotImplementedException();

            public INamedTypeSymbol ConstructedFrom => throw new NotImplementedException();
            public ImmutableArray<IMethodSymbol> InstanceConstructors => throw new NotImplementedException();
            public ImmutableArray<IMethodSymbol> StaticConstructors => throw new NotImplementedException();

            public ISymbol AssociatedSymbol => throw new NotImplementedException();
            public bool MightContainExtensionMethods => throw new NotImplementedException();
            public INamedTypeSymbol TupleUnderlyingType => throw new NotImplementedException();

            public INamedTypeSymbol Construct(ImmutableArray<ITypeSymbol> typeArguments, ImmutableArray<NullableAnnotation> typeArgumentNullableAnnotations) => throw new NotImplementedException();
            public INamedTypeSymbol ConstructUnboundGenericType() => throw new NotImplementedException();
            public ImmutableArray<CustomModifier> GetTypeArgumentCustomModifiers(int ordinal) => throw new NotImplementedException();

            public bool IsSerializable => throw new NotImplementedException();

            public INamedTypeSymbol NativeIntegerUnderlyingType => throw new NotImplementedException();
            INamedTypeSymbol INamedTypeSymbol.OriginalDefinition => throw new NotImplementedException();

            #endregion
        }
    }
}
