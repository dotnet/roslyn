// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Diagnostics;
using System.Reflection;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGen
{
    internal sealed class MetadataConstant : Cci.IMetadataExpression
    {
        public Cci.ITypeReference Type { get; }
        public object? Value { get; }

        public MetadataConstant(Cci.ITypeReference type, object? value)
        {
            RoslynDebug.Assert(type != null);
            AssertValidConstant(value);

            Type = type;
            Value = value;
        }

        void Cci.IMetadataExpression.Dispatch(Cci.MetadataVisitor visitor)
        {
            visitor.Visit(this);
        }

        [Conditional("DEBUG")]
        internal static void AssertValidConstant(object? value)
        {
            Debug.Assert(value == null || value is string || value is DateTime || value is decimal || value.GetType().GetTypeInfo().IsEnum || (value.GetType().GetTypeInfo().IsPrimitive && !(value is IntPtr) && !(value is UIntPtr)));
        }
    }
}
