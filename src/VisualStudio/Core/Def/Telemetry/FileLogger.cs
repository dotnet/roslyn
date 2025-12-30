// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Threading;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Telemetry;

/// <summary>
/// A logger that publishes events to a log file.
/// </summary>
internal sealed class FileLogger : ILogger
{
    private readonly string _logFilePath;
    private bool _enabled;

    /// <summary>
    /// Work queue to serialize all the IO to the log file.
    /// </summary>
    private readonly AsyncBatchingWorkQueue<(FunctionId functionId, string message)> _workQueue;

    public FileLogger(IGlobalOptionService optionService, IThreadingContext threadingContext)
    {
        _logFilePath = Path.Combine(Path.GetTempPath(), "Roslyn", "Telemetry", GetLogFileName());
        _workQueue = new(
            DelayTimeSpan.Short,
            ProcessWorkQueueAsync,
            AsynchronousOperationListenerProvider.NullListener,
            threadingContext.DisposalToken);
        _enabled = optionService.GetOption(VisualStudioLoggingOptionsStorage.EnableFileLoggingForDiagnostics);
        optionService.AddOptionChangedHandler(this, OptionService_OptionChanged);
    }

    private static string GetLogFileName()
        => DateTime.Now.ToString(CultureInfo.InvariantCulture).Replace(' ', '_').Replace('/', '_').Replace(':', '_') + ".log";

    private void OptionService_OptionChanged(object sender, object target, OptionChangedEventArgs e)
    {
        foreach (var (key, newValue) in e.ChangedOptions)
        {
            if (key.Option.Equals(VisualStudioLoggingOptionsStorage.EnableFileLoggingForDiagnostics))
            {
                Contract.ThrowIfNull(newValue);
                _enabled = (bool)newValue;
            }
        }
    }

    public bool IsEnabled(FunctionId functionId)
    {
        if (!_enabled)
        {
            return false;
        }

        // Limit logged function IDs to keep a reasonable log file size.
        var str = functionId.ToString();
        return str.StartsWith("Diagnostic") ||
            str.StartsWith("CodeAnalysisService") ||
            str.StartsWith("Workspace") ||
            str.StartsWith("WorkCoordinator") ||
            str.StartsWith("IncrementalAnalyzerProcessor") ||
            str.StartsWith("ExternalErrorDiagnosticUpdateSource");
    }

    private void Log(FunctionId functionId, string message)
        => _workQueue.AddWork((functionId, message));

    public void Log(FunctionId functionId, LogMessage logMessage)
        => Log(functionId, logMessage.GetMessage());

    public void LogBlockStart(FunctionId functionId, LogMessage logMessage, int uniquePairId, CancellationToken cancellationToken)
        => LogBlockEvent(functionId, logMessage, uniquePairId, "BlockStart");

    public void LogBlockEnd(FunctionId functionId, LogMessage logMessage, int uniquePairId, int delta, CancellationToken cancellationToken)
        => LogBlockEvent(functionId, logMessage, uniquePairId, cancellationToken.IsCancellationRequested ? "BlockCancelled" : "BlockEnd");

    private void LogBlockEvent(FunctionId functionId, LogMessage logMessage, int uniquePairId, string blockEvent)
        => Log(functionId, $"[{blockEvent} - {uniquePairId}] {logMessage.GetMessage()}");

    private async ValueTask ProcessWorkQueueAsync(
        ImmutableSegmentedList<(FunctionId functionId, string message)> list, CancellationToken cancellationToken)
    {
        using var _ = PooledStringBuilder.GetInstance(out var buffer);
        foreach (var (functionId, message) in list)
            buffer.AppendLine($"{DateTime.Now} ({functionId}) : {message}");

        IOUtilities.PerformIO(() =>
        {
            if (!File.Exists(_logFilePath))
            {
                Directory.CreateDirectory(PathUtilities.GetDirectoryName(_logFilePath));
            }

            File.AppendAllText(_logFilePath, buffer.ToString());
        });
    }
}
