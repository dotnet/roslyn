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
    private const string FixedBufferAttributeName = "System.Runtime.CompilerServices.FixedBufferAttribute";

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

    public static bool TryGetFixedBufferInfo(Type t, out int arrayLength, [NotNullWhen(true)] out Type? tElementType)
    {
        arrayLength = -1;
        tElementType = null;

        // Fixed buffer types are compiler-generated and are nested within the struct that contains the fixed buffer field.
        // They are structurally identical to [InlineArray] structs in that they have 1 field defined in metadata which is repeated `arrayLength` times.
        // The main difference is that the attribute is applied to the generated field and not the type itself, so we have to look a little harder to find it.
        // 
        // Example:
        // internal unsafe struct Buffer
        // {
        //     public fixed char fixedBuffer[128];
        // }
        //
        // Compiles into:
        //
        // internal struct Buffer
        // {
        //     [StructLayout(LayoutKind.Sequential, Size = 256)]
        //     [CompilerGenerated]
        //     [UnsafeValueType]
        //     public struct <fixedBuffer>e__FixedBuffer
        //     {
        //         public char FixedElementField;
        //     }

        //     [FixedBuffer(typeof(char), 128)]
        //     public <fixedBuffer>e__FixedBuffer fixedBuffer;
        // }

        // Filter out shapes we know can't be fixed buffer types
        if (!t.IsValueType || !t.IsLayoutSequential || t.IsGenericType)
        {
            return false;
        }

        if (!t.IsNested || t.DeclaringType is not Type enclosingType)
        {
            return false;
        }

        FieldInfo[] fields = enclosingType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        foreach (FieldInfo field in fields)
        {
            // Match the field whose type is the fixed buffer type and is decorated with [FixedBuffer(Type, int)]
            if (field.FieldType.Equals(t))
            {
                IList<CustomAttributeData> customAttributes = field.GetCustomAttributesData();
                foreach (var attribute in customAttributes)
                {
                    if (FixedBufferAttributeName.Equals(attribute.Constructor?.DeclaringType?.FullName))
                    {
                        var ctorParams = attribute.Constructor.GetParameters();
                        if (ctorParams.Length == 2 &&
                            ctorParams[0].ParameterType.IsReflectionType() &&
                            ctorParams[1].ParameterType.IsInt32() &&
                            attribute.ConstructorArguments.Count == 2 &&
                            attribute.ConstructorArguments[0].Value is Type type &&
                            attribute.ConstructorArguments[1].Value is int length)
                        {
                            tElementType = type;
                            arrayLength = length;
                        }

                        break;
                    }
                }

                // There should only be one field matching this type if it is indeed a fixed buffer - in any case, don't bother checking more fields
                break;
            }
        }

        return arrayLength > 0 && tElementType is not null;
    }
}
