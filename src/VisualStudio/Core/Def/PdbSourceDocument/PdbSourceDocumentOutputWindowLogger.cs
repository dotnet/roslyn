// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PdbSourceDocument;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.PdbSourceDocument;

[Export(typeof(IPdbSourceDocumentLogger)), Shared]
internal sealed class PdbSourceDocumentOutputWindowLogger : IPdbSourceDocumentLogger, IDisposable
{
    private static readonly Guid s_outputPaneGuid = new("f543e896-2e9c-48b8-8fac-d1d5030b4b89");
    private IVsOutputWindowPane? _outputPane;

    private readonly IThreadingContext _threadingContext;
    private readonly AsyncBatchingWorkQueue<string?> _logItemsQueue;
    private readonly IServiceProvider _serviceProvider;

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public PdbSourceDocumentOutputWindowLogger(SVsServiceProvider serviceProvider, IThreadingContext threadingContext, IAsynchronousOperationListenerProvider listenerProvider)
    {
        _serviceProvider = serviceProvider;
        _threadingContext = threadingContext;

        var asyncListener = listenerProvider.GetListener(nameof(PdbSourceDocumentOutputWindowLogger));

        _logItemsQueue = new AsyncBatchingWorkQueue<string?>(
            DelayTimeSpan.NearImmediate,
            ProcessLogMessagesAsync,
            asyncListener,
            _cancellationTokenSource.Token);
    }

    private async ValueTask ProcessLogMessagesAsync(ImmutableSegmentedList<string?> messages, CancellationToken cancellationToken)
    {
        await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        foreach (var message in messages)
        {
            var pane = GetPane();
            if (pane == null)
            {
                return;
            }

            if (message is null)
            {
                pane.Clear();
            }
            else if (pane is IVsOutputWindowPaneNoPump noPumpPane)
            {
                noPumpPane.OutputStringNoPump(message + Environment.NewLine);
            }
            else
            {
                pane.OutputStringThreadSafe(message + Environment.NewLine);
            }
        }
    }

    public void Clear()
    {
        _logItemsQueue.AddWork((string?)null);
    }

    public void Log(string value)
    {
        _logItemsQueue.AddWork(value);
    }

    private IVsOutputWindowPane? GetPane()
    {
        _threadingContext.ThrowIfNotOnUIThread();

        if (_outputPane == null)
        {
            var outputWindow = (IVsOutputWindow)_serviceProvider.GetService(typeof(SVsOutputWindow));

            _outputPane = TryCreateOutputPane(outputWindow);
        }

        return _outputPane;
    }

    private IVsOutputWindowPane? TryCreateOutputPane(IVsOutputWindow outputWindow)
    {
        _threadingContext.ThrowIfNotOnUIThread();

        var paneGuid = s_outputPaneGuid;

        if (ErrorHandler.Succeeded(outputWindow.CreatePane(ref paneGuid, ServicesVSResources.Navigate_to_external_sources, fInitVisible: 1, fClearWithSolution: 1)) &&
            ErrorHandler.Succeeded(outputWindow.GetPane(ref paneGuid, out var pane)))
        {
            return pane;
        }

        return null;
    }

    void IDisposable.Dispose()
    {
        _cancellationTokenSource.Cancel();
    }
}
