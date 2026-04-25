// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Razor.Threading;

// Adapted from https://github.com/dotnet/roslyn/blob/d89c824648207390f5be355a782048812ba5f91e/src/Workspaces/SharedUtilitiesAndExtensions/Compiler/Core/Utilities/SpecializedTasks.cs
internal static class SpecializedTasks
{
    public static readonly Task<bool> True = Task.FromResult(true);
    public static readonly Task<bool> False = Task.FromResult(false);

    public static Task<T?> AsNullable<T>(this Task<T> task) where T : class
        => task!;

    public static Task<T?> Default<T>()
        => EmptyTasks<T>.Default;

    public static Task<T?> Null<T>() where T : class
        => Default<T>();

    public static Task<IReadOnlyList<T>> EmptyReadOnlyList<T>()
        => EmptyTasks<T>.EmptyReadOnlyList;

    public static Task<IList<T>> EmptyList<T>()
        => EmptyTasks<T>.EmptyList;

    public static Task<ImmutableArray<T>> EmptyImmutableArray<T>()
        => EmptyTasks<T>.EmptyImmutableArray;

    public static Task<IEnumerable<T>> EmptyEnumerable<T>()
        => EmptyTasks<T>.EmptyEnumerable;

    public static Task<T[]> EmptyArray<T>()
        => EmptyTasks<T>.EmptyArray;

    private static class EmptyTasks<T>
    {
        public static readonly Task<T?> Default = Task.FromResult<T?>(default);
        public static readonly Task<IEnumerable<T>> EmptyEnumerable = Task.FromResult<IEnumerable<T>>([]);
        public static readonly Task<T[]> EmptyArray = Task.FromResult<T[]>([]);
        public static readonly Task<ImmutableArray<T>> EmptyImmutableArray = Task.FromResult(ImmutableArray<T>.Empty);
        public static readonly Task<IList<T>> EmptyList = Task.FromResult<IList<T>>([]);
        public static readonly Task<IReadOnlyList<T>> EmptyReadOnlyList = Task.FromResult<IReadOnlyList<T>>([]);
    }
}
