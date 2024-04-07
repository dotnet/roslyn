// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

extern alias InteractiveHost;

using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.InteractiveWindow.Shell;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using InteractiveHostFatalError = InteractiveHost::Microsoft.CodeAnalysis.ErrorReporting.FatalError;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.Interactive;

internal abstract partial class VsInteractiveWindowPackage<TVsInteractiveWindowProvider> : AsyncPackage, IVsToolWindowFactory
    where TVsInteractiveWindowProvider : VsInteractiveWindowProvider
{
    protected virtual void InitializeMenuCommands(OleMenuCommandService menuCommandService)
    {
    }

    protected abstract Guid LanguageServiceGuid { get; }
    protected abstract Guid ToolWindowId { get; }

    private IComponentModel _componentModel;
    private TVsInteractiveWindowProvider _interactiveWindowProvider;

    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        await base.InitializeAsync(cancellationToken, progress).ConfigureAwait(true);

        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var shell = (IVsShell)await GetServiceAsync(typeof(SVsShell)).ConfigureAwait(true);
        _componentModel = (IComponentModel)await GetServiceAsync(typeof(SComponentModel)).ConfigureAwait(true);
        var menuCommandService = (OleMenuCommandService)await GetServiceAsync(typeof(IMenuCommandService)).ConfigureAwait(true);
        cancellationToken.ThrowIfCancellationRequested();
        Assumes.Present(shell);
        Assumes.Present(_componentModel);
        Assumes.Present(menuCommandService);

        // Set both handlers to non-fatal Watson. Never fail-fast the VS process.
        // Any exception that is not recovered from shall be propagated.
        FaultReporter.InitializeFatalErrorHandlers();
        FatalError.CopyHandlersTo(typeof(InteractiveHostFatalError).Assembly);

        // Explicitly set up FatalError handlers for the InteractiveWindowPackage.
        Action<Exception> fatalHandler = e => FaultReporter.ReportFault(e, VisualStudio.Telemetry.FaultSeverity.Critical, forceDump: false);
        Action<Exception> nonFatalHandler = e => FaultReporter.ReportFault(e, VisualStudio.Telemetry.FaultSeverity.General, forceDump: false);

        SetErrorHandlers(typeof(IInteractiveWindow).Assembly, fatalHandler, nonFatalHandler);
        SetErrorHandlers(typeof(IVsInteractiveWindow).Assembly, fatalHandler, nonFatalHandler);

        _interactiveWindowProvider = _componentModel.DefaultExportProvider.GetExportedValue<TVsInteractiveWindowProvider>();

        InitializeMenuCommands(menuCommandService);
    }

    private static void SetErrorHandlers(Assembly assembly, Action<Exception> fatalHandler, Action<Exception> nonFatalHandler)
    {
        var type = assembly.GetType("Microsoft.VisualStudio.InteractiveWindow.FatalError", throwOnError: true).GetTypeInfo();

        var handlerSetter = type.GetDeclaredMethod("set_Handler");
        var nonFatalHandlerSetter = type.GetDeclaredMethod("set_NonFatalHandler");

        handlerSetter.Invoke(null, [fatalHandler]);
        nonFatalHandlerSetter.Invoke(null, [nonFatalHandler]);
    }

    protected TVsInteractiveWindowProvider InteractiveWindowProvider
    {
        get { return _interactiveWindowProvider; }
    }

    /// <summary>
    /// When a VSPackage supports multi-instance tool windows, each window uses the same rguidPersistenceSlot.
    /// The dwToolWindowId parameter is used to differentiate between the various instances of the tool window.
    /// </summary>
    int IVsToolWindowFactory.CreateToolWindow(ref Guid rguidPersistenceSlot, uint id)
    {
        if (rguidPersistenceSlot == ToolWindowId)
        {
            _interactiveWindowProvider.Create((int)id);
            return VSConstants.S_OK;
        }

        return VSConstants.E_FAIL;
    }
}
