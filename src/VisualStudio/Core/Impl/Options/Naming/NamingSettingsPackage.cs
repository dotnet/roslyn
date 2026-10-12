// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Extensions;
using Microsoft.VisualStudio.LanguageServices.Options;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options;

internal abstract class NamingSettingsPackage : AsyncPackage
{
    private readonly List<MenuCommand> _commands = [];
    private IMenuCommandService? _commandService;
    protected abstract string Language { get; }
    protected abstract Guid CommandSet { get; }
    protected abstract Type ProviderServiceType { get; }
    protected abstract Type MigratorServiceType { get; }

    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        var service = this.GetMefService<INamingSettingsService>();
        // Register callbacks before startup reads arrays; initial migration can load this very package.
        AddService(ProviderServiceType, async (_, token, _) => await service.GetProviderAsync(Language, token).ConfigureAwait(false), promote: false);
        AddService(MigratorServiceType, async (_, token, _) => await service.GetCoordinatorAsync(Language, token).ConfigureAwait(false), promote: false);
        _commandService = await GetServiceAsync(typeof(IMenuCommandService)).ConfigureAwait(true) as IMenuCommandService ?? throw new InvalidOperationException();
        var menuCommand = new MenuCommand((_, _) =>
        {
            var notificationService = this.GetMefService<VisualStudioWorkspace>().Services.GetRequiredService<INotificationService>();
            if (!ConfirmReset(notificationService))
                return;
            JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    await service.ResetAsync(Language, DisposalToken).ConfigureAwait(false);
                }
                catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, DisposalToken))
                {
                    await JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);
                    notificationService.SendNotification(string.Format(ServicesVSResources.Unable_to_update_naming_settings_0, e.Message), severity: NotificationSeverity.Error);
                }
            }).Task.ReportNonFatalErrorUnlessCancelledAsync(DisposalToken);
        }, new CommandID(CommandSet, 258));
        _commands.Add(menuCommand);
        _commandService.AddCommand(menuCommand);
        service.Start();
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
            foreach (var command in _commands)
                _commandService?.RemoveCommand(command);
            _commands.Clear();
        }
        base.Dispose(disposing);
    }
}
