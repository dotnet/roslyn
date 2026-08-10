// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.Remote;

namespace Microsoft.CodeAnalysis.Remote.Razor;

/// <summary>
/// An abstraction for hosts that intercept Razor brokered-service calls instead of routing them through Roslyn OOP.
/// </summary>
internal interface IRazorBrokeredServiceInterceptor
{
    ValueTask RunServiceAsync(
        Func<CancellationToken, ValueTask> implementation,
        CancellationToken cancellationToken);

    ValueTask<T> RunServiceAsync<T>(
        RazorSolutionWrapper solutionInfo,
        Func<Solution, ValueTask<T>> implementation,
        CancellationToken cancellationToken);
}
