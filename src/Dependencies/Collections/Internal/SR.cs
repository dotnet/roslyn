// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Collections.Internal;

internal static class SR
{
    // Strings not localized, should only be used for internal API contract messages and not surfaced to users.

    internal const string Arg_ArrayPlusOffTooSmall = "Destination array is not long enough to copy all the items in the collection. Check array index and length.";
    internal const string Arg_HTCapacityOverflow = "Hashtable's capacity overflowed and went negative. Check load factor, capacity and the current size of the table.";
    internal const string Arg_KeyNotFoundWithKey = "The given key '{0}' was not present in the dictionary.";
    internal const string Arg_LongerThanDestArray = "Destination array was not long enough. Check the destination index, length, and the array's lower bounds.";
    internal const string Arg_LongerThanSrcArray = "Source array was not long enough. Check the source index, length, and the array's lower bounds.";
    internal const string Arg_NonZeroLowerBound = "The lower bound of target array must be zero.";
    internal const string Arg_RankMultiDimNotSupported = "Only single dimensional arrays are supported for the requested action.";
    internal const string Arg_WrongType = "The value \"{0}\" is not of type \"{1}\" and cannot be used in this generic collection.";
    internal const string Argument_AddingDuplicateWithKey = "An item with the same key has already been added. Key: {0}";
    internal const string Argument_IncompatibleArrayType = "Target array type is not compatible with the type of items in the collection.";
    internal const string Argument_InvalidOffLen = "Offset and length were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection.";
    internal const string ArgumentOutOfRange_ArrayLB = "Number was less than the array's lower bound in the first dimension.";
    internal const string ArgumentOutOfRange_BiggerThanCollection = "Larger than collection size.";
    internal const string ArgumentOutOfRange_Count = "Count must be positive and count must refer to a location within the string/array/collection.";
    internal const string ArgumentOutOfRange_IndexMustBeLess = "Index was out of range. Must be non-negative and less than the size of the collection.";
    internal const string ArgumentOutOfRange_ListInsert = "Index must be within the bounds of the List.";
    internal const string ArgumentOutOfRange_NeedNonNegNum = "Non-negative number required.";
    internal const string ArgumentOutOfRange_SmallCapacity = "capacity was less than the current size.";
    internal const string InvalidOperation_ConcurrentOperationsNotSupported = "Operations that change non-concurrent collections must have exclusive access. A concurrent update was performed on this collection and corrupted its state. The collection's state is no longer correct.";
    internal const string InvalidOperation_EnumFailedVersion = "Collection was modified; enumeration operation may not execute.";
    internal const string InvalidOperation_EnumOpCantHappen = "Enumeration has either not started or has already finished.";
    internal const string InvalidOperation_IComparerFailed = "Failed to compare two elements in the array.";
    internal const string NotSupported_KeyCollectionSet = "Mutating a key collection derived from a dictionary is not allowed.";
    internal const string NotSupported_ValueCollectionSet = "Mutating a value collection derived from a dictionary is not allowed.";
    internal const string Rank_MustMatch = "The specified arrays must have the same number of dimensions.";
    internal const string NotSupported_FixedSizeCollection = "Collection was of a fixed size.";
    internal const string ArgumentException_OtherNotArrayOfCorrectLength = "Object is not a array with the same number of elements as the array to compare it to.";
    internal const string Arg_BogusIComparer = "Unable to sort because the IComparer.Compare() method returns inconsistent results. Either a value does not compare equal to itself, or one value repeatedly compared to another value yields different results. IComparer: '{0}'.";
    internal const string CannotFindOldValue = "Cannot find the old value";
    internal const string ArgumentOutOfRange_IndexMustBeLessOrEqual = "Index was out of range. Must be non-negative and less than or equal to the size of the collection.";
}
