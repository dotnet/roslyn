﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeGen;
using EmitContext = Microsoft.CodeAnalysis.Emit.EmitContext;

namespace Microsoft.Cci
{
    internal class ReturnValueParameter : IParameterDefinition
    {
        internal ReturnValueParameter(IMethodDefinition containingMethod)
        {
            _containingMethod = containingMethod;
        }

        public IEnumerable<ICustomAttribute> GetAttributes(EmitContext context)
        {
            return _containingMethod.ReturnValueAttributes;
        }

        public ISignature ContainingSignature
        {
            get { return _containingMethod; }
        }

        private readonly IMethodDefinition _containingMethod;

        public MetadataConstant Constant
        {
            get { return null; }
        }

        public ImmutableArray<Cci.ICustomModifier> RefCustomModifiers
        {
            get { return _containingMethod.RefCustomModifiers; }
        }

        public ImmutableArray<Cci.ICustomModifier> CustomModifiers
        {
            get { return _containingMethod.ReturnValueCustomModifiers; }
        }

        public MetadataConstant GetDefaultValue(EmitContext context)
        {
            return null;
        }

        public void Dispatch(MetadataVisitor visitor)
        {
        }

        public bool HasDefaultValue
        {
            get { return false; }
        }

        public ushort Index
        {
            get { return 0; }
        }

        public bool IsIn
        {
            get { return false; }
        }

        public bool IsByReference
        {
            get { return _containingMethod.ReturnValueIsByRef; }
        }

        public bool IsMarshalledExplicitly
        {
            get { return _containingMethod.ReturnValueIsMarshalledExplicitly; }
        }

        public bool IsOptional
        {
            get { return false; }
        }

        public bool IsOut
        {
            get { return false; }
        }

        public IMarshallingInformation MarshallingInformation
        {
            get { return _containingMethod.ReturnValueMarshallingInformation; }
        }

        public ImmutableArray<byte> MarshallingDescriptor
        {
            get { return _containingMethod.ReturnValueMarshallingDescriptor; }
        }

        public string Name
        {
            get { return string.Empty; }
        }

        public ITypeReference GetType(EmitContext context)
        {
            return _containingMethod.GetType(context);
        }

        public IDefinition AsDefinition(EmitContext context)
        {
            return this as IDefinition;
        }
    }
}
