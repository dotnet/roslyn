// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    [Flags]
    internal enum TypeSymbolEqualityOptions : byte
    {
        None = 0,
        IgnoreCustomModifiers = 1 << 0,
        IgnoreArraySizesAndLowerBounds = 1 << 1, 
        IgnoreDynamic = 1 << 2,
        CompareNullableModifiersForReferenceTypes = 1 << 3,

        IgnoreCustomModifiersAndArraySizesAndLowerBounds = IgnoreCustomModifiers | IgnoreArraySizesAndLowerBounds,
        SameType = IgnoreDynamic | IgnoreCustomModifiers | IgnoreArraySizesAndLowerBounds,
        AllAspects = CompareNullableModifiersForReferenceTypes,
    }
}