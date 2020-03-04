﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
