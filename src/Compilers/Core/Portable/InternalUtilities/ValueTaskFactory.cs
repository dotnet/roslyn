// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace Roslyn.Utilities
{
    /// <summary>
    /// Implements <see cref="ValueTask{TResult}"/> members that are only available in .NET 5.
    /// </summary>
    internal static class ValueTaskFactory
    {
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
        public static ValueTask<T> FromResult<T>(T result)
            => new(result);
#pragma warning restore

        public static ValueTask CompletedTask
            => new();
    }
}
