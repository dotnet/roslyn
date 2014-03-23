// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Diagnostics;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    internal abstract class MethodReference : TypeMemberReference, Microsoft.Cci.IMethodReference
    {
        protected readonly MethodSymbol UnderlyingMethod;

        public MethodReference(MethodSymbol underlyingMethod)
        {
            Debug.Assert((object)underlyingMethod != null);

            this.UnderlyingMethod = underlyingMethod;
        }

        protected override Symbol UnderlyingSymbol
        {
            get
            {
                return UnderlyingMethod;
            }
        }

        bool Microsoft.Cci.IMethodReference.AcceptsExtraArguments
        {
            get
            {
                return UnderlyingMethod.IsVararg;
            }
        }

        ushort Microsoft.Cci.IMethodReference.GenericParameterCount
        {
            get
            {
                return (ushort)UnderlyingMethod.Arity;
            }
        }

        bool Microsoft.Cci.IMethodReference.IsGeneric
        {
            get
            {
                return UnderlyingMethod.IsGenericMethod;
            }
        }

        ushort Microsoft.Cci.ISignature.ParameterCount
        {
            get
            {
                return (ushort)UnderlyingMethod.ParameterCount;
            }
        }

        Microsoft.Cci.IMethodDefinition Microsoft.Cci.IMethodReference.GetResolvedMethod(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return null;
        }

        ImmutableArray<Microsoft.Cci.IParameterTypeInformation> Microsoft.Cci.IMethodReference.ExtraParameters
        {
            get
            {
                return ImmutableArray<Microsoft.Cci.IParameterTypeInformation>.Empty;
            }
        }

        Microsoft.Cci.CallingConvention Microsoft.Cci.ISignature.CallingConvention
        {
            get
            {
                return UnderlyingMethod.CallingConvention;
            }
        }

        ImmutableArray<Microsoft.Cci.IParameterTypeInformation> Microsoft.Cci.ISignature.GetParameters(Microsoft.CodeAnalysis.Emit.Context context)
        {
            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;
            return moduleBeingBuilt.Translate(UnderlyingMethod.Parameters);
        }

        IEnumerable<Microsoft.Cci.ICustomModifier> Microsoft.Cci.ISignature.ReturnValueCustomModifiers
        {
            get
            {
                return UnderlyingMethod.ReturnTypeCustomModifiers;
            }
        }

        bool Microsoft.Cci.ISignature.ReturnValueIsByRef
        {
            get
            {
                return UnderlyingMethod.ReturnType is ByRefReturnErrorTypeSymbol;
            }
        }

        bool Microsoft.Cci.ISignature.ReturnValueIsModified
        {
            get
            {
                return UnderlyingMethod.ReturnTypeCustomModifiers.Length != 0;
            }
        }

        Microsoft.Cci.ITypeReference Microsoft.Cci.ISignature.GetType(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return ((PEModuleBuilder)context.Module).Translate(UnderlyingMethod.ReturnType, syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNodeOpt, diagnostics: context.Diagnostics);
        }

        public virtual Microsoft.Cci.IGenericMethodInstanceReference AsGenericMethodInstanceReference
        {
            get
            {
                return null;
            }
        }

        public virtual Microsoft.Cci.ISpecializedMethodReference AsSpecializedMethodReference
        {
            get
            {
                return null;
            }
        }
    }
}
