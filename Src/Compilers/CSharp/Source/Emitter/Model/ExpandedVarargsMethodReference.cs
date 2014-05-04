// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    internal class ExpandedVarargsMethodReference :
        Cci.IMethodReference,
        Cci.IGenericMethodInstanceReference,
        Cci.ISpecializedMethodReference
    {
        private readonly Cci.IMethodReference underlyingMethod;
        private readonly ImmutableArray<Cci.IParameterTypeInformation> argListParams;

        public ExpandedVarargsMethodReference(Cci.IMethodReference underlyingMethod, ImmutableArray<Cci.IParameterTypeInformation> argListParams)
        {
            Debug.Assert(underlyingMethod.AcceptsExtraArguments);
            Debug.Assert(!argListParams.IsEmpty);

            this.underlyingMethod = underlyingMethod;
            this.argListParams = argListParams;
        }

        bool Cci.IMethodReference.AcceptsExtraArguments
        {
            get { return underlyingMethod.AcceptsExtraArguments; }
        }

        ushort Cci.IMethodReference.GenericParameterCount
        {
            get { return underlyingMethod.GenericParameterCount; }
        }

        bool Cci.IMethodReference.IsGeneric
        {
            get { return underlyingMethod.IsGeneric; }
        }

        Cci.IMethodDefinition Cci.IMethodReference.GetResolvedMethod(EmitContext context)
        {
            return underlyingMethod.GetResolvedMethod(context);
        }

        ImmutableArray<Cci.IParameterTypeInformation> Cci.IMethodReference.ExtraParameters
        {
            get
            {
                return argListParams;
            }
        }

        Cci.IGenericMethodInstanceReference Cci.IMethodReference.AsGenericMethodInstanceReference
        {
            get
            {
                if (underlyingMethod.AsGenericMethodInstanceReference == null)
                {
                    return null;
                }

                Debug.Assert(underlyingMethod.AsGenericMethodInstanceReference == underlyingMethod);
                return this;
            }
        }

        Cci.ISpecializedMethodReference Cci.IMethodReference.AsSpecializedMethodReference
        {
            get
            {
                if (underlyingMethod.AsSpecializedMethodReference == null)
                {
                    return null;
                }

                Debug.Assert(underlyingMethod.AsSpecializedMethodReference == underlyingMethod);
                return this;
            }
        }

        Cci.CallingConvention Cci.ISignature.CallingConvention
        {
            get { return underlyingMethod.CallingConvention; }
        }

        ushort Cci.ISignature.ParameterCount
        {
            get { return underlyingMethod.ParameterCount; }
        }

        ImmutableArray<Cci.IParameterTypeInformation> Cci.ISignature.GetParameters(EmitContext context)
        {
            return underlyingMethod.GetParameters(context);
        }

        ImmutableArray<Cci.ICustomModifier> Cci.ISignature.ReturnValueCustomModifiers
        {
            get { return underlyingMethod.ReturnValueCustomModifiers; }
        }

        bool Cci.ISignature.ReturnValueIsByRef
        {
            get { return underlyingMethod.ReturnValueIsByRef; }
        }

        Cci.ITypeReference Cci.ISignature.GetType(EmitContext context)
        {
            return underlyingMethod.GetType(context);
        }

        Cci.ITypeReference Cci.ITypeMemberReference.GetContainingType(EmitContext context)
        {
            return underlyingMethod.GetContainingType(context);
        }

        IEnumerable<Cci.ICustomAttribute> Cci.IReference.GetAttributes(EmitContext context)
        {
            return underlyingMethod.GetAttributes(context);
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

        string Cci.INamedEntity.Name
        {
            get { return underlyingMethod.Name; }
        }

        IEnumerable<Cci.ITypeReference> Cci.IGenericMethodInstanceReference.GetGenericArguments(EmitContext context)
        {
            return underlyingMethod.AsGenericMethodInstanceReference.GetGenericArguments(context);
        }

        Cci.IMethodReference Cci.IGenericMethodInstanceReference.GetGenericMethod(EmitContext context)
        {
            return new ExpandedVarargsMethodReference(underlyingMethod.AsGenericMethodInstanceReference.GetGenericMethod(context), argListParams);
        }

        Cci.IMethodReference Cci.ISpecializedMethodReference.UnspecializedVersion
        {
            get
            {
                return new ExpandedVarargsMethodReference(underlyingMethod.AsSpecializedMethodReference.UnspecializedVersion, argListParams);
            }
        }

        public override string ToString()
        {
            var result = PooledStringBuilder.GetInstance();
            Append(result, underlyingMethod);

            result.Builder.Append(" with __arglist( ");

            bool first = true;

            foreach (var p in argListParams)
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
            var symbol = value as ISymbol;

            if (symbol != null)
            {
                result.Builder.Append(symbol.ToDisplayString(SymbolDisplayFormat.ILVisualizationFormat));
            }
            else
            {
                result.Builder.Append(value);
            }
        }
    }
}