// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Design;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Options;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options;

internal abstract class UnifiedSettingsPackage : AsyncPackage
{
    private IComponentModel _componentModel = null!;
    private INamingSettingsService _namingService = null!;
    private IDisposable? _codeStyleCommands;
    private MenuCommand? _resetCommand;
    private IMenuCommandService? _commandService;
    protected IGlobalOptionService GlobalOptions { get; private set; } = null!;
    protected IThreadingContext ThreadingContext { get; private set; } = null!;
    protected abstract string Language { get; }
    protected abstract Guid CommandSet { get; }
    protected abstract Guid PreviewFactory { get; }
    protected abstract Type ProviderServiceType { get; }
    protected abstract Type MigratorServiceType { get; }
    protected abstract CodeStyleSettingsPreviewFactory CreatePreviewFactory();

    protected sealed override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        (_componentModel, _namingService, GlobalOptions, ThreadingContext) = await GetMefServicesAsync(this, cancellationToken).ConfigureAwait(false);
        // Register callbacks before startup reads arrays; initial migration can load this very package.
        AddService(ProviderServiceType, async (_, token, _) => await _namingService.GetProviderAsync(Language, token).ConfigureAwait(false), promote: false);
        AddService(MigratorServiceType, async (_, token, _) => await _namingService.GetCoordinatorAsync(Language, token).ConfigureAwait(false), promote: false);

        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        var registrar = await GetServiceAsync(typeof(SVsUIFactory)).ConfigureAwait(true) as IVsRegisterUIFactories ?? throw new InvalidOperationException();
        var factoryId = PreviewFactory;
        var factory = CreatePreviewFactory();
        ErrorHandler.ThrowOnFailure(registrar.RegisterUIFactory(ref factoryId, factory));
        _commandService = await GetServiceAsync(typeof(IMenuCommandService)).ConfigureAwait(true) as IMenuCommandService ?? throw new InvalidOperationException();
        _codeStyleCommands = CodeStyleSettingsCommands.Register(_commandService, CommandSet,
            () => CodeStyleSettingsCommands.GenerateEditorConfig(this, factory.CreatePendingStore(), Language),
            CodeStyleSettingsCommands.ShowHelp, DisposalToken);
        _resetCommand = new MenuCommand((_, _) =>
        {
            var notificationService = _componentModel.GetService<VisualStudioWorkspace>().Services.GetRequiredService<INotificationService>();
            if (!ConfirmReset(notificationService))
                return;
            JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    await _namingService.ResetAsync(Language, DisposalToken).ConfigureAwait(false);
                }
                catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, DisposalToken))
                {
                    await JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);
                    notificationService.SendNotification(string.Format(ServicesVSResources.Unable_to_update_naming_settings_0, e.Message), severity: NotificationSeverity.Error);
                }
            }).Task.ReportNonFatalErrorUnlessCancelledAsync(DisposalToken);
        }, new CommandID(CommandSet, 258));
        _commandService.AddCommand(_resetCommand);
        _namingService.Start();
    }

    internal static async Task<(IComponentModel ComponentModel, INamingSettingsService NamingService, IGlobalOptionService GlobalOptions, IThreadingContext ThreadingContext)> GetMefServicesAsync(
        IAsyncServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var componentModel = (IComponentModel?)await serviceProvider.GetServiceAsync(typeof(SComponentModel)).ConfigureAwait(false);
        Assumes.Present(componentModel);
        await TaskScheduler.Default;
        cancellationToken.ThrowIfCancellationRequested();
        return (componentModel, componentModel.GetService<INamingSettingsService>(), componentModel.GetService<IGlobalOptionService>(), componentModel.GetService<IThreadingContext>());
    }

    internal static bool ConfirmReset(INotificationService notificationService)
        => notificationService.ConfirmMessageBox(
            ServicesVSResources.Reset_naming_preferences_confirmation,
            ServicesVSResources.Reset_naming_preferences,
            NotificationSeverity.Warning);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _codeStyleCommands?.Dispose();
            if (_resetCommand is not null)
                _commandService?.RemoveCommand(_resetCommand);
        }
        base.Dispose(disposing);
    }
}
