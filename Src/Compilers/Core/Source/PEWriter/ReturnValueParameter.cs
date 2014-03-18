// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Microsoft.Cci
{
    internal class ReturnValueParameter : IParameterDefinition
    {
        internal ReturnValueParameter(IMethodDefinition containingMethod)
        {
            this.containingMethod = containingMethod;
        }

        public IEnumerable<ICustomAttribute> GetAttributes(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return this.containingMethod.ReturnValueAttributes;
        }

        public ISignature ContainingSignature
        {
            get { return this.containingMethod; }
        }

        private IMethodDefinition containingMethod;

        public IMetadataConstant Constant
        {
            get { return null; }
        }

        public IEnumerable<ICustomModifier> CustomModifiers
        {
            get { return this.containingMethod.ReturnValueCustomModifiers; }
        }

        public IMetadataConstant GetDefaultValue(Microsoft.CodeAnalysis.Emit.Context context)
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
            get { return this.containingMethod.ReturnValueIsByRef; }
        }

        public bool HasByRefBeforeCustomModifiers
        {
            get { return false; }
        }

        public bool IsModified
        {
            get { return this.containingMethod.ReturnValueIsModified; }
        }

        public bool IsMarshalledExplicitly
        {
            get { return this.containingMethod.ReturnValueIsMarshalledExplicitly; }
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
            get { return this.containingMethod.ReturnValueMarshallingInformation; }
        }

        public ImmutableArray<byte> MarshallingDescriptor
        {
            get { return this.containingMethod.ReturnValueMarshallingDescriptor; }
        }

        public string Name
        {
            get { return string.Empty; }
        }

        public ITypeReference GetType(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return this.containingMethod.GetType(context);
        }

        public IDefinition AsDefinition(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return this as IDefinition;
        }
    }
}