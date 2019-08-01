using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    internal static partial class NullableExtensions
    {
        private sealed class NamedTypeSymbolWithNullableAnnotation : TypeSymbolWithNullableAnnotation, INamedTypeSymbol
        {
            public NamedTypeSymbolWithNullableAnnotation(INamedTypeSymbol wrappedSymbol, NullableAnnotation nullability) : base(wrappedSymbol, nullability)
            {
            }

            private new INamedTypeSymbol WrappedSymbol => (INamedTypeSymbol)base.WrappedSymbol;

            public override void Accept(SymbolVisitor visitor)
            {
                visitor.VisitNamedType(this);
            }

            public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
            {
                return visitor.VisitNamedType(this);
            }

            #region INamedTypeSymbol Implementation Forwards

            public int Arity => WrappedSymbol.Arity;
            public bool IsGenericType => WrappedSymbol.IsGenericType;
            public bool IsUnboundGenericType => WrappedSymbol.IsUnboundGenericType;
            public bool IsScriptClass => WrappedSymbol.IsScriptClass;
            public bool IsImplicitClass => WrappedSymbol.IsImplicitClass;
            public bool IsComImport => WrappedSymbol.IsComImport;
            public IEnumerable<string> MemberNames => WrappedSymbol.MemberNames;
            public ImmutableArray<ITypeParameterSymbol> TypeParameters => WrappedSymbol.TypeParameters;
            public ImmutableArray<ITypeSymbol> TypeArguments => WrappedSymbol.TypeArguments;
            public ImmutableArray<NullableAnnotation> TypeArgumentNullableAnnotations => WrappedSymbol.TypeArgumentNullableAnnotations;
            public IMethodSymbol DelegateInvokeMethod => WrappedSymbol.DelegateInvokeMethod;
            public INamedTypeSymbol EnumUnderlyingType => WrappedSymbol.EnumUnderlyingType;
            public INamedTypeSymbol ConstructedFrom => WrappedSymbol.ConstructedFrom;
            public ImmutableArray<IMethodSymbol> InstanceConstructors => WrappedSymbol.InstanceConstructors;
            public ImmutableArray<IMethodSymbol> StaticConstructors => WrappedSymbol.StaticConstructors;
            public ImmutableArray<IMethodSymbol> Constructors => WrappedSymbol.Constructors;
            public ISymbol AssociatedSymbol => WrappedSymbol.AssociatedSymbol;
            public bool MightContainExtensionMethods => WrappedSymbol.MightContainExtensionMethods;
            public INamedTypeSymbol TupleUnderlyingType => WrappedSymbol.TupleUnderlyingType;
            public ImmutableArray<IFieldSymbol> TupleElements => WrappedSymbol.TupleElements;
            public bool IsSerializable => WrappedSymbol.IsSerializable;
            INamedTypeSymbol INamedTypeSymbol.OriginalDefinition => WrappedSymbol.OriginalDefinition;

            public INamedTypeSymbol Construct(params ITypeSymbol[] typeArguments)
            {
                return WrappedSymbol.Construct(typeArguments).WithNullability(Nullability);
            }

            public INamedTypeSymbol Construct(ImmutableArray<ITypeSymbol> typeArguments, ImmutableArray<NullableAnnotation> typeArgumentNullableAnnotations)
            {
                return WrappedSymbol.Construct(typeArguments, typeArgumentNullableAnnotations).WithNullability(Nullability);
            }

            public INamedTypeSymbol ConstructUnboundGenericType()
            {
                return WrappedSymbol.ConstructUnboundGenericType();
            }

            public ImmutableArray<CustomModifier> GetTypeArgumentCustomModifiers(int ordinal)
            {
                return WrappedSymbol.GetTypeArgumentCustomModifiers(ordinal);
            }

            #endregion
        }
    }
}
