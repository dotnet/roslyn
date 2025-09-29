// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.CodeAnalysis.Internal.Log;

/// <summary>
/// Implementation of <see cref="ILogger"/> that output to output window
/// </summary>
internal sealed class OutputWindowLogger : ILogger
{
    private readonly Func<FunctionId, bool> _isEnabledPredicate;

    public OutputWindowLogger(Func<FunctionId, bool> isEnabledPredicate)
    {
        _isEnabledPredicate = isEnabledPredicate;
    }

    public bool IsEnabled(FunctionId functionId)
        => _isEnabledPredicate(functionId);

    public void Log(FunctionId functionId, LogMessage logMessage)
    {
        OutputPane.WriteLine(string.Format("[{0}] {1} - {2}", Environment.CurrentManagedThreadId, functionId.ToString(), logMessage.GetMessage()));
    }

    public void LogBlockStart(FunctionId functionId, LogMessage logMessage, int uniquePairId, CancellationToken cancellationToken)
    {
        OutputPane.WriteLine(string.Format("[{0}] Start({1}) : {2} - {3}", Environment.CurrentManagedThreadId, uniquePairId, functionId.ToString(), logMessage.GetMessage()));
    }

    public void LogBlockEnd(FunctionId functionId, LogMessage logMessage, int uniquePairId, int delta, CancellationToken cancellationToken)
    {
        var functionString = functionId.ToString() + (cancellationToken.IsCancellationRequested ? " Canceled" : string.Empty);
        OutputPane.WriteLine(string.Format("[{0}] End({1}) : [{2}ms] {3}", Environment.CurrentManagedThreadId, uniquePairId, delta, functionString));
    }

    private sealed class OutputPane
    {
        private static readonly Guid s_outputPaneGuid = new("BBAFF416-4AF5-41F2-9F93-91F283E43C3B");

        public static readonly OutputPane s_instance = new();

        private readonly IServiceProvider _serviceProvider;
        private readonly IThreadingContext _threadingContext;

        public static void WriteLine(string value)
        {
            s_instance.WriteLineInternal(value);
        }

        public OutputPane()
        {
            _serviceProvider = ServiceProvider.GlobalProvider;

            var componentModel = (IComponentModel)_serviceProvider.GetService(typeof(SComponentModel));
            _threadingContext = componentModel.GetService<IThreadingContext>();
        }

        private IVsOutputWindowPane _doNotAccessDirectlyOutputPane;

        private void WriteLineInternal(string value)
        {
            var pane = GetPane();
            if (pane == null)
            {
                return;
            }

            pane.OutputStringThreadSafe(value + Environment.NewLine);
        }

        private IVsOutputWindowPane GetPane()
        {
            if (_doNotAccessDirectlyOutputPane == null)
            {
                _threadingContext.JoinableTaskFactory.Run(async () =>
               {
                   await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();

                   if (_doNotAccessDirectlyOutputPane != null)
                   {
                       // check whether other one already initialized output window.
                       // the output API already handle double initialization, so this is just quick bail
                       // rather than any functional issue
                       return;
                   }

                   var outputWindow = (IVsOutputWindow)_serviceProvider.GetService(typeof(SVsOutputWindow));

                   // this should bring outout window to the front
                   _doNotAccessDirectlyOutputPane = CreateOutputPane(outputWindow);
               });
            }

            return _doNotAccessDirectlyOutputPane;
        }

        private IVsOutputWindowPane CreateOutputPane(IVsOutputWindow outputWindow)
        {
            _threadingContext.ThrowIfNotOnUIThread();

            // Try to get the workspace pane if it has already been registered
            var workspacePaneGuid = s_outputPaneGuid;

            // If the pane has already been created, CreatePane returns it
            if (ErrorHandler.Succeeded(outputWindow.CreatePane(ref workspacePaneGuid, "Roslyn Logger Output", fInitVisible: 1, fClearWithSolution: 1)) &&
                ErrorHandler.Succeeded(outputWindow.GetPane(ref workspacePaneGuid, out var pane)))
            {
                return pane;
            }

            return null;
        }
    }
}
