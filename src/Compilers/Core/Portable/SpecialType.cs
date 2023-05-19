// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    /// (https://github.com/dotnet/roslyn/blob/main/docs/compilers/Co-located%20core%20types.md)
    /// can be in this enum.
    /// The following things should be in sync:
    ///     1) SpecialType enum
    ///     2) names in SpecialTypes.EmittedNames array.
    /// </remarks>
    public enum SpecialType : sbyte
    {
        /// <summary>
        /// Indicates a non-special type (default value).
        /// </summary>
        None = 0,

        /// <summary>
        /// Indicates that the type is <see cref="object"/>.
        /// </summary>
        System_Object = 1,

        /// <summary>
        /// Indicates that the type is <see cref="Enum"/>.
        /// </summary>
        System_Enum = 2,

        /// <summary>
        /// Indicates that the type is <see cref="MulticastDelegate"/>.
        /// </summary>
        System_MulticastDelegate = 3,

        /// <summary>
        /// Indicates that the type is <see cref="Delegate"/>.
        /// </summary>
        System_Delegate = 4,

        /// <summary>
        /// Indicates that the type is <see cref="ValueType"/>.
        /// </summary>
        System_ValueType = 5,

        /// <summary>
        /// Indicates that the type is <see cref="void"/>.
        /// </summary>
        System_Void = 6,

        /// <summary>
        /// Indicates that the type is <see cref="bool"/>.
        /// </summary>
        System_Boolean = 7,

        /// <summary>
        /// Indicates that the type is <see cref="char"/>.
        /// </summary>
        System_Char = 8,

        /// <summary>
        /// Indicates that the type is <see cref="sbyte"/>.
        /// </summary>
        System_SByte = 9,

        /// <summary>
        /// Indicates that the type is <see cref="byte"/>.
        /// </summary>
        System_Byte = 10,

        /// <summary>
        /// Indicates that the type is <see cref="short"/>.
        /// </summary>
        System_Int16 = 11,

        /// <summary>
        /// Indicates that the type is <see cref="ushort"/>.
        /// </summary>
        System_UInt16 = 12,

        /// <summary>
        /// Indicates that the type is <see cref="int"/>.
        /// </summary>
        System_Int32 = 13,

        /// <summary>
        /// Indicates that the type is <see cref="uint"/>.
        /// </summary>
        System_UInt32 = 14,

        /// <summary>
        /// Indicates that the type is <see cref="long"/>.
        /// </summary>
        System_Int64 = 15,

        /// <summary>
        /// Indicates that the type is <see cref="ulong"/>.
        /// </summary>
        System_UInt64 = 16,

        /// <summary>
        /// Indicates that the type is <see cref="decimal"/>.
        /// </summary>
        System_Decimal = 17,

        /// <summary>
        /// Indicates that the type is <see cref="float"/>.
        /// </summary>
        System_Single = 18,

        /// <summary>
        /// Indicates that the type is <see cref="double"/>.
        /// </summary>
        System_Double = 19,

        /// <summary>
        /// Indicates that the type is <see cref="string"/>.
        /// </summary>
        System_String = 20,

        /// <summary>
        /// Indicates that the type is <see cref="IntPtr" />.
        /// </summary>
        System_IntPtr = 21,

        /// <summary>
        /// Indicates that the type is <see cref="UIntPtr"/>.
        /// </summary>
        System_UIntPtr = 22,

        /// <summary>
        /// Indicates that the type is <see cref="Array"/>.
        /// </summary>
        System_Array = 23,

        /// <summary>
        /// Indicates that the type is <see cref="IEnumerable"/>.
        /// </summary>
        System_Collections_IEnumerable = 24,

        /// <summary>
        /// Indicates that the type is <see cref="IEnumerable{T}"/>.
        /// </summary>
        System_Collections_Generic_IEnumerable_T = 25, // Note: IEnumerable<int> (i.e. constructed type) has no special type

        /// <summary>
        /// Indicates that the type is <see cref="IList{T}"/>.
        /// </summary>
        System_Collections_Generic_IList_T = 26,

        /// <summary>
        /// Indicates that the type is <see cref="ICollection{T}"/>.
        /// </summary>
        System_Collections_Generic_ICollection_T = 27,

        /// <summary>
        /// Indicates that the type is <see cref="IEnumerator"/>.
        /// </summary>
        System_Collections_IEnumerator = 28,

        /// <summary>
        /// Indicates that the type is <see cref="IEnumerator{T}"/>.
        /// </summary>
        System_Collections_Generic_IEnumerator_T = 29,

        /// <summary>
        /// Indicates that the type is <see cref="IReadOnlyList{T}"/>.
        /// </summary>
        System_Collections_Generic_IReadOnlyList_T = 30,

        /// <summary>
        /// Indicates that the type is <see cref="IReadOnlyCollection{T}"/>.
        /// </summary>
        System_Collections_Generic_IReadOnlyCollection_T = 31,

        /// <summary>
        /// Indicates that the type is <see cref="Nullable{T}"/>.
        /// </summary>
        System_Nullable_T = 32,

        /// <summary>
        /// Indicates that the type is <see cref="DateTime"/>.
        /// </summary>
        System_DateTime = 33,

        /// <summary>
        /// Indicates that the type is <see cref="IsVolatile"/>.
        /// </summary>
        System_Runtime_CompilerServices_IsVolatile = 34,

        /// <summary>
        /// Indicates that the type is <see cref="IDisposable"/>.
        /// </summary>
        System_IDisposable = 35,

#pragma warning disable CA1200 // Avoid using cref tags with a prefix
        /// <summary>
        /// Indicates that the type is <see cref="T:System.TypedReference"/>.
        /// </summary>
#pragma warning restore CA1200 // Avoid using cref tags with a prefix
        System_TypedReference = 36,

#pragma warning disable CA1200 // Avoid using cref tags with a prefix
        /// <summary>
        /// Indicates that the type is <see cref="T:System.ArgIterator"/>.
        /// </summary>
#pragma warning restore CA1200 // Avoid using cref tags with a prefix
        System_ArgIterator = 37,

#pragma warning disable CA1200 // Avoid using cref tags with a prefix
        /// <summary>
        /// Indicates that the type is <see cref="T:System.RuntimeArgumentHandle"/>.
        /// </summary>
#pragma warning restore CA1200 // Avoid using cref tags with a prefix
        System_RuntimeArgumentHandle = 38,

        /// <summary>
        /// Indicates that the type is <see cref="RuntimeFieldHandle"/>.
        /// </summary>
        System_RuntimeFieldHandle = 39,

        /// <summary>
        /// Indicates that the type is <see cref="RuntimeMethodHandle"/>.
        /// </summary>
        System_RuntimeMethodHandle = 40,

        /// <summary>
        /// Indicates that the type is <see cref="RuntimeTypeHandle"/>.
        /// </summary>
        System_RuntimeTypeHandle = 41,

        /// <summary>
        /// Indicates that the type is <see cref="IAsyncResult"/>.
        /// </summary>
        System_IAsyncResult = 42,

        /// <summary>
        /// Indicates that the type is <see cref="AsyncCallback"/>.
        /// </summary>
        System_AsyncCallback = 43,

        /// <summary>
        /// Indicates that the type is System.Runtime.CompilerServices.RuntimeFeature.
        /// </summary>
        System_Runtime_CompilerServices_RuntimeFeature = 44,

        /// <summary>
        /// An attribute that is placed on each method with a 'methodimpl" aka ".override" in metadata.
        /// </summary>
        System_Runtime_CompilerServices_PreserveBaseOverridesAttribute = 45,

        /// <summary>
        /// An attribute that is placed on an inline array type.
        /// </summary>
        System_Runtime_CompilerServices_InlineArrayAttribute = 46,

        /// <summary>
        /// Count of special types. This is not a count of enum members.
        /// </summary>
        Count = System_Runtime_CompilerServices_InlineArrayAttribute
    }
}
