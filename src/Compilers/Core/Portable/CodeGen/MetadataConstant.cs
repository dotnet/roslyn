// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
