// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class ArrayExtensions
    {
        public static bool IsNullOrEmpty<T>([NotNullWhen(returnValue: false)] this T[]? array)
        {
            return array == null || array.Length == 0;
        }

        public static bool Contains<T>(this T[] array, T item)
        {
            return Array.IndexOf(array, item) >= 0;
        }
    }
}
