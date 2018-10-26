// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.CodeAnalysis.Internal.Log
{
    /// <summary>
    /// Implementation of <see cref="ILogger"/> that output to output window
    /// </summary>
    internal sealed class OutputWindowLogger : ILogger
    {
        private readonly Func<FunctionId, bool> _loggingChecker;

        public OutputWindowLogger()
            : this((Func<FunctionId, bool>)null)
        {
        }

        public OutputWindowLogger(IGlobalOptionService optionService)
            : this(Logger.GetLoggingChecker(optionService))
        {
        }

        public OutputWindowLogger(Func<FunctionId, bool> loggingChecker)
        {
            _loggingChecker = loggingChecker;
        }

        public bool IsEnabled(FunctionId functionId)
        {
            return _loggingChecker == null || _loggingChecker(functionId);
        }

        public void Log(FunctionId functionId, LogMessage logMessage)
        {
            OutputPane.WriteLine(string.Format("[{0}] {1} - {2}", Thread.CurrentThread.ManagedThreadId, functionId.ToString(), logMessage.GetMessage()));
        }

        public void LogBlockStart(FunctionId functionId, LogMessage logMessage, int uniquePairId, CancellationToken cancellationToken)
        {
            OutputPane.WriteLine(string.Format("[{0}] Start({1}) : {2} - {3}", Thread.CurrentThread.ManagedThreadId, uniquePairId, functionId.ToString(), logMessage.GetMessage()));
        }

        public void LogBlockEnd(FunctionId functionId, LogMessage logMessage, int uniquePairId, int delta, CancellationToken cancellationToken)
        {
            var functionString = functionId.ToString() + (cancellationToken.IsCancellationRequested ? " Canceled" : string.Empty);
            OutputPane.WriteLine(string.Format("[{0}] End({1}) : [{2}ms] {3}", Thread.CurrentThread.ManagedThreadId, uniquePairId, delta, functionString));
        }

        private class OutputPane
        {
            private static readonly Guid s_outputPaneGuid = new Guid("BBAFF416-4AF5-41F2-9F93-91F283E43C3B");

            public static readonly OutputPane s_instance = new OutputPane();

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
}
