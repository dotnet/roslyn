// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using BindingFlags = Microsoft.VisualStudio.Debugger.Metadata.BindingFlags;
using FieldInfo = Microsoft.VisualStudio.Debugger.Metadata.FieldInfo;
using Type = Microsoft.VisualStudio.Debugger.Metadata.Type;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator;

internal static class InlineArrayHelpers
{
    private const string InlineArrayAttributeName = "System.Runtime.CompilerServices.InlineArrayAttribute";
    private const string FixedBufferAttributeName = "System.Runtime.CompilerServices.FixedBufferAttribute";

    public static bool TryGetInlineArrayInfo(Type type, out int arrayLength, [NotNullWhen(true)] out Type? elementType)
    {
        arrayLength = -1;
        elementType = null;

        if (!type.IsValueType)
        {
            return false;
        }

        foreach (var attribute in type.GetCustomAttributesData())
        {
            if (InlineArrayAttributeName.Equals(attribute.Constructor?.DeclaringType?.FullName))
            {
                var ctorParams = attribute.Constructor.GetParameters();
                if (ctorParams.Length == 1 && ctorParams[0].ParameterType.IsInt32() &&
                    attribute.ConstructorArguments.Count == 1 && attribute.ConstructorArguments[0].Value is int ctorLengthArg)
                {
                    arrayLength = ctorLengthArg;
                }
            }
        }

        // Inline arrays must have length > 0
        if (arrayLength <= 0)
        {
            return false;
        }

        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        if (fields.Length == 1)
        {
            elementType = fields[0].FieldType;
        }
        else
        {
            // Inline arrays must have exactly one field
            return false;
        }

        return true;
    }

    public static bool TryGetFixedBufferInfo(Type type, out int arrayLength, [NotNullWhen(true)] out Type? elementType)
    {
        arrayLength = -1;
        elementType = null;

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
        if (!type.IsValueType || !type.IsLayoutSequential || type.IsGenericType)
        {
            return false;
        }

        if (!type.IsNested || type.DeclaringType is not Type enclosingType)
        {
            return false;
        }

        FieldInfo[] fields = enclosingType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        foreach (FieldInfo field in fields)
        {
            // Match the field whose type is the fixed buffer type and is decorated with [FixedBuffer(Type, int)]
            if (field.FieldType.Equals(type))
            {
                foreach (var attribute in field.GetCustomAttributesData())
                {
                    if (FixedBufferAttributeName.Equals(attribute.Constructor?.DeclaringType?.FullName))
                    {
                        var ctorParams = attribute.Constructor.GetParameters();
                        if (ctorParams.Length == 2 &&
                            ctorParams[0].ParameterType.IsReflectionType() &&
                            ctorParams[1].ParameterType.IsInt32() &&
                            attribute.ConstructorArguments.Count == 2 &&
                            attribute.ConstructorArguments[0].Value is Type ctorElementTypeArg &&
                            attribute.ConstructorArguments[1].Value is int ctorLengthArg)
                        {
                            elementType = ctorElementTypeArg;
                            arrayLength = ctorLengthArg;
                        }

                        break;
                    }
                }

                // There should only be one field matching this type if it is indeed a fixed buffer - in any case, don't bother checking more fields
                break;
            }
        }

        return arrayLength > 0 && elementType is not null;
    }
}
