// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using BindingFlags = Microsoft.VisualStudio.Debugger.Metadata.BindingFlags;
using CustomAttributeData = Microsoft.VisualStudio.Debugger.Metadata.CustomAttributeData;
using FieldInfo = Microsoft.VisualStudio.Debugger.Metadata.FieldInfo;
using Type = Microsoft.VisualStudio.Debugger.Metadata.Type;
using TypeCode = Microsoft.VisualStudio.Debugger.Metadata.TypeCode;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator;

internal static class InlineArrayHelpers
{
    private const string InlineArrayAttributeName = "System.Runtime.CompilerServices.InlineArrayAttribute";
    private const string CompilerGeneratedAttributeName = "System.Runtime.CompilerServices.CompilerGeneratedAttribute";
    private const string UnsafeValueTypeAttributeName = "System.Runtime.CompilerServices.UnsafeValueTypeAttribute";

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

    public static bool TryGetFixedBufferInfo(Type type, out int arrayLength, [NotNullWhen(true)] out Type? elementType)
    {
        arrayLength = -1;
        elementType = null;

        // Fixed buffer types are compiler-generated and are nested within the struct that contains the fixed buffer field.
        // They are structurally identical to [InlineArray] structs in that they have 1 field defined in metadata which is repeated `arrayLength` times.
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

        if (!type.IsValueType || GetStructLayoutAttribute(type) is not { Value: LayoutKind.Sequential, Size: int explicitStructSize })
        {
            return false;
        }

        if (!type.Name.EndsWith(">e__FixedBuffer"))
        {
            return false;
        }

        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        if (fields.Length != 1)
        {
            return false;
        }

        bool isCompilerGenerated = false;
        bool isUnsafeValueType = false;
        foreach (var attribute in type.GetCustomAttributesData())
        {
            switch (attribute.Constructor.DeclaringType?.FullName)
            {
                case CompilerGeneratedAttributeName:
                    isCompilerGenerated = true;
                    break;
                case UnsafeValueTypeAttributeName:
                    isUnsafeValueType = true;
                    break;
                default:
                    break;
            }
        }

        if (!isCompilerGenerated || !isUnsafeValueType)
        {
            return false;
        }

        int elementSize = Type.GetTypeCode(fields[0].FieldType) switch
        {
            TypeCode.Boolean => sizeof(bool),
            TypeCode.Byte => sizeof(byte),
            TypeCode.SByte => sizeof(sbyte),
            TypeCode.UInt16 => sizeof(ushort),
            TypeCode.Int16 => sizeof(short),
            TypeCode.Char => sizeof(char),
            TypeCode.UInt32 => sizeof(uint),
            TypeCode.Int32 => sizeof(int),
            TypeCode.UInt64 => sizeof(ulong),
            TypeCode.Int64 => sizeof(long),
            TypeCode.Single => sizeof(float),
            TypeCode.Double => sizeof(double),
            _ => -1,
        };

        if (elementSize <= 0 || explicitStructSize % elementSize != 0)
        {
            return false;
        }

        elementType = fields[0].FieldType;
        arrayLength = explicitStructSize / elementSize;

        return arrayLength > 0 && elementType is not null;
    }

    // LMR Type defaults to throwing on access to StructLayoutAttribute and is not virtual.
    // This hack is a necessity to be able to test the use of StructLayoutAttribute with mocks derived from LMR Type.
    // n.b. [StructLayout] does not appear as an attribute in metadata; it is burned into the class layout.
    private static StructLayoutAttribute? GetStructLayoutAttribute(Type type)
    {
#if NETSTANDARD 
        // Retail, cannot see mock TypeImpl
        return type.StructLayoutAttribute;
#else
        return type switch
        {
            TypeImpl mockType => mockType.Type.StructLayoutAttribute,
            _ => type.StructLayoutAttribute
        };
#endif
    }
}
