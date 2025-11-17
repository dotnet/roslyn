// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using BindingFlags = Microsoft.VisualStudio.Debugger.Metadata.BindingFlags;
using FieldInfo = Microsoft.VisualStudio.Debugger.Metadata.FieldInfo;
using CustomAttributeData = Microsoft.VisualStudio.Debugger.Metadata.CustomAttributeData;
using Type = Microsoft.VisualStudio.Debugger.Metadata.Type;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator;

internal static class InlineArrayHelpers
{
    private const string InlineArrayAttributeName = "System.Runtime.CompilerServices.InlineArrayAttribute";

    public static bool IsInlineArray(Type t)
        => t.IsValueType && t.GetCustomAttributesData().Any(static a => a.Constructor?.DeclaringType?.FullName == InlineArrayAttributeName);

    public static bool TryGetInlineArrayInfo(Type t, out int arrayLength, [NotNullWhen(true)] out Type? tElementType)
    {
        arrayLength = -1;
        tElementType = null;

        if (!t.IsValueType)
        {
            return false;
        }

        IList<CustomAttributeData> customAttributes = t.GetCustomAttributesData();
        foreach (var attribute in customAttributes)
        {
            if (attribute.Constructor?.DeclaringType?.FullName?.Equals(InlineArrayAttributeName, StringComparison.Ordinal) == true)
            {
                if (attribute.ConstructorArguments.Count == 1 && attribute.ConstructorArguments[0].Value is int length)
                {
                    arrayLength = length;
                }
            }
        }

        // Inline arrays must have length > 0
        if (arrayLength <= 0)
        {
            return false;
        }

        FieldInfo[] fields = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        if (fields.Length == 1)
        {
            tElementType = fields[0].FieldType;
        }
        else
        {
            // Inline arrays must have exactly one field
            return false;
        }

        return true;
    }
}
