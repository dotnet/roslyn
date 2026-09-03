// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Templates.Editorconfig.Wizard.Utilities;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Templates.Editorconfig.Wizard.Logging;

public class TelemetrySessionAggregator
{
    private readonly TelemetrySession _session;

    public TelemetrySessionAggregator(TelemetrySession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

#if DEBUG
    public bool IsOptedIn => true;
#else
    public bool IsOptedIn => _session.IsOptedIn;
#endif

    public TelemetryScope<UserTaskEvent> StartUserTask(string eventName)
    {
#if DEBUG
        _ = WriteLineToOutputWindowAsync($"Started User Task: {eventName}");
#endif
        return _session.StartUserTask(eventName);
    }

    public TelemetryScope<OperationEvent> StartOperation(string eventName)
    {
#if DEBUG
        _ = WriteLineToOutputWindowAsync($"Started Operation: {eventName}");
#endif
        return _session.StartOperation(eventName);
    }

    public void PostEvent(TelemetryEvent telemetryEvent)
    {
#if DEBUG
        _ = WriteTelemetryEventToOutputWindowAsync(telemetryEvent, "Event");
#endif
        _session.PostEvent(telemetryEvent);
    }

    public TelemetryEventCorrelation PostFault(string eventName, string description)
    {
#if DEBUG
        _ = WriteLineToOutputWindowAsync($"Posted Fault: '{eventName}'");
        _ = WriteLineToOutputWindowAsync($"              {description}");
#endif
        return _session.PostFault(eventName, description);
    }

    public TelemetryEventCorrelation PostFault(string eventName, string description, FaultSeverity faultSeverity, Exception exceptionObject)
    {
#if DEBUG
        _ = WriteLineToOutputWindowAsync($"Exception: '{eventName}'");
        _ = WriteLineToOutputWindowAsync($"           {description}");
        _ = WriteLineToOutputWindowAsync($"           {Enum.GetName(typeof(FaultSeverity), faultSeverity)}");
        _ = WriteLineToOutputWindowAsync($"           {exceptionObject}");
#endif
        return _session.PostFault(eventName, description, faultSeverity, exceptionObject);
    }

    public void EndUserTask(TelemetryScope<UserTaskEvent> userTask, TelemetryResult telemetryResult)
    {
#if DEBUG
        _ = WriteTelemetryEventToOutputWindowAsync(userTask.EndEvent, "Ended User Task");
#endif

        userTask.End(telemetryResult);
    }

    public void EndOperation(TelemetryScope<OperationEvent> operation, TelemetryResult telemetryResult)
    {
#if DEBUG
        _ = WriteTelemetryEventToOutputWindowAsync(operation.EndEvent, "Ended Operation");
#endif
        operation.End(telemetryResult);
    }

#if DEBUG
    public async Task WriteTelemetryEventToOutputWindowAsync(TelemetryEvent telemetryEvent, string descrption)
    {
        await WriteLineToOutputWindowAsync($"{descrption}: {telemetryEvent}");
        if (telemetryEvent.HasProperties)
        {
            foreach (var kvp in telemetryEvent.Properties)
            {
                _ = WriteLineToOutputWindowAsync($"       {kvp.Key} : {kvp.Value}");

            }
        }
    }


    public async Task WriteLineToOutputWindowAsync(string value)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        await EnsurePaneAsync();
        if (_outputWindowPane == null)
        {
            throw new InvalidOperationException("IVsOutputWindowPane should exist");
        }

        if (_outputWindowPane is IVsOutputWindowPaneNoPump vsOutputWindowPaneNoPump)
        {
            vsOutputWindowPaneNoPump.OutputStringNoPump(value + Environment.NewLine);
        }
        else
        {
            ErrorHandler.ThrowOnFailure(_outputWindowPane!.OutputStringThreadSafe(value + Environment.NewLine));
        }
    }

    private static IVsOutputWindowPane? _outputWindowPane;

    private static async Task EnsurePaneAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        if (_outputWindowPane is null)
        {
            var outputWindow = VSHelpers.GetService<SVsOutputWindow, IVsOutputWindow>();
            Guid paneGuid = new("5cc878b5-288f-4b48-a0f6-371173b4cfdb");
            const string paneName = "New Editorconfig Command";
            const int visible = 1;
            const int clearWithSolution = 1;
            ErrorHandler.ThrowOnFailure(outputWindow.CreatePane(ref paneGuid, paneName, visible, clearWithSolution));
            ErrorHandler.ThrowOnFailure(outputWindow.GetPane(ref paneGuid, out _outputWindowPane));
        }
    }
#endif
}

