// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

/// <summary>
/// An implementation of <see cref="IRemoteServiceInvoker"/> that doesn't actually do anything remote,
/// but rather directly calls service methods.
/// </summary>
internal sealed class TestRemoteServiceInvoker(
    JoinableTaskContext joinableTaskContext,
    ExportProvider exportProvider,
    ILoggerFactory loggerFactory) : IRemoteServiceInvoker, IDisposable
{
    private readonly TestBrokeredServiceInterceptor _serviceInterceptor = new();
    private readonly Dictionary<Type, object> _services = [];
    private readonly ReentrantSemaphore _reentrantSemaphore = ReentrantSemaphore.Create(initialCount: 1, joinableTaskContext);

    private async Task<TService> GetOrCreateServiceAsync<TService>()
        where TService : class
    {
        return await _reentrantSemaphore.ExecuteAsync(async () =>
        {
            if (!_services.TryGetValue(typeof(TService), out var service))
            {
                service = await BrokeredServiceFactory.CreateServiceAsync<TService>(_serviceInterceptor, exportProvider, loggerFactory);
                _services.Add(typeof(TService), service);
            }

            return (TService)service;
        });
    }

    public async ValueTask<TResult?> TryInvokeAsync<TService, TResult>(
        Solution solution,
        Func<TService, RazorPinnedSolutionInfoWrapper, CancellationToken, ValueTask<TResult>> invocation,
        CancellationToken cancellationToken,
        [CallerFilePath] string? callerFilePath = null,
        [CallerMemberName] string? callerMemberName = null)
        where TService : class
    {
        var service = await GetOrCreateServiceAsync<TService>();

        var solutionInfo = await _serviceInterceptor.GetSolutionInfoAsync(solution, cancellationToken);
        return await invocation(service, solutionInfo, cancellationToken);
    }

    public void MapSolutionIdToRemote(SolutionId localSolutionId, Solution remoteSolution)
    {
        _serviceInterceptor.MapSolutionIdToRemote(localSolutionId, remoteSolution);
    }

    public void Dispose()
    {
        _reentrantSemaphore.Dispose();

        foreach (var service in _services.Values)
        {
            if (service is IDisposable d)
            {
                d.Dispose();
            }
        }
    }
}
