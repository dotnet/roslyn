// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Roslyn.Utilities
{
    internal static partial class TaskExtensions
    {
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods.  This is an explicit task management method.
        public static async ValueTask<ImmutableArray<TResult>> WhenAll<TResult>(this ImmutableArray<Task<TResult>> tasks)
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
        {
            using var _ = ArrayBuilder<TResult>.GetInstance(tasks.Length, out var result);

            // Explicit cast to IEnumerable<Task> so we call the overload that doesn't allocate an array as the result.
            await Task.WhenAll((IEnumerable<Task>)tasks).ConfigureAwait(false);
            foreach (var task in tasks)
                result.Add(await task.ConfigureAwait(false));

            return result.ToImmutableAndClear();
        }
    }
}
