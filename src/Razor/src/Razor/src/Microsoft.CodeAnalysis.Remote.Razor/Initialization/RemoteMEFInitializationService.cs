// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Api;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.ServiceHub.Framework;
using static Microsoft.CodeAnalysis.Remote.Razor.RazorBrokeredServiceBase;

namespace Microsoft.CodeAnalysis.Remote.Razor;

/// <summary>
/// A special service that is used to initialize the MEF composition for Razor in the remote host.
/// </summary>
/// <remarks>
/// It's special because it doesn't use MEF. Nor can it use anything else really.
/// </remarks>
internal sealed class RemoteMEFInitializationService : IRemoteMEFInitializationService
{
    internal sealed class Factory : FactoryBase<IRemoteMEFInitializationService>
    {
        protected override Task<object> CreateInternalAsync(Stream? stream, IServiceProvider hostProvidedServices, IServiceBroker? serviceBroker)
        {
            var traceSource = (TraceSource?)hostProvidedServices.GetService(typeof(TraceSource));

            var service = new RemoteMEFInitializationService();
            if (stream is not null)
            {
                var serverConnection = CreateServerConnection(stream, traceSource);
                ConnectService(serverConnection, service);
            }

            return Task.FromResult<object>(service);
        }

        protected override IRemoteMEFInitializationService CreateService(in ServiceArgs args)
            => Assumed.Unreachable<IRemoteMEFInitializationService>("This service overrides CreateInternalAsync to avoid MEF instatiation, so the CreateService method should never be called.");
    }

    public ValueTask InitializeAsync(string cacheDirectory, CancellationToken cancellationToken)
    {
        return RazorBrokeredServiceImplementation.RunServiceAsync(_ =>
          {
              RemoteMefComposition.CacheDirectory = cacheDirectory;
              return new();
          }, cancellationToken);
    }
}
