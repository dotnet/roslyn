//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;
using Roslyn.Compilers.Internal;

namespace Roslyn.Compilers.CSharp
{
    internal static class CorLibTypes
    {
        /// <summary>
        /// Type ids should be in sync with names in s_EmittedNames array.
        /// </summary>
        /// <remarks></remarks>
        public enum TypeId
        {
            System_Object,
            System_Enum,
            System_MulticastDelegate,
            System_Delegate,
            System_ValueType,
            System_Void,
            System_Boolean,
            System_Char,
            System_SByte,
            System_Byte,
            System_Int16,
            System_UInt16,
            System_Int32,
            System_UInt32,
            System_Int64,
            System_UInt64,
            System_Single,
            System_Double,
            System_String,
            System_IntPtr,
            System_UIntPtr,
            System_Decimal,
            System_Type,
            System_Array,
            System_Collections_IEnumerable,
            System_Collections_Generic_IEnumerable_T,
            System_Collections_Generic_IList_T,
            System_Collections_Generic_ICollection_T,
            System_Nullable_T,

            Count,
        }

        /// <summary>
        /// Array of names for types from Cor Libraray.
        /// The names should correspond to ids from TypeId enum so
        /// that we could use ids to index into the array
        /// </summary>
        /// <remarks></remarks>
        private static readonly string[] emittedNames = new string[]
           {
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
            "System.Single",
            "System.Double",
            "System.String",
            "System.IntPtr",
            "System.UIntPtr",
            "System.Decimal",
            "System.Type",
            "System.Array",
            "System.Collections.IEnumerable",
            "System.Collections.Generic.IEnumerable`1",
            "System.Collections.Generic.IList`1",
            "System.Collections.Generic.ICollection`1",
            "System.Nullable`1",
            };

        public static string GetEmittedName(TypeId id)
        {
            return emittedNames[(int)id];
        }
    }
}