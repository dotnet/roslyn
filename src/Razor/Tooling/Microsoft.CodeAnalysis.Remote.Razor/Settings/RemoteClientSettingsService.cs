// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.Settings;

namespace Microsoft.CodeAnalysis.Remote.Razor;

internal sealed class RemoteClientSettingsService(in ServiceArgs args) : RazorBrokeredServiceBase(in args), IRemoteClientSettingsService
{
    internal sealed class Factory : FactoryBase<IRemoteClientSettingsService>
    {
        protected override IRemoteClientSettingsService CreateService(in ServiceArgs args)
            => new RemoteClientSettingsService(in args);
    }

    private readonly RemoteClientSettingsManager _clientSettingsManager = args.ExportProvider.GetExportedValue<RemoteClientSettingsManager>();

    public ValueTask UpdateAsync(ClientSettings settings, CancellationToken cancellationToken)
        => RunServiceAsync(
            ct =>
            {
                _clientSettingsManager.Update(settings);
                return default;
            },
            cancellationToken);
}
