// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Diagnostics;
using Roslyn.Utilities;
using MSB = Microsoft.Build;

namespace Microsoft.CodeAnalysis.MSBuild;

internal sealed class MSBuildDiagnosticLogger : MSB.Framework.ILogger
{
    /// <summary>
    /// Maps a build's <see cref="MSB.Framework.BuildEventContext.SubmissionId"/> to the
    /// <see cref="DiagnosticLog"/> that should receive the errors and warnings raised while that
    /// submission is building. This lets a single logger instance route events to the correct project
    /// even when multiple builds are in flight. The id matches
    /// <see cref="MSB.Execution.BuildSubmission.SubmissionId"/>.
    /// </summary>
    private readonly ConcurrentDictionary<int, DiagnosticLog> _logsBySubmissionId = new();

    private MSB.Framework.IEventSource? _eventSource;

    public string? Parameters { get; set; }
    public MSB.Framework.LoggerVerbosity Verbosity { get; set; }

    /// <summary>
    /// Registers the <paramref name="log"/> that should receive diagnostics for the build submission with
    /// the given <paramref name="submissionId"/>. Must be called after the submission's id has been
    /// assigned (i.e. after <see cref="MSB.Execution.BuildManager.PendBuildRequest(MSB.Execution.BuildRequestData)"/>)
    /// but before the submission starts executing, so no event can arrive before the log is registered.
    /// </summary>
    public void RegisterLog(int submissionId, DiagnosticLog log)
        => Contract.ThrowIfFalse(_logsBySubmissionId.TryAdd(submissionId, log), $"A log is already registered for submission {submissionId}.");

    public bool TryUnregisterLog(int submissionId)
        => _logsBySubmissionId.TryRemove(submissionId, out _);

    private void OnErrorRaised(object sender, MSB.Framework.BuildErrorEventArgs e)
        => AddLogItem(DiagnosticLogItemKind.Error, e.BuildEventContext, e.ProjectFile, e.Message, e.File, e.LineNumber, e.ColumnNumber);

    private void OnWarningRaised(object sender, MSB.Framework.BuildWarningEventArgs e)
        => AddLogItem(DiagnosticLogItemKind.Warning, e.BuildEventContext, e.ProjectFile, e.Message, e.File, e.LineNumber, e.ColumnNumber);

    private void AddLogItem(DiagnosticLogItemKind kind, MSB.Framework.BuildEventContext? buildEventContext, string? projectFile, string? message, string? file, int lineNumber, int columnNumber)
    {
        // A build error or warning is always raised in the context of a building submission whose log we
        // registered before executing it, so we expect a context and a log registered for its submission.
        Debug.Assert(buildEventContext != null);
        if (buildEventContext is null)
            return;

        if (!_logsBySubmissionId.TryGetValue(buildEventContext.SubmissionId, out var log))
        {
            // We don't expect this, but if it happens there's no log to attribute the event to, so ignore it.
            Debug.Fail($"No log is registered for submission {buildEventContext.SubmissionId}.");
            return;
        }

        log.Add(new MSBuildDiagnosticLogItem(kind, projectFile ?? "", message ?? "", file ?? "", lineNumber, columnNumber));
    }

    public void Initialize(MSB.Framework.IEventSource eventSource)
    {
        Debug.Assert(_eventSource == null);

        _eventSource = eventSource;
        _eventSource.ErrorRaised += OnErrorRaised;
        _eventSource.WarningRaised += OnWarningRaised;
    }

    public void Shutdown()
    {
        if (_eventSource != null)
        {
            _eventSource.ErrorRaised -= OnErrorRaised;
            _eventSource.WarningRaised -= OnWarningRaised;

            _eventSource = null;

            _logsBySubmissionId.Clear();
        }
    }
}
