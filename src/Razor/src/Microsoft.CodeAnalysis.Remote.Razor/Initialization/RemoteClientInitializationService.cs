// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.Remote;

namespace Microsoft.CodeAnalysis.Remote.Razor;

internal sealed class RemoteClientInitializationService(in ServiceArgs args) : RazorBrokeredServiceBase(in args), IRemoteClientInitializationService
{
    internal sealed class Factory : FactoryBase<IRemoteClientInitializationService>
    {
        protected override IRemoteClientInitializationService CreateService(in ServiceArgs args)
            => new RemoteClientInitializationService(in args);
    }

    private readonly RemoteLanguageServerFeatureOptions _remoteLanguageServerFeatureOptions = args.ExportProvider.GetExportedValue<RemoteLanguageServerFeatureOptions>();
    private readonly ImmutableArray<ILspLifetimeService> _lspLifetimeServices = args.ExportProvider.GetExportedValues<ILspLifetimeService>().ToImmutableArray();

    public ValueTask InitializeAsync(RemoteClientInitializationOptions options, CancellationToken cancellationToken)
        => RunServiceAsync(ct =>
            {
                _remoteLanguageServerFeatureOptions.SetOptions(options);
                return default;
            },
            cancellationToken);

    public ValueTask InitializeLspAsync(RemoteClientLSPInitializationOptions options, CancellationToken cancellationToken)
        => RunServiceAsync(ct =>
            {
                foreach (var service in _lspLifetimeServices)
                {
                    service.OnLspInitialized(options);
                }

                return default;
            },
            cancellationToken);
}
