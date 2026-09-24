// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Host.Mef;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.LanguageServices.Feedback;

[Export(typeof(IVisualStudioFeedbackFileWatcherService))]
internal sealed class VisualStudioFeedbackFileWatcherService : IVisualStudioFeedbackFileWatcherService, IDisposable
{
    private const string VSFeedbackSemaphoreDir = @"Microsoft\VSFeedbackCollector";
    private const string VSFeedbackSemaphoreFileName = "feedback.recording.json";

    private readonly DateTime _vsProcessStartTime;
    private readonly string _vsFeedbackSemaphoreFullPath;
    private readonly FileSystemWatcher? _vsFeedbackSemaphoreFileWatcher;
    private int _isRecordingForCurrentVisualStudioInstance;

    public event EventHandler? RecordingStarted;

    public event EventHandler? RecordingEnded;

    public bool IsWatching => _vsFeedbackSemaphoreFileWatcher is not null;

    public bool IsRecordingForCurrentVisualStudioInstance
        => Volatile.Read(ref _isRecordingForCurrentVisualStudioInstance) == 1;

    public int VisualStudioProcessId { get; }

    public string TempDirectory { get; }

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VisualStudioFeedbackFileWatcherService()
    {
        var vsProcess = Process.GetCurrentProcess();

        VisualStudioProcessId = vsProcess.Id;
        _vsProcessStartTime = vsProcess.StartTime;
        TempDirectory = Path.GetTempPath();

        var vsFeedbackTempDir = Path.Combine(TempDirectory, VSFeedbackSemaphoreDir);
        _vsFeedbackSemaphoreFullPath = Path.Combine(vsFeedbackTempDir, VSFeedbackSemaphoreFileName);

        // Directory may not exist in scenarios such as Razor integration tests.
        if (!Directory.Exists(vsFeedbackTempDir))
        {
            return;
        }

        _vsFeedbackSemaphoreFileWatcher = new FileSystemWatcher(vsFeedbackTempDir, VSFeedbackSemaphoreFileName);
        _vsFeedbackSemaphoreFileWatcher.Created += (_, _) => OnFeedbackSemaphoreCreatedOrChanged();
        _vsFeedbackSemaphoreFileWatcher.Changed += (_, _) => OnFeedbackSemaphoreCreatedOrChanged();
        _vsFeedbackSemaphoreFileWatcher.Deleted += (_, _) => OnFeedbackSemaphoreDeleted();

        if (File.Exists(_vsFeedbackSemaphoreFullPath))
        {
            OnFeedbackSemaphoreCreatedOrChanged();
        }

        _vsFeedbackSemaphoreFileWatcher.EnableRaisingEvents = true;
    }

    private void OnFeedbackSemaphoreCreatedOrChanged()
    {
        if (!IsLoggingEnabledForCurrentVisualStudioInstance(_vsFeedbackSemaphoreFullPath))
        {
            return;
        }

        if (Interlocked.CompareExchange(ref _isRecordingForCurrentVisualStudioInstance, 1, 0) == 0)
        {
            RecordingStarted?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnFeedbackSemaphoreDeleted()
    {
        if (Interlocked.Exchange(ref _isRecordingForCurrentVisualStudioInstance, 0) == 1)
        {
            RecordingEnded?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Dispose()
    {
        _vsFeedbackSemaphoreFileWatcher?.Dispose();
    }

    private bool IsLoggingEnabledForCurrentVisualStudioInstance(string semaphoreFilePath)
    {
        try
        {
            if (_vsProcessStartTime > File.GetCreationTime(semaphoreFilePath))
            {
                // Semaphore file is older than the running instance of VS.
                return false;
            }

            var content = File.ReadAllText(semaphoreFilePath);
            return JObject.Parse(content)["processIds"] is JContainer pidCollection && pidCollection.Values<int>().Contains(VisualStudioProcessId);
        }
        catch
        {
            // ignore
            return false;
        }
    }
}
