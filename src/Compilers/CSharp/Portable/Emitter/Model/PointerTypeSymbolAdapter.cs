// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal partial class
#if DEBUG
        PointerTypeSymbolAdapter : SymbolAdapter,
#else
        PointerTypeSymbol :
#endif 
        Cci.IPointerTypeReference
    {
#if DEBUG
        internal PointerTypeSymbolAdapter(PointerTypeSymbol underlyingPointerTypeSymbol)
        {
            AdaptedPointerTypeSymbol = underlyingPointerTypeSymbol;
        }

        internal sealed override Symbol AdaptedSymbol => AdaptedPointerTypeSymbol;
        internal PointerTypeSymbol AdaptedPointerTypeSymbol { get; }
#else
        internal PointerTypeSymbol AdaptedPointerTypeSymbol => this;
#endif 
    }

    internal partial class PointerTypeSymbol
    {
#if DEBUG
        private PointerTypeSymbolAdapter _lazyAdapter;

        protected sealed override SymbolAdapter GetCciAdapterImpl() => GetCciAdapter();
#endif
        internal new
#if DEBUG
            PointerTypeSymbolAdapter
#else
            PointerTypeSymbol
#endif
            GetCciAdapter()
        {
#if DEBUG
            if (_lazyAdapter is null)
            {
                return InterlockedOperations.Initialize(ref _lazyAdapter, new PointerTypeSymbolAdapter(this));
            }

            return _lazyAdapter;
#else
            return this;
#endif
        }
    }

    internal partial class
#if DEBUG
        PointerTypeSymbolAdapter
#else
        PointerTypeSymbol
#endif
    {
        Cci.ITypeReference Cci.IPointerTypeReference.GetTargetType(EmitContext context)
        {
            var type = ((PEModuleBuilder)context.Module).Translate(AdaptedPointerTypeSymbol.PointedAtType, syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNodeOpt, diagnostics: context.Diagnostics);

            if (AdaptedPointerTypeSymbol.PointedAtTypeWithAnnotations.CustomModifiers.Length == 0)
            {
                return type;
            }
            else
            {
                return new Cci.ModifiedTypeReference(type, ImmutableArray<Cci.ICustomModifier>.CastUp(AdaptedPointerTypeSymbol.PointedAtTypeWithAnnotations.CustomModifiers));
            }
        }

        bool Cci.ITypeReference.IsEnum
        {
            get { return false; }
        }

        bool Cci.ITypeReference.IsValueType
        {
            get { return false; }
        }

        Cci.ITypeDefinition Cci.ITypeReference.GetResolvedType(EmitContext context)
        {
            return null;
        }

        Cci.PrimitiveTypeCode Cci.ITypeReference.TypeCode
        {
            get { return Cci.PrimitiveTypeCode.Pointer; }
        }

        TypeDefinitionHandle Cci.ITypeReference.TypeDef
        {
            get { return default(TypeDefinitionHandle); }
        }

        Cci.IGenericMethodParameterReference Cci.ITypeReference.AsGenericMethodParameterReference
        {
            get { return null; }
        }

        Cci.IGenericTypeInstanceReference Cci.ITypeReference.AsGenericTypeInstanceReference
        {
            get { return null; }
        }

        Cci.IGenericTypeParameterReference Cci.ITypeReference.AsGenericTypeParameterReference
        {
            get { return null; }
        }

        Cci.INamespaceTypeDefinition Cci.ITypeReference.AsNamespaceTypeDefinition(EmitContext context)
        {
            return null;
        }

        Cci.INamespaceTypeReference Cci.ITypeReference.AsNamespaceTypeReference
        {
            get { return null; }
        }

        Cci.INestedTypeDefinition Cci.ITypeReference.AsNestedTypeDefinition(EmitContext context)
        {
            return null;
        }

        Cci.INestedTypeReference Cci.ITypeReference.AsNestedTypeReference
        {
            get { return null; }
        }

        Cci.ISpecializedNestedTypeReference Cci.ITypeReference.AsSpecializedNestedTypeReference
        {
            get { return null; }
        }

        Cci.ITypeDefinition Cci.ITypeReference.AsTypeDefinition(EmitContext context)
        {
            return null;
        }

        void Cci.IReference.Dispatch(Cci.MetadataVisitor visitor)
        {
            visitor.Visit((Cci.IPointerTypeReference)this);
        }

        Cci.IDefinition Cci.IReference.AsDefinition(EmitContext context)
        {
            return null;
        }
    }
}
