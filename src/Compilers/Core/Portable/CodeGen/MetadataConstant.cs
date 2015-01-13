// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Reflection;

namespace Microsoft.CodeAnalysis.CodeGen
{
    internal sealed class MetadataConstant : Cci.IMetadataConstant
    {
        private readonly Cci.ITypeReference type;
        private readonly object value;

        public MetadataConstant(Cci.ITypeReference type, object value)
        {
            Debug.Assert(type != null);
            AssertValidConstant(value);

            this.type = type;
            this.value = value;
        }

        object Cci.IMetadataConstant.Value
        {
            get { return this.value; }
        }

        void Cci.IMetadataExpression.Dispatch(Cci.MetadataVisitor visitor)
        {
            visitor.Visit(this);
        }

        Cci.ITypeReference Cci.IMetadataExpression.Type
        {
            get { return this.type; }
        }

        [Conditional("DEBUG")]
        internal static void AssertValidConstant(object value)
        {
            Debug.Assert(value == null || value is string || value is DateTime || value is decimal || value.GetType().GetTypeInfo().IsEnum || (value.GetType().GetTypeInfo().IsPrimitive && !(value is IntPtr) && !(value is UIntPtr)));
        }
    }
}
