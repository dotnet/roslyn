// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.CodeAnalysis.Razor.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.Razor.Logging;

[Export(typeof(RazorActivityLog))]
internal sealed class RazorActivityLog : IDisposable
{
    private enum EntryType { Error, Warning, Info }

    private readonly IAsyncServiceProvider _serviceProvider;

    private readonly CancellationTokenSource _disposeTokenSource;
    private readonly AsyncBatchingWorkQueue<(EntryType, string)> _loggingQueue;
    private IVsActivityLog? _vsActivityLog;

    [ImportingConstructor]
    public RazorActivityLog([Import(typeof(SAsyncServiceProvider))] IAsyncServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        _disposeTokenSource = new();
        _loggingQueue = new AsyncBatchingWorkQueue<(EntryType, string)>(TimeSpan.Zero, ProcessBatchAsync, _disposeTokenSource.Token);
    }

    public void Dispose()
    {
        if (_disposeTokenSource.IsCancellationRequested)
        {
            return;
        }

        _disposeTokenSource.Cancel();
        _disposeTokenSource.Dispose();
    }

    private async ValueTask ProcessBatchAsync(ImmutableArray<(EntryType, string)> items, CancellationToken token)
    {
        if (token.IsCancellationRequested)
        {
            return;
        }

        _vsActivityLog ??= await _serviceProvider.GetServiceAsync<SVsActivityLog, IVsActivityLog>(token).ConfigureAwait(false);

        foreach (var (entryType, message) in items)
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

            var vsEntryType = entryType switch
            {
                EntryType.Error => __ACTIVITYLOG_ENTRYTYPE.ALE_ERROR,
                EntryType.Warning => __ACTIVITYLOG_ENTRYTYPE.ALE_WARNING,
                EntryType.Info => __ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION,
                _ => Assumed.Unreachable<__ACTIVITYLOG_ENTRYTYPE>()
            };

            _vsActivityLog.LogEntry(
                (uint)vsEntryType,
                "Razor",
                $"Info:{Environment.NewLine}{message}");
        }
    }

    public void LogError(string message)
    {
        _loggingQueue.AddWork((EntryType.Error, message));
    }

    public void LogWarning(string message)
    {
        _loggingQueue.AddWork((EntryType.Warning, message));
    }

    public void LogInfo(string message)
    {
        _loggingQueue.AddWork((EntryType.Info, message));
    }
}
