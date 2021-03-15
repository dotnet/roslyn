﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis.Shared.Collections
{
    internal static class TemporaryArrayExtensions
    {
        /// <summary>
        /// Gets a mutable reference to a <see cref="TemporaryArray{T}"/> stored in a <c>using</c> variable.
        /// </summary>
        /// <remarks>
        /// <para>This supporting method allows <see cref="TemporaryArray{T}"/>, a non-copyable <see langword="struct"/>
        /// implementing <see cref="IDisposable"/>, to be used with <c>using</c> statements while still allowing them to
        /// be passed by reference in calls. The following two calls are equivalent:</para>
        ///
        /// <code>
        /// using var array = TemporaryArray&lt;T&gt;.Empty;
        ///
        /// // Using the 'Unsafe.AsRef' method
        /// Method(ref Unsafe.AsRef(in array));
        ///
        /// // Using this helper method
        /// Method(ref array.AsRef());
        /// </code>
        ///
        /// <para>⚠ Do not move or rename this method without updating the corresponding
        /// <see href="https://github.com/dotnet/roslyn-analyzers/blob/30180a51af8c4711e51d98df7345f14d083efb63/src/Roslyn.Diagnostics.Analyzers/Core/TemporaryArrayAsRefAnalyzer.cs">RS0049</see>
        /// analyzer.</para>
        /// </remarks>
        /// <typeparam name="T">The type of element stored in the temporary array.</typeparam>
        /// <param name="array">A read-only reference to a temporary array which is part of a <c>using</c> statement.</param>
        /// <returns>A mutable reference to the temporary array.</returns>
        public static ref TemporaryArray<T> AsRef<T>(this in TemporaryArray<T> array)
            => ref Unsafe.AsRef(in array);

        public static bool Any<T>(this in TemporaryArray<T> array, Func<T, bool> predicate)
        {
            foreach (var item in array)
            {
                if (predicate(item))
                    return true;
            }

            return false;
        }

        public static bool All<T>(this in TemporaryArray<T> array, Func<T, bool> predicate)
        {
            foreach (var item in array)
            {
                if (!predicate(item))
                    return false;
            }

            return true;
        }

        public static void AddIfNotNull<T>(this ref TemporaryArray<T> array, T? value)
            where T : struct
        {
            if (value is not null)
            {
                array.Add(value.Value);
            }
        }

        public static void AddIfNotNull<T>(this ref TemporaryArray<T> array, T? value)
            where T : class
        {
            if (value is not null)
            {
                array.Add(value);
            }
        }
    }
}
