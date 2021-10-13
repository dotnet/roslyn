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
        RefTypeSymbolAdapter : SymbolAdapter,
#else
        RefTypeSymbol :
#endif
        Cci.IRefTypeReference
    {
        Cci.ITypeReference Cci.IRefTypeReference.GetReferencedType(EmitContext context)
        {
            var type = ((PEModuleBuilder)context.Module).Translate(AdaptedRefTypeSymbol.ReferencedTypeWithAnnotations.Type, syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNode, diagnostics: context.Diagnostics);

            if (AdaptedRefTypeSymbol.ReferencedTypeWithAnnotations.CustomModifiers.Length == 0)
            {
                return type;
            }
            else
            {
                return new Cci.ModifiedTypeReference(type, ImmutableArray<Cci.ICustomModifier>.CastUp(AdaptedRefTypeSymbol.ReferencedTypeWithAnnotations.CustomModifiers));
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
            get { return Cci.PrimitiveTypeCode.Reference; }
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

    internal partial class RefTypeSymbol
    {

#if DEBUG
        private RefTypeSymbolAdapter _lazyAdapter;

        protected sealed override SymbolAdapter GetCciAdapterImpl() => GetCciAdapter();

        internal new RefTypeSymbolAdapter GetCciAdapter()
        {
            if (_lazyAdapter is null)
            {
                return InterlockedOperations.Initialize(ref _lazyAdapter, new RefTypeSymbolAdapter(this));
            }

            return _lazyAdapter;
        }
#else
        internal RefTypeSymbol AdaptedRefTypeSymbol => this;

        internal new RefTypeSymbol GetCciAdapter()
        {
            return this;
        }
#endif
    }

#if DEBUG
    internal partial class RefTypeSymbolAdapter
    {
        internal RefTypeSymbolAdapter(RefTypeSymbol underlyingRefTypeSymbol)
        {
            AdaptedRefTypeSymbol = underlyingRefTypeSymbol;
        }

        internal sealed override Symbol AdaptedSymbol => AdaptedRefTypeSymbol;
        internal RefTypeSymbol AdaptedRefTypeSymbol { get; }
    }
#endif
}
