// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Api;

namespace Microsoft.CodeAnalysis.Remote.Razor;

/// <summary>
/// An abstraction to avoid calling the static <see cref="RazorBrokeredServiceImplementation"/> helper defined in Roslyn.
/// </summary>
internal interface IRazorBrokeredServiceInterceptor
{
    ValueTask RunServiceAsync(
        Func<CancellationToken, ValueTask> implementation,
        CancellationToken cancellationToken);

    ValueTask<T> RunServiceAsync<T>(
        RazorPinnedSolutionInfoWrapper solutionInfo,
        Func<Solution, ValueTask<T>> implementation,
        CancellationToken cancellationToken);
}
