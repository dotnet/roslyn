// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Roslyn.Utilities;
using System.Reflection.Metadata;

namespace Microsoft.CodeAnalysis
{
    internal static class MetadataTypeCodeExtensions
    {
        internal static SpecialType ToSpecialType(this SignatureTypeCode typeCode)
        {
            switch (typeCode)
            {
                case SignatureTypeCode.TypedReference:
                    return SpecialType.System_TypedReference;

                case SignatureTypeCode.Void:
                    return SpecialType.System_Void;

                case SignatureTypeCode.Boolean:
                    return SpecialType.System_Boolean;

                case SignatureTypeCode.SByte:
                    return SpecialType.System_SByte;

                case SignatureTypeCode.Byte:
                    return SpecialType.System_Byte;

                case SignatureTypeCode.Int16:
                    return SpecialType.System_Int16;

                case SignatureTypeCode.UInt16:
                    return SpecialType.System_UInt16;

                case SignatureTypeCode.Int32:
                    return SpecialType.System_Int32;

                case SignatureTypeCode.UInt32:
                    return SpecialType.System_UInt32;

                case SignatureTypeCode.Int64:
                    return SpecialType.System_Int64;

                case SignatureTypeCode.UInt64:
                    return SpecialType.System_UInt64;

                case SignatureTypeCode.Single:
                    return SpecialType.System_Single;

                case SignatureTypeCode.Double:
                    return SpecialType.System_Double;

                case SignatureTypeCode.Char:
                    return SpecialType.System_Char;

                case SignatureTypeCode.String:
                    return SpecialType.System_String;

                case SignatureTypeCode.IntPtr:
                    return SpecialType.System_IntPtr;

                case SignatureTypeCode.UIntPtr:
                    return SpecialType.System_UIntPtr;

                case SignatureTypeCode.Object:
                    return SpecialType.System_Object;

                default:
                    throw ExceptionUtilities.UnexpectedValue(typeCode);
            }
        }

        internal static bool HasShortFormSignatureEncoding(this SpecialType type)
        {
            // Spec II.23.2.16: Short form signatures:
            // The following table shows which short-forms should be used in place of each long-form item. 
            // Long Form                             Short Form                     
            //   CLASS     System.String               ELEMENT_TYPE_STRING
            //   CLASS     System.Object               ELEMENT_TYPE_OBJECT
            //   VALUETYPE System.Void                 ELEMENT_TYPE_VOID
            //   VALUETYPE System.Boolean              ELEMENT_TYPE_BOOLEAN
            //   VALUETYPE System.Char                 ELEMENT_TYPE_CHAR
            //   VALUETYPE System.Byte                 ELEMENT_TYPE_U1
            //   VALUETYPE System.Sbyte                ELEMENT_TYPE_I1
            //   VALUETYPE System.Int16                ELEMENT_TYPE_I2
            //   VALUETYPE System.UInt16               ELEMENT_TYPE_U2
            //   VALUETYPE System.Int32                ELEMENT_TYPE_I4
            //   VALUETYPE System.UInt32               ELEMENT_TYPE_U4
            //   VALUETYPE System.Int64                ELEMENT_TYPE_I8
            //   VALUETYPE System.UInt64               ELEMENT_TYPE_U8
            //   VALUETYPE System.IntPtr               ELEMENT_TYPE_I
            //   VALUETYPE System.UIntPtr              ELEMENT_TYPE_U
            //   VALUETYPE System.TypedReference       ELEMENT_TYPE_TYPEDBYREF

            // The spec is missing:
            //   VALUETYPE System.Single               ELEMENT_TYPE_R4
            //   VALUETYPE System.Double               ELEMENT_TYPE_R8

            switch (type)
            {
                case SpecialType.System_String:
                case SpecialType.System_Object:
                case SpecialType.System_Void:
                case SpecialType.System_Boolean:
                case SpecialType.System_Char:
                case SpecialType.System_Byte:
                case SpecialType.System_SByte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_IntPtr:
                case SpecialType.System_UIntPtr:
                case SpecialType.System_TypedReference:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                    return true;
            }

            return false;
        }

        internal static SerializationTypeCode ToSerializationType(this SpecialType specialType)
        {
            var result = ToSerializationTypeOrInvalid(specialType);

            if (result == SerializationTypeCode.Invalid)
            {
                throw ExceptionUtilities.UnexpectedValue(specialType);
            }

            return result;
        }

        internal static SerializationTypeCode ToSerializationTypeOrInvalid(this SpecialType specialType)
        {
            switch (specialType)
            {
                case SpecialType.System_Boolean:
                    return SerializationTypeCode.Boolean;

                case SpecialType.System_SByte:
                    return SerializationTypeCode.SByte;

                case SpecialType.System_Byte:
                    return SerializationTypeCode.Byte;

                case SpecialType.System_Int16:
                    return SerializationTypeCode.Int16;

                case SpecialType.System_Int32:
                    return SerializationTypeCode.Int32;

                case SpecialType.System_Int64:
                    return SerializationTypeCode.Int64;

                case SpecialType.System_UInt16:
                    return SerializationTypeCode.UInt16;

                case SpecialType.System_UInt32:
                    return SerializationTypeCode.UInt32;

                case SpecialType.System_UInt64:
                    return SerializationTypeCode.UInt64;

                case SpecialType.System_Single:
                    return SerializationTypeCode.Single;

                case SpecialType.System_Double:
                    return SerializationTypeCode.Double;

                case SpecialType.System_Char:
                    return SerializationTypeCode.Char;

                case SpecialType.System_String:
                    return SerializationTypeCode.String;

                case SpecialType.System_Object:
                    return SerializationTypeCode.TaggedObject;

                default:
                    return SerializationTypeCode.Invalid;
            }
        }
    }
}
