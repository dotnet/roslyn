// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    internal static class SpecialTypes
    {
        /// <summary>
        /// Array of names for types from Cor Libraray.
        /// The names should correspond to ids from TypeId enum so
        /// that we could use ids to index into the array
        /// </summary>
        /// <remarks></remarks>
        private static readonly string[] emittedNames = new string[]
        {
            // The following things should be in sync:
            // 1) SpecialType enum
            // 2) names in SpecialTypes.EmittedNames array.
            // 3) languageNames in SemanticFacts.cs
            // 4) languageNames in SemanticFacts.vb
            null, // SpecialType.None
            "System.Object",
            "System.Enum",
            "System.MulticastDelegate",
            "System.Delegate",
            "System.ValueType",
            "System.Void",
            "System.Boolean",
            "System.Char",
            "System.SByte",
            "System.Byte",
            "System.Int16",
            "System.UInt16",
            "System.Int32",
            "System.UInt32",
            "System.Int64",
            "System.UInt64",
            "System.Decimal",
            "System.Single",
            "System.Double",
            "System.String",
            "System.IntPtr",
            "System.UIntPtr",
            "System.Array",
            "System.Collections.IEnumerable",
            "System.Collections.Generic.IEnumerable`1",
            "System.Collections.Generic.IList`1",
            "System.Collections.Generic.ICollection`1",
            "System.Collections.IEnumerator",
            "System.Collections.Generic.IEnumerator`1",
            "System.Collections.Generic.IReadOnlyList`1",
            "System.Collections.Generic.IReadOnlyCollection`1",
            "System.Nullable`1",
            "System.DateTime",
            "System.Runtime.CompilerServices.IsVolatile",
            "System.IDisposable",
            "System.TypedReference",
            "System.ArgIterator",
            "System.RuntimeArgumentHandle",
            "System.RuntimeFieldHandle",
            "System.RuntimeMethodHandle",
            "System.RuntimeTypeHandle",
            "System.IAsyncResult",
            "System.AsyncCallback",
        };

        private readonly static Dictionary<string, SpecialType> nameToTypeIdMap;

        private static readonly Microsoft.Cci.PrimitiveTypeCode[] typeIdToTypeCodeMap;
        private static readonly SpecialType[] typeCodeToTypeIdMap;

        static SpecialTypes()
        {
            nameToTypeIdMap = new Dictionary<string, SpecialType>((int)SpecialType.Count);

            int i;

            for (i = 1; i < emittedNames.Length; i++)
            {
                Debug.Assert(emittedNames[i].IndexOf('+') < 0); // Compilers aren't prepared to lookup for a nested special type.
                nameToTypeIdMap.Add(emittedNames[i], (SpecialType)i);
            }

            typeIdToTypeCodeMap = new Microsoft.Cci.PrimitiveTypeCode[(int)SpecialType.Count + 1];

            for (i = 0; i < typeIdToTypeCodeMap.Length; i++)
            {
                typeIdToTypeCodeMap[i] = Microsoft.Cci.PrimitiveTypeCode.NotPrimitive;
            }

            typeIdToTypeCodeMap[(int)SpecialType.System_Boolean] = Microsoft.Cci.PrimitiveTypeCode.Boolean;
            typeIdToTypeCodeMap[(int)SpecialType.System_Char] = Microsoft.Cci.PrimitiveTypeCode.Char;
            typeIdToTypeCodeMap[(int)SpecialType.System_Void] = Microsoft.Cci.PrimitiveTypeCode.Void;
            typeIdToTypeCodeMap[(int)SpecialType.System_String] = Microsoft.Cci.PrimitiveTypeCode.String;
            typeIdToTypeCodeMap[(int)SpecialType.System_Int64] = Microsoft.Cci.PrimitiveTypeCode.Int64;
            typeIdToTypeCodeMap[(int)SpecialType.System_Int32] = Microsoft.Cci.PrimitiveTypeCode.Int32;
            typeIdToTypeCodeMap[(int)SpecialType.System_Int16] = Microsoft.Cci.PrimitiveTypeCode.Int16;
            typeIdToTypeCodeMap[(int)SpecialType.System_SByte] = Microsoft.Cci.PrimitiveTypeCode.Int8;
            typeIdToTypeCodeMap[(int)SpecialType.System_UInt64] = Microsoft.Cci.PrimitiveTypeCode.UInt64;
            typeIdToTypeCodeMap[(int)SpecialType.System_UInt32] = Microsoft.Cci.PrimitiveTypeCode.UInt32;
            typeIdToTypeCodeMap[(int)SpecialType.System_UInt16] = Microsoft.Cci.PrimitiveTypeCode.UInt16;
            typeIdToTypeCodeMap[(int)SpecialType.System_Byte] = Microsoft.Cci.PrimitiveTypeCode.UInt8;
            typeIdToTypeCodeMap[(int)SpecialType.System_Single] = Microsoft.Cci.PrimitiveTypeCode.Float32;
            typeIdToTypeCodeMap[(int)SpecialType.System_Double] = Microsoft.Cci.PrimitiveTypeCode.Float64;
            typeIdToTypeCodeMap[(int)SpecialType.System_IntPtr] = Microsoft.Cci.PrimitiveTypeCode.IntPtr;
            typeIdToTypeCodeMap[(int)SpecialType.System_UIntPtr] = Microsoft.Cci.PrimitiveTypeCode.UIntPtr;

            typeCodeToTypeIdMap = new SpecialType[(int)Microsoft.Cci.PrimitiveTypeCode.Invalid + 1];

            for (i = 0; i < typeCodeToTypeIdMap.Length; i++)
            {
                typeCodeToTypeIdMap[i] = SpecialType.None;
            }

            typeCodeToTypeIdMap[(int)Microsoft.Cci.PrimitiveTypeCode.Boolean] = SpecialType.System_Boolean;
            typeCodeToTypeIdMap[(int)Microsoft.Cci.PrimitiveTypeCode.Char] = SpecialType.System_Char;
            typeCodeToTypeIdMap[(int)Microsoft.Cci.PrimitiveTypeCode.Void] = SpecialType.System_Void;
            typeCodeToTypeIdMap[(int)Microsoft.Cci.PrimitiveTypeCode.String] = SpecialType.System_String;
            typeCodeToTypeIdMap[(int)Microsoft.Cci.PrimitiveTypeCode.Int64] = SpecialType.System_Int64;
            typeCodeToTypeIdMap[(int)Microsoft.Cci.PrimitiveTypeCode.Int32] = SpecialType.System_Int32;
            typeCodeToTypeIdMap[(int)Microsoft.Cci.PrimitiveTypeCode.Int16] = SpecialType.System_Int16;
            typeCodeToTypeIdMap[(int)Microsoft.Cci.PrimitiveTypeCode.Int8] = SpecialType.System_SByte;
            typeCodeToTypeIdMap[(int)Microsoft.Cci.PrimitiveTypeCode.UInt64] = SpecialType.System_UInt64;
            typeCodeToTypeIdMap[(int)Microsoft.Cci.PrimitiveTypeCode.UInt32] = SpecialType.System_UInt32;
            typeCodeToTypeIdMap[(int)Microsoft.Cci.PrimitiveTypeCode.UInt16] = SpecialType.System_UInt16;
            typeCodeToTypeIdMap[(int)Microsoft.Cci.PrimitiveTypeCode.UInt8] = SpecialType.System_Byte;
            typeCodeToTypeIdMap[(int)Microsoft.Cci.PrimitiveTypeCode.Float32] = SpecialType.System_Single;
            typeCodeToTypeIdMap[(int)Microsoft.Cci.PrimitiveTypeCode.Float64] = SpecialType.System_Double;
            typeCodeToTypeIdMap[(int)Microsoft.Cci.PrimitiveTypeCode.IntPtr] = SpecialType.System_IntPtr;
            typeCodeToTypeIdMap[(int)Microsoft.Cci.PrimitiveTypeCode.UIntPtr] = SpecialType.System_UIntPtr;

        }

        /// <summary>
        /// Gets the name of the special type as it would appear in metadata.
        /// </summary>
        public static string GetMetadataName(this SpecialType id)
        {
            return emittedNames[(int)id];
        }

        public static SpecialType GetTypeFromMetadataName(string metadataName)
        {
            SpecialType id;

            if (nameToTypeIdMap.TryGetValue(metadataName, out id))
            {
                return id;
            }

            return SpecialType.None;
        }

        public static SpecialType GetTypeFromMetadataName(Microsoft.Cci.PrimitiveTypeCode typeCode)
        {
            return typeCodeToTypeIdMap[(int)typeCode];
        }

        public static Microsoft.Cci.PrimitiveTypeCode GetTypeCode(SpecialType typeId)
        {
            return typeIdToTypeCodeMap[(int)typeId];
        }
    }
}