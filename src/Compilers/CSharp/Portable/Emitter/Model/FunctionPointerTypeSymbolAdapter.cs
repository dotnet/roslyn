// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#nullable enable

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
    internal sealed partial class FunctionPointerTypeSymbol : IFunctionPointerTypeReference
    {
        private FunctionPointerMethodSignature? _lazySignature;
        ISignature IFunctionPointerTypeReference.Signature
        {
            get
            {
                if (_lazySignature is null)
                {
                    Interlocked.CompareExchange(ref _lazySignature, new FunctionPointerMethodSignature(Signature), null);
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
        IEnumerable<ICustomAttribute> IReference.GetAttributes(EmitContext context) => SpecializedCollections.EmptyEnumerable<ICustomAttribute>();
        IDefinition? IReference.AsDefinition(EmitContext context) => null;

        /// <summary>
        /// We need to be able to differentiate between a FunctionPointer used as a type and a function pointer used
        /// as a StandaloneMethodSig. To do this, we wrap the <see cref="FunctionPointerMethodSymbol"/> in a
        /// <see cref="FunctionPointerMethodSignature"/>, to hide its implementation of <see cref="IMethodSymbol"/>.
        /// </summary>
        private sealed class FunctionPointerMethodSignature : ISignature, ISymbolCompareKindComparableInternal
        {
            private readonly FunctionPointerMethodSymbol _underlying;
            internal ISignature Underlying => _underlying;

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
                return obj is FunctionPointerMethodSignature { Underlying: var otherUnderlying } &&
                    Underlying.Equals(otherUnderlying);
            }

            bool ISymbolCompareKindComparableInternal.Equals(ISymbolCompareKindComparableInternal? other, TypeCompareKind compareKind)
            {
                return other is FunctionPointerMethodSignature otherSig && _underlying.Equals(otherSig._underlying, compareKind);
            }

            public override int GetHashCode() => Underlying.GetHashCode();

            public override string ToString() => _underlying.ToDisplayString(SymbolDisplayFormat.ILVisualizationFormat);
        }
    }
}
