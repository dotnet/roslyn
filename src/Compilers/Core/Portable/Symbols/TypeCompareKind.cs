// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

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

        AllNullableIgnoreOptions = IgnoreNullableModifiersForReferenceTypes | ObliviousNullableModifierMatchesAny,
        AllIgnoreOptions = IgnoreCustomModifiersAndArraySizesAndLowerBounds | IgnoreDynamic | IgnoreTupleNames | AllNullableIgnoreOptions,
        AllIgnoreOptionsForVB = IgnoreCustomModifiersAndArraySizesAndLowerBounds | IgnoreTupleNames,

        CLRSignatureCompareOptions = TypeCompareKind.AllIgnoreOptions & ~TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds,
    }
}
