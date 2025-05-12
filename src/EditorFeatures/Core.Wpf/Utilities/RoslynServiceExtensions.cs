// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.Shell;

internal static class RoslynServiceExtensions
{
    /// <inheritdoc cref="RoslynServiceExtensions.GetService{TService, TInterface}(IServiceProvider, JoinableTaskFactory, bool)"/>
    public static TInterface GetServiceOnMainThread<TService, TInterface>(this IServiceProvider serviceProvider)
    {
        var service = serviceProvider.GetService(typeof(TService));
        if (service is null)
            throw new ServiceUnavailableException(typeof(TService));

        if (service is not TInterface @interface)
            throw new ServiceUnavailableException(typeof(TInterface));

        return @interface;
    }

    /// <summary>
    /// Gets a service interface from a service provider.
    /// </summary>
    /// <typeparam name="TService">The service type</typeparam>
    /// <typeparam name="TInterface">The interface type</typeparam>
    /// <param name="serviceProvider">The service provider</param>
    /// <returns>The requested service interface. Never <see langword="null"/>.</returns>
    /// <exception cref="ServiceUnavailableException">
    /// Either the service could not be acquired, or the service does not support
    /// the requested interface.
    /// </exception>
    public static TInterface GetService<TService, TInterface>(
        this IServiceProvider serviceProvider,
        JoinableTaskFactory joinableTaskFactory)
        where TInterface : class
    {
        return GetService<TService, TInterface>(serviceProvider, joinableTaskFactory, throwOnFailure: true)!;
    }

    /// <summary>
    /// Gets a service interface from a service provider.
    /// </summary>
    /// <typeparam name="TService">The service type</typeparam>
    /// <typeparam name="TInterface">The interface type</typeparam>
    /// <param name="serviceProvider">The service provider</param>
    /// <param name="throwOnFailure">
    /// Determines how a failure to get the requested service interface is handled. If <see langword="true"/>, an
    /// exception is thrown; if <see langword="false"/>, <see langword="null"/> is returned.
    /// </param>
    /// <returns>The requested service interface, if it could be obtained; otherwise <see langword="null"/> if
    /// <paramref name="throwOnFailure"/> is <see langword="false"/>.</returns>
    /// <exception cref="ServiceUnavailableException">
    /// Either the service could not be acquired, or the service does not support
    /// the requested interface.
    /// </exception>
    [SuppressMessage("Usage", "VSTHRD102:Implement internal logic asynchronously", Justification = "Intentional to match required semantics for IServiceProvider.GetService")]
    public static TInterface? GetService<TService, TInterface>(
        this IServiceProvider serviceProvider,
        JoinableTaskFactory joinableTaskFactory,
        bool throwOnFailure)
        where TInterface : class
    {
        Requires.NotNull(serviceProvider, nameof(serviceProvider));

        return joinableTaskFactory.Run(async () =>
        {
            await joinableTaskFactory.SwitchToMainThreadAsync();

            var service = serviceProvider.GetService(typeof(TService));
            if (service is null)
            {
                if (throwOnFailure)
                    throw new ServiceUnavailableException(typeof(TService));

                return null;
            }

            if (service is not TInterface @interface)
            {
                if (throwOnFailure)
                    throw new ServiceUnavailableException(typeof(TInterface));

                return null;
            }

            return @interface;
        });
    }
}
