// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Roslyn.Utilities
{
    /// <summary>
    /// A class that abstracts the accessing of a value that is guaranteed to be available at some point.
    /// </summary>
    internal abstract class ValueSource<T>
    {
        public abstract bool TryGetValue([MaybeNullWhen(false)] out T value);
        public abstract T GetValue(CancellationToken cancellationToken = default);
        public abstract Task<T> GetValueAsync(CancellationToken cancellationToken = default);

        public static ValueSource<T> Constant(T value)
            => new AsyncLazy<T>(value);
    }

    internal static class ValueSourceExtensions
    {
        internal static T GetValueOrDefault<T>(this Optional<T> optional) where T : struct
            => optional.Value;
    }
}
