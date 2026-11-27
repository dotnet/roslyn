// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;

namespace Microsoft.CodeAnalysis.Razor.Utilities;

// NOTE: This code is copied and modified slightly from dotnet/roslyn:
// https://github.com/dotnet/roslyn/blob/98cd097bf122677378692ebe952b71ab6e5bb013/src/Workspaces/Core/Portable/Shared/Utilities/AsyncBatchingWorkQueue%600.cs

/// <inheritdoc cref="AsyncBatchingWorkQueue{TItem, TResult}"/>
internal class AsyncBatchingWorkQueue(
    TimeSpan delay,
    Func<CancellationToken, ValueTask> processBatchAsync,
    CancellationToken cancellationToken) : AsyncBatchingWorkQueue<VoidResult>(delay, Convert(processBatchAsync), EqualityComparer<VoidResult>.Default, cancellationToken)
{
    private static Func<ImmutableArray<VoidResult>, CancellationToken, ValueTask> Convert(Func<CancellationToken, ValueTask> processBatchAsync)
        => (items, ct) => processBatchAsync(ct);

    public void AddWork(bool cancelExistingWork = false)
        => AddWork(default(VoidResult), cancelExistingWork);
}
