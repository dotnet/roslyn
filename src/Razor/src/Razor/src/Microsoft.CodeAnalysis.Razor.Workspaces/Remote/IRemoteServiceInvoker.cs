// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Razor.Remote;

internal interface IRemoteServiceInvoker
{
    ValueTask<TResult?> TryInvokeAsync<TService, TResult>(
        Func<TService, CancellationToken, ValueTask<TResult>> invocation,
        CancellationToken cancellationToken,
        [CallerFilePath] string? callerFilePath = null,
        [CallerMemberName] string? callerMemberName = null)
        where TService : class;

    ValueTask<TResult?> TryInvokeAsync<TService, TResult>(
        Solution solution,
        Func<TService, RazorSolutionWrapper, CancellationToken, ValueTask<TResult>> invocation,
        CancellationToken cancellationToken,
        [CallerFilePath] string? callerFilePath = null,
        [CallerMemberName] string? callerMemberName = null)
        where TService : class;
}
