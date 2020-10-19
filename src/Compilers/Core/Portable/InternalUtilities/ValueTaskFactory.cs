// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Roslyn.Utilities
{
    /// <summary>
    /// Implements <see cref="ValueTask"/> and <see cref="ValueTask{TResult}"/> static members that are only available in .NET 5.
    /// </summary>
    internal static class ValueTaskFactory
    {
        [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "This is a ValueTask wrapper, not an asynchronous method.")]
        public static ValueTask<T> FromResult<T>(T result)
            => new(result);

        public static ValueTask CompletedTask
            => new();
    }
}
