// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Internal.VisualStudio.Shell.Embeddable.Feedback;

namespace Microsoft.VisualStudio.LanguageServices.Feedback;

internal abstract class AbstractFeedbackDiagnosticFileProvider : IFeedbackDiagnosticFileProvider
{
    private readonly IVisualStudioFeedbackFileWatcherService _feedbackFileWatcherService;
    private volatile int _isRecordingInProgress;

    protected AbstractFeedbackDiagnosticFileProvider(IVisualStudioFeedbackFileWatcherService feedbackFileWatcherService)
    {
        _feedbackFileWatcherService = feedbackFileWatcherService;
    }

    protected bool IsWatching => _feedbackFileWatcherService.IsWatching;

    protected void StartListeningToFeedbackRecording()
    {
        if (!_feedbackFileWatcherService.IsWatching)
        {
            return;
        }

        _feedbackFileWatcherService.RecordingStarted += HandleFeedbackRecordingStarted;
        _feedbackFileWatcherService.RecordingEnded += HandleFeedbackRecordingEnded;

        if (_feedbackFileWatcherService.IsRecordingForCurrentVisualStudioInstance)
        {
            HandleFeedbackRecordingStarted(this, EventArgs.Empty);
        }
    }

    public abstract IReadOnlyCollection<string> GetFiles();

    protected abstract void OnFeedbackRecordingStarted();

    protected abstract void OnFeedbackRecordingEnded();

    private void HandleFeedbackRecordingStarted(object? sender, EventArgs args)
    {
        if (Interlocked.CompareExchange(ref _isRecordingInProgress, 1, 0) == 0)
        {
            OnFeedbackRecordingStarted();
        }
    }

    private void HandleFeedbackRecordingEnded(object? sender, EventArgs args)
    {
        if (Interlocked.Exchange(ref _isRecordingInProgress, 0) == 1)
        {
            OnFeedbackRecordingEnded();
        }
    }
}
