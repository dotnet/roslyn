// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.Remote;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

[Export(typeof(ExportableRemoteServiceInvoker))]
[Export(typeof(IRemoteServiceInvoker))]
[PartNotDiscoverable]
internal class ExportableRemoteServiceInvoker : IRemoteServiceInvoker
{
    private IRemoteServiceInvoker? _remoteServiceInvoker;

    internal void SetInvoker(IRemoteServiceInvoker remoteServiceInvoker)
    {
        _remoteServiceInvoker = remoteServiceInvoker;
    }

    public ValueTask<TResult?> TryInvokeAsync<TService, TResult>(Solution solution, Func<TService, RazorPinnedSolutionInfoWrapper, CancellationToken, ValueTask<TResult>> invocation, CancellationToken cancellationToken, [CallerFilePath] string? callerFilePath = null, [CallerMemberName] string? callerMemberName = null) where TService : class
        => _remoteServiceInvoker.AssumeNotNull().TryInvokeAsync(solution, invocation, cancellationToken, callerFilePath, callerMemberName);
}
