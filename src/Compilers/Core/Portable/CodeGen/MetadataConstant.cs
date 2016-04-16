// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Reflection;

namespace Microsoft.CodeAnalysis.CodeGen
{
    internal sealed class MetadataConstant : Cci.IMetadataConstant
    {
        private readonly Cci.ITypeReference _type;
        private readonly object _value;

        public MetadataConstant(Cci.ITypeReference type, object value)
        {
            Debug.Assert(type != null);
            AssertValidConstant(value);

            _type = type;
            _value = value;
        }

        object Cci.IMetadataConstant.Value => _value;

        void Cci.IMetadataExpression.Dispatch(Cci.MetadataVisitor visitor)
        {
            visitor.Visit(this);
        }

        Cci.ITypeReference Cci.IMetadataExpression.Type => _type;

        [Conditional("DEBUG")]
        internal static void AssertValidConstant(object value)
        {
            Debug.Assert(value == null || value is string || value is DateTime || value is decimal || value.GetType().GetTypeInfo().IsEnum || (value.GetType().GetTypeInfo().IsPrimitive && !(value is IntPtr) && !(value is UIntPtr)));
        }
    }
}
