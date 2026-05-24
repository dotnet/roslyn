// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudioCode.RazorExtension.Services;

[Shared]
[Export(typeof(IRemoteServiceInvoker))]
[method: ImportingConstructor]
internal class VSCodeRemoteServiceInvoker(
    IWorkspaceProvider workspaceProvider,
    ILoggerFactory loggerFactory) : IRemoteServiceInvoker, IDisposable
{
    private readonly IWorkspaceProvider _workspaceProvider = workspaceProvider;
    private readonly ILoggerFactory _loggerFactory = loggerFactory;
    private readonly Dictionary<Type, object> _services = [];
    private readonly Lock _serviceLock = new();
    private readonly VSCodeBrokeredServiceInterceptor _serviceInterceptor = new();

    public async ValueTask<TResult?> TryInvokeAsync<TService, TResult>(
        Solution solution,
        Func<TService, RazorPinnedSolutionInfoWrapper, CancellationToken, ValueTask<TResult>> invocation,
        CancellationToken cancellationToken,
        [CallerFilePath] string? callerFilePath = null,
        [CallerMemberName] string? callerMemberName = null) where TService : class
    {
        var service = await GetOrCreateServiceAsync<TService>().ConfigureAwait(false);

        if (service == null)
        {
            // Service not available
            return default;
        }

        // Get solution info with direct reference stored
        var solutionInfo = new RazorPinnedSolutionInfoWrapper(checksum: default, solution: solution);

        // Invoke the function with the service and solution info
        return await invocation(service, solutionInfo, cancellationToken).ConfigureAwait(false);
    }

    private async Task<TService?> GetOrCreateServiceAsync<TService>() where TService : class
    {
        lock (_serviceLock)
        {
            if (_services.TryGetValue(typeof(TService), out var existingService))
            {
                return (TService)existingService;
            }
        }

        // Create the service using the InProcServiceFactory
        try
        {
            var service = await InProcServiceFactory.CreateServiceAsync<TService>(_serviceInterceptor, _workspaceProvider, _loggerFactory).ConfigureAwait(false);

            lock (_serviceLock)
            {
                _services[typeof(TService)] = service;
            }

            return service;
        }
        catch (Exception)
        {
            // If service creation fails, return null
            return null;
        }
    }

    public void Dispose()
    {
        lock (_serviceLock)
        {
            foreach (var service in _services.Values)
            {
                if (service is IDisposable d)
                {
                    d.Dispose();
                }
            }

            _services.Clear();
        }
    }
}
