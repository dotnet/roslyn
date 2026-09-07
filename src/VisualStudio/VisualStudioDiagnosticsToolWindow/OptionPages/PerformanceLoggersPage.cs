// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.LanguageServices.Implementation;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;
using Microsoft.VisualStudio.LanguageServices.Telemetry;
using Roslyn.Utilities;

namespace Roslyn.VisualStudio.DiagnosticsWindow.OptionsPages;

[Guid(Guids.RoslynOptionPagePerformanceLoggersIdString)]
internal sealed class PerformanceLoggersPage : AbstractOptionPage
{
    private IGlobalOptionService _globalOptions;
    private IThreadingContext _threadingContext;
    private SolutionServices _workspaceServices;

    private static IDisposable s_etwRegistration;
    private static IDisposable s_traceRegistration;
    private static IDisposable s_outputWindowRegistration;

    protected override AbstractOptionPageControl CreateOptionPage(IServiceProvider serviceProvider, OptionStore optionStore)
    {
        if (_globalOptions == null)
        {
            var componentModel = (IComponentModel)serviceProvider.GetService(typeof(SComponentModel));

            _globalOptions = componentModel.GetService<IGlobalOptionService>();
            _threadingContext = componentModel.GetService<IThreadingContext>();

            var workspace = componentModel.GetService<VisualStudioWorkspace>();
            _workspaceServices = workspace.Services.SolutionServices;
        }

        return new InternalOptionsControl(FunctionIdOptions.GetOptions(), optionStore);
    }

    protected override void OnApply(PageApplyEventArgs e)
    {
        base.OnApply(e);

        SetLoggers(_globalOptions, _threadingContext, _workspaceServices);
    }

    public static void SetLoggers(IGlobalOptionService globalOptions, IThreadingContext threadingContext, SolutionServices workspaceServices)
    {
        var isEnabled = FunctionIdOptions.CreateFunctionIsEnabledPredicate(globalOptions);

        var etwEnabled = globalOptions.GetOption(LoggerOptionsStorage.EtwLoggerKey);
        var traceEnabled = globalOptions.GetOption(LoggerOptionsStorage.TraceLoggerKey);
        var outputWindowEnabled = globalOptions.GetOption(LoggerOptionsStorage.OutputWindowLoggerKey);

        // These sinks exist only for this page, so each is registered while enabled and unregistered
        // when not. isEnabled is a snapshot of the per-FunctionId options, which is why a fresh sink is
        // built on every apply.
        Register(ref s_etwRegistration, etwEnabled, () => new EtwEventSink(isEnabled));
        Register(ref s_traceRegistration, traceEnabled, () => new TraceEventSink(isEnabled));
        Register(ref s_outputWindowRegistration, outputWindowEnabled, () => new OutputWindowEventSink(isEnabled));

        // update loggers in remote process
        var client = threadingContext.JoinableTaskFactory.Run(() => RemoteHostClient.TryGetClientAsync(workspaceServices, CancellationToken.None));
        if (client != null)
        {
            var loggerTypeNames = ImmutableArray<string>.Empty;
            if (etwEnabled)
                loggerTypeNames = loggerTypeNames.Add(nameof(EtwEventSink));
            if (traceEnabled)
                loggerTypeNames = loggerTypeNames.Add(nameof(TraceEventSink));

            var functionIds = Enum.GetValues<FunctionId>().WhereAsArray(isEnabled);

            threadingContext.JoinableTaskFactory.Run(async () => _ = await client.TryInvokeAsync<IRemoteProcessTelemetryService>(
                (service, cancellationToken) => service.EnableLoggingAsync(loggerTypeNames, functionIds, cancellationToken),
                CancellationToken.None).ConfigureAwait(false));
        }

        static void Register(ref IDisposable registration, bool enabled, Func<IEventSink> create)
        {
            Interlocked.Exchange(ref registration, enabled ? RoslynTelemetry.AddEventSink(create()) : null)?.Dispose();
        }
    }
}
