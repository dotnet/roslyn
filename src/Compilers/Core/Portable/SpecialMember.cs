// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis
{
    // members of special types
    internal enum SpecialMember
    {
        System_String__CtorSZArrayChar,

        System_String__ConcatStringString,
        System_String__ConcatStringStringString,
        System_String__ConcatStringStringStringString,
        System_String__ConcatStringArray,

        System_String__ConcatObject,
        System_String__ConcatObjectObject,
        System_String__ConcatObjectObjectObject,
        System_String__ConcatObjectArray,

        System_String__Concat_2ReadOnlySpans,
        System_String__Concat_3ReadOnlySpans,
        System_String__Concat_4ReadOnlySpans,

        System_String__op_Equality,
        System_String__op_Inequality,
        System_String__Length,
        System_String__Chars,
        System_String__Format,
        System_String__Format_IFormatProvider,
        System_String__Substring,

        System_String__op_Implicit_ToReadOnlySpanOfChar,

        System_Double__IsNaN,
        System_Single__IsNaN,

        System_Delegate__Combine,
        System_Delegate__Remove,
        System_Delegate__op_Equality,
        System_Delegate__op_Inequality,
        System_Delegate__CreateDelegate,
        System_Delegate__CreateDelegate4,

        System_Decimal__Zero,
        System_Decimal__MinusOne,
        System_Decimal__One,
        System_Decimal__CtorInt32,
        System_Decimal__CtorUInt32,
        System_Decimal__CtorInt64,
        System_Decimal__CtorUInt64,
        System_Decimal__CtorSingle,
        System_Decimal__CtorDouble,
        System_Decimal__CtorInt32Int32Int32BooleanByte,

        System_Decimal__op_Addition,
        System_Decimal__op_Subtraction,
        System_Decimal__op_Multiply,
        System_Decimal__op_Division,
        System_Decimal__op_Modulus,
        System_Decimal__op_UnaryNegation,
        System_Decimal__op_Increment,
        System_Decimal__op_Decrement,

        System_Decimal__NegateDecimal,
        System_Decimal__RemainderDecimalDecimal,
        System_Decimal__AddDecimalDecimal,
        System_Decimal__SubtractDecimalDecimal,
        System_Decimal__MultiplyDecimalDecimal,
        System_Decimal__DivideDecimalDecimal,
        System_Decimal__ModuloDecimalDecimal,
        System_Decimal__CompareDecimalDecimal,

        System_Decimal__op_Equality,
        System_Decimal__op_Inequality,
        System_Decimal__op_GreaterThan,
        System_Decimal__op_GreaterThanOrEqual,
        System_Decimal__op_LessThan,
        System_Decimal__op_LessThanOrEqual,

        System_Decimal__op_Implicit_FromByte,
        System_Decimal__op_Implicit_FromChar,
        System_Decimal__op_Implicit_FromInt16,
        System_Decimal__op_Implicit_FromInt32,
        System_Decimal__op_Implicit_FromInt64,
        System_Decimal__op_Implicit_FromSByte,
        System_Decimal__op_Implicit_FromUInt16,
        System_Decimal__op_Implicit_FromUInt32,
        System_Decimal__op_Implicit_FromUInt64,

        System_Decimal__op_Explicit_ToByte,
        System_Decimal__op_Explicit_ToUInt16,
        System_Decimal__op_Explicit_ToSByte,
        System_Decimal__op_Explicit_ToInt16,
        System_Decimal__op_Explicit_ToSingle,
        System_Decimal__op_Explicit_ToDouble,
        System_Decimal__op_Explicit_ToChar,
        System_Decimal__op_Explicit_ToUInt64,
        System_Decimal__op_Explicit_ToInt32,
        System_Decimal__op_Explicit_ToUInt32,
        System_Decimal__op_Explicit_ToInt64,
        System_Decimal__op_Explicit_FromDouble,
        System_Decimal__op_Explicit_FromSingle,

        System_DateTime__MinValue,
        System_DateTime__CtorInt64,
        System_DateTime__CompareDateTimeDateTime,

        System_DateTime__op_Equality,
        System_DateTime__op_Inequality,
        System_DateTime__op_GreaterThan,
        System_DateTime__op_GreaterThanOrEqual,
        System_DateTime__op_LessThan,
        System_DateTime__op_LessThanOrEqual,

        System_Collections_IEnumerable__GetEnumerator,
        System_Collections_IEnumerator__Current,
        System_Collections_IEnumerator__get_Current,
        System_Collections_IEnumerator__MoveNext,
        System_Collections_IEnumerator__Reset,

        System_Collections_Generic_IEnumerable_T__GetEnumerator,
        System_Collections_Generic_IEnumerator_T__Current,
        System_Collections_Generic_IEnumerator_T__get_Current,

        System_IDisposable__Dispose,

        System_Array__Length,
        System_Array__LongLength,
        System_Array__GetLowerBound,
        System_Array__GetUpperBound,

        System_Object__GetHashCode,
        System_Object__Equals,
        System_Object__EqualsObjectObject,
        System_Object__ToString,
        System_Object__ReferenceEquals,

        System_IntPtr__op_Explicit_ToPointer,
        System_IntPtr__op_Explicit_ToInt32,
        System_IntPtr__op_Explicit_ToInt64,
        System_IntPtr__op_Explicit_FromPointer,
        System_IntPtr__op_Explicit_FromInt32,
        System_IntPtr__op_Explicit_FromInt64,
        System_UIntPtr__op_Explicit_ToPointer,
        System_UIntPtr__op_Explicit_ToUInt32,
        System_UIntPtr__op_Explicit_ToUInt64,
        System_UIntPtr__op_Explicit_FromPointer,
        System_UIntPtr__op_Explicit_FromUInt32,
        System_UIntPtr__op_Explicit_FromUInt64,

        System_Nullable_T_GetValueOrDefault,
        System_Nullable_T_GetValueOrDefaultDefaultValue,
        System_Nullable_T_get_Value,
        System_Nullable_T_get_HasValue,
        System_Nullable_T__ctor,
        System_Nullable_T__op_Implicit_FromT,
        System_Nullable_T__op_Explicit_ToT,

        System_Runtime_CompilerServices_RuntimeFeature__DefaultImplementationsOfInterfaces,
        System_Runtime_CompilerServices_RuntimeFeature__UnmanagedSignatureCallingConvention,
        System_Runtime_CompilerServices_RuntimeFeature__CovariantReturnsOfClasses,
        System_Runtime_CompilerServices_RuntimeFeature__VirtualStaticsInInterfaces,
        System_Runtime_CompilerServices_RuntimeFeature__NumericIntPtr,
        System_Runtime_CompilerServices_RuntimeFeature__ByRefFields,
        System_Runtime_CompilerServices_RuntimeFeature__ByRefLikeGenerics,

        System_Runtime_CompilerServices_PreserveBaseOverridesAttribute__ctor,
        System_Runtime_CompilerServices_InlineArrayAttribute__ctor,

        System_ReadOnlySpan_T__ctor_Reference,

        System_Collections_Generic_IReadOnlyCollection_T__Count,
        System_Collections_Generic_IReadOnlyList_T__get_Item,
        System_Collections_Generic_ICollection_T__Count,
        System_Collections_Generic_ICollection_T__IsReadOnly,
        System_Collections_Generic_ICollection_T__Add,
        System_Collections_Generic_ICollection_T__Clear,
        System_Collections_Generic_ICollection_T__Contains,
        System_Collections_Generic_ICollection_T__CopyTo,
        System_Collections_Generic_ICollection_T__Remove,
        System_Collections_Generic_IList_T__get_Item,
        System_Collections_Generic_IList_T__IndexOf,
        System_Collections_Generic_IList_T__Insert,
        System_Collections_Generic_IList_T__RemoveAt,

        System_Reflection_MethodBase__GetMethodFromHandle,
        System_Reflection_MethodBase__GetMethodFromHandle2,

        System_Array__get_Length,
        System_Array__Empty,
        System_Array__SetValue,

        System_Type__GetTypeFromHandle,

        System_Runtime_CompilerServices_AsyncHelpers__AwaitAwaiter_TAwaiter,
        System_Runtime_CompilerServices_AsyncHelpers__UnsafeAwaitAwaiter_TAwaiter,
        System_Runtime_CompilerServices_AsyncHelpers__HandleAsyncEntryPoint_Task,
        System_Runtime_CompilerServices_AsyncHelpers__HandleAsyncEntryPoint_Task_Int32,

        Count
    }
}
