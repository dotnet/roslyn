// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    internal sealed class ExpandedVarargsMethodReference :
        Cci.IMethodReference,
        Cci.IGenericMethodInstanceReference,
        Cci.ISpecializedMethodReference
    {
        private readonly Cci.IMethodReference _underlyingMethod;
        private readonly ImmutableArray<Cci.IParameterTypeInformation> _argListParams;

        public ExpandedVarargsMethodReference(Cci.IMethodReference underlyingMethod, ImmutableArray<Cci.IParameterTypeInformation> argListParams)
        {
            Debug.Assert(underlyingMethod.AcceptsExtraArguments);
            Debug.Assert(!argListParams.IsEmpty);

            _underlyingMethod = underlyingMethod;
            _argListParams = argListParams;
        }

        bool Cci.IMethodReference.AcceptsExtraArguments
        {
            get { return _underlyingMethod.AcceptsExtraArguments; }
        }

        ushort Cci.IMethodReference.GenericParameterCount
        {
            get { return _underlyingMethod.GenericParameterCount; }
        }

        Cci.IMethodDefinition Cci.IMethodReference.GetResolvedMethod(EmitContext context)
        {
            return _underlyingMethod.GetResolvedMethod(context);
        }

        ImmutableArray<Cci.IParameterTypeInformation> Cci.IMethodReference.ExtraParameters
        {
            get
            {
                return _argListParams;
            }
        }

        Cci.IGenericMethodInstanceReference Cci.IMethodReference.AsGenericMethodInstanceReference
        {
            get
            {
                if (_underlyingMethod.AsGenericMethodInstanceReference == null)
                {
                    return null;
                }

                Debug.Assert(_underlyingMethod.AsGenericMethodInstanceReference == _underlyingMethod);
                return this;
            }
        }

        Cci.ISpecializedMethodReference Cci.IMethodReference.AsSpecializedMethodReference
        {
            get
            {
                if (_underlyingMethod.AsSpecializedMethodReference == null)
                {
                    return null;
                }

                Debug.Assert(_underlyingMethod.AsSpecializedMethodReference == _underlyingMethod);
                return this;
            }
        }

        Cci.CallingConvention Cci.ISignature.CallingConvention
        {
            get { return _underlyingMethod.CallingConvention; }
        }

        ushort Cci.ISignature.ParameterCount
        {
            get { return _underlyingMethod.ParameterCount; }
        }

        ImmutableArray<Cci.IParameterTypeInformation> Cci.ISignature.GetParameters(EmitContext context)
        {
            return _underlyingMethod.GetParameters(context);
        }

        ImmutableArray<Cci.ICustomModifier> Cci.ISignature.ReturnValueCustomModifiers
        {
            get { return _underlyingMethod.ReturnValueCustomModifiers; }
        }

        ImmutableArray<Cci.ICustomModifier> Cci.ISignature.RefCustomModifiers
        {
            get { return _underlyingMethod.RefCustomModifiers; }
        }

        bool Cci.ISignature.ReturnValueIsByRef
        {
            get { return _underlyingMethod.ReturnValueIsByRef; }
        }

        Cci.ITypeReference Cci.ISignature.GetType(EmitContext context)
        {
            return _underlyingMethod.GetType(context);
        }

        Cci.ITypeReference Cci.ITypeMemberReference.GetContainingType(EmitContext context)
        {
            return _underlyingMethod.GetContainingType(context);
        }

        IEnumerable<Cci.ICustomAttribute> Cci.IReference.GetAttributes(EmitContext context)
        {
            return _underlyingMethod.GetAttributes(context);
        }

        void Cci.IReference.Dispatch(Cci.MetadataVisitor visitor)
        {
            if (((Cci.IMethodReference)this).AsGenericMethodInstanceReference != null)
            {
                visitor.Visit((Cci.IGenericMethodInstanceReference)this);
            }
            else if (((Cci.IMethodReference)this).AsSpecializedMethodReference != null)
            {
                visitor.Visit((Cci.ISpecializedMethodReference)this);
            }
            else
            {
                visitor.Visit((Cci.IMethodReference)this);
            }
        }

        Cci.IDefinition Cci.IReference.AsDefinition(EmitContext context)
        {
            return null;
        }

        CodeAnalysis.Symbols.ISymbolInternal Cci.IReference.GetInternalSymbol() => null;

        string Cci.INamedEntity.Name
        {
            get { return _underlyingMethod.Name; }
        }

        IEnumerable<Cci.ITypeReference> Cci.IGenericMethodInstanceReference.GetGenericArguments(EmitContext context)
        {
            return _underlyingMethod.AsGenericMethodInstanceReference.GetGenericArguments(context);
        }

        Cci.IMethodReference Cci.IGenericMethodInstanceReference.GetGenericMethod(EmitContext context)
        {
            return new ExpandedVarargsMethodReference(_underlyingMethod.AsGenericMethodInstanceReference.GetGenericMethod(context), _argListParams);
        }

        Cci.IMethodReference Cci.ISpecializedMethodReference.UnspecializedVersion
        {
            get
            {
                return new ExpandedVarargsMethodReference(_underlyingMethod.AsSpecializedMethodReference.UnspecializedVersion, _argListParams);
            }
        }

        public override string ToString()
        {
            var result = PooledStringBuilder.GetInstance();
            Append(result, _underlyingMethod.GetInternalSymbol() ?? (object)_underlyingMethod);

            result.Builder.Append(" with __arglist( ");

            bool first = true;

            foreach (var p in _argListParams)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    result.Builder.Append(", ");
                }

                if (p.IsByReference)
                {
                    result.Builder.Append("ref ");
                }

                Append(result, p.GetType(new EmitContext()));
            }

            result.Builder.Append(")");

            return result.ToStringAndFree();
        }

        private static void Append(PooledStringBuilder result, object value)
        {
            Debug.Assert(!(value is ISymbol));

            var symbol = (value as ISymbolInternal)?.GetISymbol();

            if (symbol != null)
            {
                result.Builder.Append(symbol.ToDisplayString(SymbolDisplayFormat.ILVisualizationFormat));
            }
            else
            {
                result.Builder.Append(value);
            }
        }

        public sealed override bool Equals(object obj)
        {
            // It is not supported to rely on default equality of these Cci objects, an explicit way to compare and hash them should be used.
            throw Roslyn.Utilities.ExceptionUtilities.Unreachable();
        }

        public sealed override int GetHashCode()
        {
            // It is not supported to rely on default equality of these Cci objects, an explicit way to compare and hash them should be used.
            throw Roslyn.Utilities.ExceptionUtilities.Unreachable();
        }
    }
}
