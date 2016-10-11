﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        IgnoreCustomModifiers = 1,
        IgnoreArraySizesAndLowerBounds = 2,
        IgnoreDynamic = 4,
        IgnoreTupleNames = 8,
        IgnoreDynamicAndTupleNames = IgnoreDynamic | IgnoreTupleNames,
        IgnoreCustomModifiersAndArraySizesAndLowerBounds = IgnoreCustomModifiers | IgnoreArraySizesAndLowerBounds,
        AllIgnoreOptions = IgnoreCustomModifiersAndArraySizesAndLowerBounds | IgnoreDynamic | IgnoreTupleNames
    }

    internal static class TypeCompareKindExtension
    {
        public static TypeCompareKind AddIgnoreCustomModifiersAndArraySizesAndLowerBounds(this TypeCompareKind self, bool condition)
        {
            return condition ? (self | TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds) : self;
        }

        public static TypeCompareKind AddIgnoreDynamic(this TypeCompareKind self, bool condition)
        {
            return condition ? (self | TypeCompareKind.IgnoreDynamic) : self;
        }

        public static TypeCompareKind AddIgnoreTupleNames(this TypeCompareKind self, bool condition)
        {
            return condition ? (self | TypeCompareKind.IgnoreTupleNames) : self;
        }
    }
}
