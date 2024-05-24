// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Telemetry;

/// <summary>
/// A logger that publishes events to a log file.
/// </summary>
internal sealed class FileLogger : ILogger
{
    private readonly object _gate;
    private readonly string _logFilePath;
    private readonly StringBuilder _buffer;
    private bool _enabled;

    /// <summary>
    /// Task queue to serialize all the IO to the log file.
    /// </summary>
    private readonly TaskQueue _taskQueue;

    public FileLogger(IGlobalOptionService globalOptions, string logFilePath)
    {
        _logFilePath = logFilePath;
        _gate = new();
        _buffer = new();
        _taskQueue = new(AsynchronousOperationListenerProvider.NullListener, TaskScheduler.Default);
        _enabled = globalOptions.GetOption(VisualStudioLoggingOptionsStorage.EnableFileLoggingForDiagnostics);
        globalOptions.AddOptionChangedHandler(this, OptionService_OptionChanged);
    }

    public FileLogger(IGlobalOptionService optionService)
        : this(optionService, Path.Combine(Path.GetTempPath(), "Roslyn", "Telemetry", GetLogFileName()))
    {
    }

    private static string GetLogFileName()
        => DateTime.Now.ToString(CultureInfo.InvariantCulture).Replace(' ', '_').Replace('/', '_').Replace(':', '_') + ".log";

    private void OptionService_OptionChanged(object? sender, OptionChangedEventArgs e)
    {
        if (e.Option == VisualStudioLoggingOptionsStorage.EnableFileLoggingForDiagnostics)
        {
            Contract.ThrowIfNull(e.Value);

            _enabled = (bool)e.Value;
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
    {
        _taskQueue.ScheduleTask(nameof(FileLogger), () =>
        {
            lock (_gate)
            {
                _buffer.AppendLine($"{DateTime.Now} ({functionId}) : {message}");

                IOUtilities.PerformIO(() =>
                {
                    if (!File.Exists(_logFilePath))
                    {
                        Directory.CreateDirectory(PathUtilities.GetDirectoryName(_logFilePath));
                    }

                    File.AppendAllText(_logFilePath, _buffer.ToString());
                    _buffer.Clear();
                });
            }
        }, CancellationToken.None);
    }

    public void Log(FunctionId functionId, LogMessage logMessage)
        => Log(functionId, logMessage.GetMessage());

    public void LogBlockStart(FunctionId functionId, LogMessage logMessage, int uniquePairId, CancellationToken cancellationToken)
        => LogBlockEvent(functionId, logMessage, uniquePairId, "BlockStart");

    public void LogBlockEnd(FunctionId functionId, LogMessage logMessage, int uniquePairId, int delta, CancellationToken cancellationToken)
        => LogBlockEvent(functionId, logMessage, uniquePairId, cancellationToken.IsCancellationRequested ? "BlockCancelled" : "BlockEnd");

    private void LogBlockEvent(FunctionId functionId, LogMessage logMessage, int uniquePairId, string blockEvent)
        => Log(functionId, $"[{blockEvent} - {uniquePairId}] {logMessage.GetMessage()}");
}
