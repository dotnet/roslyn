// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Threading;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed partial class
#if DEBUG
        FunctionPointerTypeSymbolAdapter : SymbolAdapter,
#else
        FunctionPointerTypeSymbol :
#endif 
        IFunctionPointerTypeReference
    {
        private FunctionPointerMethodSignature? _lazySignature;
        ISignature IFunctionPointerTypeReference.Signature
        {
            get
            {
                if (_lazySignature is null)
                {
                    Interlocked.CompareExchange(ref _lazySignature, new FunctionPointerMethodSignature(AdaptedFunctionPointerTypeSymbol.Signature), null);
                }

                return _lazySignature;
            }
        }
        void IReference.Dispatch(MetadataVisitor visitor) => visitor.Visit((IFunctionPointerTypeReference)this);

        bool ITypeReference.IsEnum => false;
        Cci.PrimitiveTypeCode ITypeReference.TypeCode => Cci.PrimitiveTypeCode.FunctionPointer;
        TypeDefinitionHandle ITypeReference.TypeDef => default;
        IGenericMethodParameterReference? ITypeReference.AsGenericMethodParameterReference => null;
        IGenericTypeInstanceReference? ITypeReference.AsGenericTypeInstanceReference => null;
        IGenericTypeParameterReference? ITypeReference.AsGenericTypeParameterReference => null;
        INamespaceTypeReference? ITypeReference.AsNamespaceTypeReference => null;
        INestedTypeReference? ITypeReference.AsNestedTypeReference => null;
        ISpecializedNestedTypeReference? ITypeReference.AsSpecializedNestedTypeReference => null;
        INamespaceTypeDefinition? ITypeReference.AsNamespaceTypeDefinition(EmitContext context) => null;
        INestedTypeDefinition? ITypeReference.AsNestedTypeDefinition(EmitContext context) => null;
        ITypeDefinition? ITypeReference.AsTypeDefinition(EmitContext context) => null;
        ITypeDefinition? ITypeReference.GetResolvedType(EmitContext context) => null;
        bool ITypeReference.IsValueType => AdaptedFunctionPointerTypeSymbol.IsValueType;

        IEnumerable<ICustomAttribute> IReference.GetAttributes(EmitContext context) => SpecializedCollections.EmptyEnumerable<ICustomAttribute>();
        IDefinition? IReference.AsDefinition(EmitContext context) => null;

        /// <summary>
        /// We need to be able to differentiate between a FunctionPointer used as a type and a function pointer used
        /// as a StandaloneMethodSig. To do this, we wrap the <see cref="FunctionPointerMethodSymbol"/> in a
        /// <see cref="FunctionPointerMethodSignature"/>, to hide its implementation of <see cref="IMethodSymbol"/>.
        /// </summary>
        private sealed class FunctionPointerMethodSignature : ISignature
        {
            private readonly FunctionPointerMethodSymbol _underlying;
            internal ISignature Underlying => _underlying.GetCciAdapter();

            internal FunctionPointerMethodSignature(FunctionPointerMethodSymbol underlying)
            {
                _underlying = underlying;
            }

            public CallingConvention CallingConvention => Underlying.CallingConvention;
            public ushort ParameterCount => Underlying.ParameterCount;
            public ImmutableArray<ICustomModifier> ReturnValueCustomModifiers => Underlying.ReturnValueCustomModifiers;
            public ImmutableArray<ICustomModifier> RefCustomModifiers => Underlying.RefCustomModifiers;
            public bool ReturnValueIsByRef => Underlying.ReturnValueIsByRef;

            public ImmutableArray<IParameterTypeInformation> GetParameters(EmitContext context)
                => Underlying.GetParameters(context);
            public ITypeReference GetType(EmitContext context) => Underlying.GetType(context);

            public override bool Equals(object? obj)
            {
                // It is not supported to rely on default equality of these Cci objects, an explicit way to compare and hash them should be used.
                throw ExceptionUtilities.Unreachable();
            }

            // It is not supported to rely on default equality of these Cci objects, an explicit way to compare and hash them should be used.
            public override int GetHashCode() => throw ExceptionUtilities.Unreachable();

            public override string ToString() => _underlying.ToDisplayString(SymbolDisplayFormat.ILVisualizationFormat);
        }
    }

    internal partial class FunctionPointerTypeSymbol
    {
#if DEBUG
        private FunctionPointerTypeSymbolAdapter? _lazyAdapter;

        protected sealed override SymbolAdapter GetCciAdapterImpl() => GetCciAdapter();

        internal new FunctionPointerTypeSymbolAdapter GetCciAdapter()
        {
            if (_lazyAdapter is null)
            {
                return InterlockedOperations.Initialize(ref _lazyAdapter, new FunctionPointerTypeSymbolAdapter(this));
            }

            return _lazyAdapter;
        }
#else
        internal FunctionPointerTypeSymbol AdaptedFunctionPointerTypeSymbol => this;

        internal new FunctionPointerTypeSymbol GetCciAdapter()
        {
            return this;
        }
#endif 
    }

#if DEBUG
    internal partial class FunctionPointerTypeSymbolAdapter
    {
        internal FunctionPointerTypeSymbolAdapter(FunctionPointerTypeSymbol underlyingFunctionPointerTypeSymbol)
        {
            AdaptedFunctionPointerTypeSymbol = underlyingFunctionPointerTypeSymbol;
        }

        internal sealed override Symbol AdaptedSymbol => AdaptedFunctionPointerTypeSymbol;
        internal FunctionPointerTypeSymbol AdaptedFunctionPointerTypeSymbol { get; }
    }
#endif
}
