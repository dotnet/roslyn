// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis
{
    [Flags]
    internal enum ValueUsageInfo
    {
        /// <summary>
        /// Represents default value indicating no usage.
        /// </summary>
        None = 0x0,

        /// <summary>
        /// Represents a value read.
        /// For example, reading the value of a local/field/parameter.
        /// </summary>
        ValueRead = 0x1,

        /// <summary>
        /// Represents a value write.
        /// For example, assigning a value to a local/field/parameter.
        /// </summary>
        ValueWrite = 0x2,

        /// <summary>
        /// Represents a reference being taken for the symbol.
        /// For example, passing an argument to an "in", "ref" or "out" parameter.
        /// </summary>
        LocationReference = 0x4,

        /// <summary>
        /// Represents a name-only reference that neither reads nor writes the underlying value.
        /// For example, 'nameof(x)' or reference to a symbol 'x' in a documentation comment
        /// does not read or write the underlying value stored in 'x'.
        /// </summary>
        Name = 0x8,

        /// <summary>
        /// Represents a reference to a namespace or type on the left side of a dotted name (qualified name or member access).
        /// For example, 'NS' in <code>NS.Type x = new NS.Type();</code> or <code>NS.Type.StaticMethod();</code> or 
        /// 'Type' in <code>Type.NestedType x = new Type.NestedType();</code> or <code>Type.StaticMethod();</code>
        /// </summary>
        DottedName = 0x10,

        /// <summary>
        /// Represents a generic type argument reference.
        /// For example, 'Type' in <code>Generic{Type} x = ...;</code> or <code>class Derived : Base{Type} { }</code>
        /// </summary>
        GenericTypeArgument = 0x20,

        /// <summary>
        /// Represents a base type or interface reference in the base list of a named type.
        /// For example, 'Base' in <code>class Derived : Base { }</code>.
        /// </summary>
        BaseTypeOrInterface = 0x40,

        /// <summary>
        /// Represents a reference to a type whose instance is being created.
        /// For example, 'C' in <code>var x = new C();</code>, where 'C' is a named type.
        /// </summary>
        ObjectCreation = 0x80,

        /// <summary>
        /// Represents a reference to a namespace or type within a using or imports directive.
        /// For example, <code>using NS;</code> or <code>using static NS.Extensions</code> or <code>using Alias = MyType</code>.
        /// </summary>
        NamespaceOrTypeInUsing = 0x100,

        /// <summary>
        /// Represents a reference to a namespace name in a namespace declaration context.
        /// For example, 'N1' or <code>namespaces N1.N2 { }</code>.
        /// </summary>
        NamespaceDeclaration = 0x200,

        /// <summary>
        /// Represents a value read and/or write.
        /// For example, an increment or compound assignment operation.
        /// </summary>
        ValueReadWrite = ValueRead | ValueWrite,

        /// <summary>
        /// Represents a readable reference being taken to the value.
        /// For example, passing an argument to an "in" or "ref readonly" parameter.
        /// </summary>
        ValueReadableReference = ValueRead | LocationReference,

        /// <summary>
        /// Represents a readable reference being taken to the value.
        /// For example, passing an argument to an "out" parameter.
        /// </summary>
        ValueWritableReference = ValueWrite | LocationReference,

        /// <summary>
        /// Represents a value read or write.
        /// For example, passing an argument to a "ref" parameter.
        /// </summary>
        ValueReadableWritableReference = ValueRead | ValueWrite | LocationReference
    }

    internal static class ValueUsageInfoExtensions
    {
        public static bool IsReadFrom(this ValueUsageInfo valueUsageInfo)
            => (valueUsageInfo & ValueUsageInfo.ValueRead) != 0;

        public static bool IsWrittenTo(this ValueUsageInfo valueUsageInfo)
            => (valueUsageInfo & ValueUsageInfo.ValueWrite) != 0;

        public static bool IsNameOnly(this ValueUsageInfo valueUsageInfo)
            => (valueUsageInfo & ValueUsageInfo.Name) != 0;

        public static string ToLocalizableString(this ValueUsageInfo value)
        {
            // We don't support localizing value combinations.
            Debug.Assert(value.IsSingleBitSet());

            switch (value)
            {
                case ValueUsageInfo.ValueRead:
                    return WorkspacesResources.ValueUsageInfo_ValueRead;

                case ValueUsageInfo.ValueWrite:
                    return WorkspacesResources.ValueUsageInfo_ValueWrite;

                case ValueUsageInfo.LocationReference:
                    return WorkspacesResources.ValueUsageInfo_ValueReference;

                case ValueUsageInfo.Name:
                    return WorkspacesResources.ValueUsageInfo_Name;

                case ValueUsageInfo.DottedName:
                    return WorkspacesResources.ValueUsageInfo_DottedName;

                case ValueUsageInfo.GenericTypeArgument:
                    return WorkspacesResources.ValueUsageInfo_GenericTypeArgument;

                case ValueUsageInfo.BaseTypeOrInterface:
                    return WorkspacesResources.ValueUsageInfo_BaseTypeOrInterface;

                case ValueUsageInfo.ObjectCreation:
                    return WorkspacesResources.ValueUsageInfo_ObjectCreation;

                case ValueUsageInfo.NamespaceOrTypeInUsing:
                    return WorkspacesResources.ValueUsageInfo_NamespaceOrTypeInUsing;

                case ValueUsageInfo.NamespaceDeclaration:
                    return WorkspacesResources.ValueUsageInfo_NamespaceDeclaration;

                default:
                    Debug.Fail($"Unhandled value: '{value.ToString()}'");
                    return value.ToString();
            }
        }

        public static bool IsSingleBitSet(this ValueUsageInfo valueUsageInfo)
            => valueUsageInfo != ValueUsageInfo.None && (valueUsageInfo & (valueUsageInfo - 1)) == 0;

        public static ImmutableArray<string> ToLocalizableValues(this ValueUsageInfo valueUsageInfo)
        {
            if (valueUsageInfo == ValueUsageInfo.None)
            {
                return ImmutableArray<string>.Empty;
            }

            var builder = ArrayBuilder<string>.GetInstance();
            foreach (ValueUsageInfo value in Enum.GetValues(typeof(ValueUsageInfo)))
            {
                if (value.IsSingleBitSet() && (valueUsageInfo & value) != 0)
                {
                    builder.Add(value.ToLocalizableString());
                }
            }

            return builder.ToImmutableAndFree();
        }
    }
}
