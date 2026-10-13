// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.LanguageServices.Options;
using Microsoft.VisualStudio.RpcContracts;
using Microsoft.VisualStudio.RpcContracts.UnifiedSettings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.ServiceBroker;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options;

[Export(typeof(INativeAdvancedSettingsService))]
internal sealed class NativeAdvancedSettingsService : INativeAdvancedSettingsService
{
    private readonly IThreadingContext _threadingContext;
    private readonly IVsService<IBrokeredServiceContainer> _container;
    private readonly Lazy<ILegacyGlobalOptionService> _globalOptions;
    private int _started;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public NativeAdvancedSettingsService(IThreadingContext threadingContext,
        IVsService<SVsBrokeredServiceContainer, IBrokeredServiceContainer> container,
        Lazy<ILegacyGlobalOptionService> globalOptions)
    {
        _threadingContext = threadingContext;
        _container = container;
        _globalOptions = globalOptions;
    }

    public void Start(LocalUserRegistryOptionPersister registry)
    {
        if (Interlocked.Exchange(ref _started, 1) != 0)
            return;
        var cancellationToken = _threadingContext.DisposalToken;
        _threadingContext.JoinableTaskFactory.RunAsync(async () =>
        {
            await Task.Yield();
            var container = await _container.GetValueAsync(cancellationToken).ConfigureAwait(false);
            using var client = new ServiceBrokerClient(container.GetFullAccessServiceBroker(), _threadingContext.JoinableTaskFactory);
            using var rental = await client.GetProxyAsync<IUnifiedSettingsService>(VisualStudioServices.VS2022_10.UnifiedSettingsService, cancellationToken).ConfigureAwait(false);
            var native = rental.Proxy ?? throw new InvalidOperationException("Unified settings service is unavailable.");
            using var synchronizer = new NativeAdvancedSettingsSynchronizer(native, CreateBindings(registry), Refresh,
                task => task.ReportNonFatalErrorUnlessCancelledAsync(cancellationToken), _threadingContext.JoinableTaskContext, cancellationToken);
            await synchronizer.InitializeAsync().ConfigureAwait(false);
            await registry.WatchChangesAsync(synchronizer.OnRegistryChanged, cancellationToken).ConfigureAwait(false);
        }).Task.ReportNonFatalErrorUnlessCancelledAsync(cancellationToken);
    }

    internal static ImmutableArray<NativeAdvancedSettingsSynchronizer.Binding> CreateBindings(LocalUserRegistryOptionPersister registry)
    {
        var key = new OptionKey2(VisualStudioLoggingOptionsStorage.EnableFileLoggingForDiagnostics);
        var storage = (VisualStudioOptionStorage.LocalUserProfileStorage)VisualStudioOptionStorage.Storages[key.Option.Definition.ConfigName];
        return [new(key, "languages.dotnet.advanced.enableFileLoggingForDiagnostics",
            () => registry.ReadStoredBoolean(storage.Path, storage.Key),
            value => storage.PersistAsync(registry, key, value))];
    }

    private void Refresh(OptionKey2 key, object? value)
    {
        if (_globalOptions.Value.GlobalOptions.RefreshOption(key, value))
            _globalOptions.Value.UpdateRegisteredWorkspaces();
    }
}
