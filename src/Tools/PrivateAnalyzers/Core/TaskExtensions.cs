// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.PrivateAnalyzers;

internal static class TaskExtensions
{
    public static Task<T?> AsNullable<T>(this Task<T> task)
        where T : class
        => task!;
}
