// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using BindingFlags = Microsoft.VisualStudio.Debugger.Metadata.BindingFlags;
using CustomAttributeData = Microsoft.VisualStudio.Debugger.Metadata.CustomAttributeData;
using FieldInfo = Microsoft.VisualStudio.Debugger.Metadata.FieldInfo;
using Type = Microsoft.VisualStudio.Debugger.Metadata.Type;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator;

internal static class InlineArrayHelpers
{
    private const string InlineArrayAttributeName = "System.Runtime.CompilerServices.InlineArrayAttribute";

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
            if (InlineArrayAttributeName.Equals(attribute.Constructor?.DeclaringType?.FullName))
            {
                var ctorParams = attribute.Constructor.GetParameters();
                if (ctorParams.Length == 1 && ctorParams[0].ParameterType.IsInt32() &&
                    attribute.ConstructorArguments.Count == 1 && attribute.ConstructorArguments[0].Value is int length)
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
