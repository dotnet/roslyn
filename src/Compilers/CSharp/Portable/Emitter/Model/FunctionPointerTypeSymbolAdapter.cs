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
        /// We need to be able to differentiate between a FunctionPointer used a type and function pointer used
        /// as a StandaloneMethodSig. To do this, we wrap the <see cref="FunctionPointerMethodSymbol"/> in a
        /// <see cref="FunctionPointerMethodSignature"/>, to hide its implementation of <see cref="IMethodSymbol"/>.
        /// </summary>
        private sealed class FunctionPointerMethodSignature : ISignature
        {
            private readonly ISignature _underlying;

            internal FunctionPointerMethodSignature(ISignature underlying)
            {
                _underlying = underlying;
            }

            public CallingConvention CallingConvention => _underlying.CallingConvention;
            public ushort ParameterCount => _underlying.ParameterCount;
            public ImmutableArray<ICustomModifier> ReturnValueCustomModifiers => _underlying.ReturnValueCustomModifiers;
            public ImmutableArray<ICustomModifier> RefCustomModifiers => _underlying.RefCustomModifiers;
            public bool ReturnValueIsByRef => _underlying.ReturnValueIsByRef;
            public ImmutableArray<IParameterTypeInformation> GetParameters(EmitContext context)
                => _underlying.GetParameters(context);
            public ITypeReference GetType(EmitContext context) => _underlying.GetType(context);

            public override bool Equals(object obj)
            {
                return obj is FunctionPointerMethodSignature { _underlying: var otherUnderlying } &&
                    _underlying.Equals(otherUnderlying);
            }

            public override int GetHashCode() => _underlying.GetHashCode();
        }
    }
}
