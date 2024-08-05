// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal partial class
#if DEBUG
        ArrayTypeSymbolAdapter : SymbolAdapter,
#else
        ArrayTypeSymbol :
#endif 
        Cci.IArrayTypeReference
    {
        Cci.ITypeReference Cci.IArrayTypeReference.GetElementType(EmitContext context)
        {
            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;

            TypeWithAnnotations elementType = AdaptedArrayTypeSymbol.ElementTypeWithAnnotations;
            var type = moduleBeingBuilt.Translate(elementType.Type, syntaxNodeOpt: (CSharpSyntaxNode?)context.SyntaxNode, diagnostics: context.Diagnostics, eraseExtensions: false);

            if (elementType.CustomModifiers.Length == 0)
            {
                return type;
            }
            else
            {
                return new Cci.ModifiedTypeReference(type, ImmutableArray<Cci.ICustomModifier>.CastUp(elementType.CustomModifiers));
            }
        }

        bool Cci.IArrayTypeReference.IsSZArray
        {
            get
            {
                return AdaptedArrayTypeSymbol.IsSZArray;
            }
        }

        ImmutableArray<int> Cci.IArrayTypeReference.LowerBounds => AdaptedArrayTypeSymbol.LowerBounds;
        int Cci.IArrayTypeReference.Rank => AdaptedArrayTypeSymbol.Rank;
        ImmutableArray<int> Cci.IArrayTypeReference.Sizes => AdaptedArrayTypeSymbol.Sizes;

        void Cci.IReference.Dispatch(Cci.MetadataVisitor visitor)
        {
            visitor.Visit((Cci.IArrayTypeReference)this);
        }

        bool Cci.ITypeReference.IsEnum => false;
        bool Cci.ITypeReference.IsValueType => false;

        TypeDefinitionHandle Cci.ITypeReference.TypeDef => default(TypeDefinitionHandle);
        Cci.PrimitiveTypeCode Cci.ITypeReference.TypeCode => Cci.PrimitiveTypeCode.NotPrimitive;

        Cci.ITypeDefinition? Cci.ITypeReference.GetResolvedType(EmitContext context) => null;
        Cci.IGenericMethodParameterReference? Cci.ITypeReference.AsGenericMethodParameterReference => null;
        Cci.IGenericTypeInstanceReference? Cci.ITypeReference.AsGenericTypeInstanceReference => null;
        Cci.IGenericTypeParameterReference? Cci.ITypeReference.AsGenericTypeParameterReference => null;
        Cci.INamespaceTypeDefinition? Cci.ITypeReference.AsNamespaceTypeDefinition(EmitContext context) => null;
        Cci.INamespaceTypeReference? Cci.ITypeReference.AsNamespaceTypeReference => null;
        Cci.INestedTypeDefinition? Cci.ITypeReference.AsNestedTypeDefinition(EmitContext context) => null;
        Cci.INestedTypeReference? Cci.ITypeReference.AsNestedTypeReference => null;
        Cci.ISpecializedNestedTypeReference? Cci.ITypeReference.AsSpecializedNestedTypeReference => null;
        Cci.ITypeDefinition? Cci.ITypeReference.AsTypeDefinition(EmitContext context) => null;
        Cci.IDefinition? Cci.IReference.AsDefinition(EmitContext context) => null;
    }

    internal partial class ArrayTypeSymbol
    {
#if DEBUG
        private ArrayTypeSymbolAdapter? _lazyAdapter;

        protected sealed override SymbolAdapter GetCciAdapterImpl() => GetCciAdapter();

        internal new ArrayTypeSymbolAdapter GetCciAdapter()
        {
            if (_lazyAdapter is null)
            {
                return InterlockedOperations.Initialize(ref _lazyAdapter, new ArrayTypeSymbolAdapter(this));
            }

            return _lazyAdapter;
        }
#else
        internal ArrayTypeSymbol AdaptedArrayTypeSymbol => this;

        internal new ArrayTypeSymbol GetCciAdapter()
        {
            return this;
        }
#endif
    }

#if DEBUG
    internal partial class ArrayTypeSymbolAdapter
    {
        internal ArrayTypeSymbolAdapter(ArrayTypeSymbol underlyingArrayTypeSymbol)
        {
            AdaptedArrayTypeSymbol = underlyingArrayTypeSymbol;
        }

        internal sealed override Symbol AdaptedSymbol => AdaptedArrayTypeSymbol;
        internal ArrayTypeSymbol AdaptedArrayTypeSymbol { get; }
    }
#endif 
}
