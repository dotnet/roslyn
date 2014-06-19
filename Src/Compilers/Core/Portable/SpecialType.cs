// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Specifies the Ids of special runtime types.
    /// </summary>
    /// <remarks>
    /// Only types explicitly mentioned in "Co-located core types" spec 
    /// (http://devdiv/sites/docs/Dev11/CLR/Specs/CoreFx/Co-located%20core%20types.docx)
    /// can be in this enum.
    /// The following things should be in sync:
    ///     1) SpecialType enum
    ///     2) names in SpecialTypes.EmittedNames array.
    /// </remarks>
    public enum SpecialType : sbyte
    {
        /// <summary>
        /// OIndicated Non-sepecial type (default value).
        /// </summary>
        None = 0,

        /// <summary>
        /// Indicates that the type is <see cref="object"/>.
        /// </summary>
        System_Object,

        /// <summary>
        /// Indicates that the type is <see cref="Enum"/>.
        /// </summary>
        System_Enum,

        /// <summary>
        /// Indicates that the type is <see cref="MulticastDelegate"/>.
        /// </summary>
        System_MulticastDelegate,

        /// <summary>
        /// Indicates that the type is <see cref="Delegate"/>.
        /// </summary>
        System_Delegate,

        /// <summary>
        /// Indicates that the type is <see cref="ValueType"/>.
        /// </summary>
        System_ValueType,

        /// <summary>
        /// Indicates that the type is <see cref="void"/>.
        /// </summary>
        System_Void,

        /// <summary>
        /// Indicates that the type is <see cref="bool"/>.
        /// </summary>
        System_Boolean,

        /// <summary>
        /// Indicates that the type is <see cref="char"/>.
        /// </summary>
        System_Char,

        /// <summary>
        /// Indicates that the type is <see cref="sbyte"/>.
        /// </summary>
        System_SByte,

        /// <summary>
        /// Indicates that the type is <see cref="byte"/>.
        /// </summary>
        System_Byte,

        /// <summary>
        /// Indicates that the type is <see cref="short"/>.
        /// </summary>
        System_Int16,

        /// <summary>
        /// Indicates that the type is <see cref="ushort"/>.
        /// </summary>
        System_UInt16,

        /// <summary>
        /// Indicates that the type is <see cref="int"/>.
        /// </summary>
        System_Int32,

        /// <summary>
        /// Indicates that the type is <see cref="uint"/>.
        /// </summary>
        System_UInt32,

        /// <summary>
        /// Indicates that the type is <see cref="long"/>.
        /// </summary>
        System_Int64,

        /// <summary>
        /// Indicates that the type is <see cref="ulong"/>.
        /// </summary>
        System_UInt64,

        /// <summary>
        /// Indicates that the type is <see cref="decimal"/>.
        /// </summary>
        System_Decimal,

        /// <summary>
        /// Indicates that the type is <see cref="float"/>.
        /// </summary>
        System_Single,

        /// <summary>
        /// Indicates that the type is <see cref="double"/>.
        /// </summary>
        System_Double,

        /// <summary>
        /// Indicates that the type is <see cref="string"/>.
        /// </summary>
        System_String,

        /// <summary>
        /// Indicates that the type is <see cref="IntPtr" />.
        /// </summary>
        System_IntPtr,

        /// <summary>
        /// Indicates that the type is <see cref="UIntPtr"/>.
        /// </summary>
        System_UIntPtr,

        /// <summary>
        /// Indicates that the type is <see cref="Array"/>.
        /// </summary>
        System_Array,

        /// <summary>
        /// Indicates that the type is <see cref="IEnumerable"/>.
        /// </summary>
        System_Collections_IEnumerable,

        /// <summary>
        /// Indicates that the type is <see cref="IEnumerable{T}"/>.
        /// </summary>
        System_Collections_Generic_IEnumerable_T, // Note: IEnumerable<int> (i.e. constructed type) has no special type

        /// <summary>
        /// Indicates that the type is <see cref="IList{T}"/>.
        /// </summary>
        System_Collections_Generic_IList_T,

        /// <summary>
        /// Indicates that the type is <see cref="ICollection{T}"/>.
        /// </summary>
        System_Collections_Generic_ICollection_T,

        /// <summary>
        /// Indicates that the type is <see cref="IEnumerator"/>.
        /// </summary>
        System_Collections_IEnumerator,

        /// <summary>
        /// Indicates that the type is <see cref="IEnumerator{T}"/>.
        /// </summary>
        System_Collections_Generic_IEnumerator_T,

        /// <summary>
        /// Indicates that the type is <see cref="IReadOnlyList{T}"/>.
        /// </summary>
        System_Collections_Generic_IReadOnlyList_T,

        /// <summary>
        /// Indicates that the type is <see cref="IReadOnlyCollection{T}"/>.
        /// </summary>
        System_Collections_Generic_IReadOnlyCollection_T,

        /// <summary>
        /// Indicates that the type is <see cref="Nullable{T}"/>.
        /// </summary>
        System_Nullable_T,

        /// <summary>
        /// Indicates that the type is <see cref="DateTime"/>.
        /// </summary>
        System_DateTime,

        /// <summary>
        /// Indicates that the type is <see cref="IsVolatile"/>.
        /// </summary>
        System_Runtime_CompilerServices_IsVolatile,

        /// <summary>
        /// Indicates that the type is <see cref="IDisposable"/>.
        /// </summary>
        System_IDisposable,

        /// <summary>
        /// Indicates that the type is <see cref="T:System.TypedReference"/>.
        /// </summary>
        System_TypedReference,

        /// <summary>
        /// Indicates that the type is <see cref="T:System.ArgIterator"/>.
        /// </summary>
        System_ArgIterator,

        /// <summary>
        /// Indicates that the type is <see cref="T:System.RuntimeArgumentHandle"/>.
        /// </summary>
        System_RuntimeArgumentHandle,

        /// <summary>
        /// Indicates that the type is <see cref="RuntimeFieldHandle"/>.
        /// </summary>
        System_RuntimeFieldHandle,

        /// <summary>
        /// Indicates that the type is <see cref="RuntimeMethodHandle"/>.
        /// </summary>
        System_RuntimeMethodHandle,

        /// <summary>
        /// Indicates that the type is <see cref="RuntimeTypeHandle"/>.
        /// </summary>
        System_RuntimeTypeHandle,

        /// <summary>
        /// Indicates that the type is <see cref="IAsyncResult"/>.
        /// </summary>
        System_IAsyncResult,

        /// <summary>
        /// Indicates that the type is <see cref="AsyncCallback"/>.
        /// </summary>
        System_AsyncCallback,

        /// <summary>
        /// Count of special types. This is not a count of enum members.
        /// </summary>
        Count = System_AsyncCallback
    }
}