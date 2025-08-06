// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Specifies the different kinds of comparison between types.
    /// </summary>
    [Flags]
    internal enum TypeCompareKind
    {
        ConsiderEverything = 0,

        // This comparison option is temporary. All usages should be reviewed, and it should be removed. https://github.com/dotnet/roslyn/issues/31742
        ConsiderEverything2 = ConsiderEverything,
        IgnoreCustomModifiersAndArraySizesAndLowerBounds = 1,
        IgnoreDynamic = 2,
        IgnoreTupleNames = 4,
        IgnoreDynamicAndTupleNames = IgnoreDynamic | IgnoreTupleNames,
        IgnoreNullableModifiersForReferenceTypes = 8,
        ObliviousNullableModifierMatchesAny = 16,
        IgnoreNativeIntegers = 32,

        // For the purposes of a few specific cases such as overload comparisons, we need to consider function pointers that only differ
        // by ref, in, out, or ref readonly identical. For these specific scenarios, this option is available. However, it is not in
        // in AllIgnoreOptions because except for these few specific cases, ignoring the RefKind is not the correct behavior.
        // For overloading, we disallow overloading on just the type of refness in a function pointer parameter or return type. Technically
        // we could emit signatures overloaded on this distinction, because we must encode the type of ref in a modreq on the appropriate
        // parameter in metadata, which would change the type. However, this would be inconsistent with how ref vs out vs in works on
        // top-level signatures today, so we disallow it in source.
        FunctionPointerRefOutInRefReadonlyMatch = 64,

        AllNullableIgnoreOptions = IgnoreNullableModifiersForReferenceTypes | ObliviousNullableModifierMatchesAny,
        AllIgnoreOptions = IgnoreCustomModifiersAndArraySizesAndLowerBounds | IgnoreDynamic | IgnoreTupleNames | AllNullableIgnoreOptions | IgnoreNativeIntegers,
        AllIgnoreOptionsForVB = IgnoreCustomModifiersAndArraySizesAndLowerBounds | IgnoreTupleNames,

        CLRSignatureCompareOptions = TypeCompareKind.AllIgnoreOptions & ~TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds,

        AllIgnoreOptionsPlusNullableWithObliviousMatchesAny = TypeCompareKind.AllIgnoreOptions & ~(TypeCompareKind.IgnoreNullableModifiersForReferenceTypes),
    }
}
