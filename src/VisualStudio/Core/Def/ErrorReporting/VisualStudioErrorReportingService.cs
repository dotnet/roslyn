// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.CodeAnalysis.ErrorReporting
{
    [ExportWorkspaceService(typeof(IErrorReportingService), ServiceLayer.Host), Shared]
    internal partial class VisualStudioErrorReportingService : IErrorReportingService
    {
        private readonly IThreadingContext _threadingContext;
        private readonly IAsynchronousOperationListener _listener;
        private readonly VisualStudioInfoBar _infoBar;
        private readonly SVsServiceProvider _serviceProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioErrorReportingService(
            IThreadingContext threadingContext,
            IAsynchronousOperationListenerProvider listenerProvider,
            SVsServiceProvider serviceProvider)
        {
            _threadingContext = threadingContext;
            _listener = listenerProvider.GetListener(FeatureAttribute.Workspace);
            _infoBar = new VisualStudioInfoBar(threadingContext, serviceProvider, listenerProvider);
            _serviceProvider = serviceProvider;
        }

        public string HostDisplayName => "Visual Studio";

        public void ShowGlobalErrorInfo(string message, TelemetryFeatureName featureName, Exception? exception, params InfoBarUI[] items)
        {
            var stackTrace = exception is null ? "" : GetFormattedExceptionStack(exception);
            LogGlobalErrorToActivityLog(message, stackTrace);
            _infoBar.ShowInfoBar(message, items);

            Logger.Log(FunctionId.VS_ErrorReportingService_ShowGlobalErrorInfo, KeyValueLogMessage.Create(LogType.UserAction, m =>
            {
                m["Message"] = message;
                m["FeatureName"] = featureName.ToString();
            }));
        }

        public void ShowDetailedErrorInfo(Exception exception)
        {
            var errorInfo = GetFormattedExceptionStack(exception);
            new DetailedErrorInfoDialog(exception.Message, errorInfo).ShowModal();
        }

        public void ShowFeatureNotAvailableErrorInfo(string message, TelemetryFeatureName featureName, Exception? exception)
        {
            var infoBarUIs = new List<InfoBarUI>();

            if (exception != null)
            {
                infoBarUIs.Add(new InfoBarUI(
                    WorkspacesResources.Show_Stack_Trace,
                    InfoBarUI.UIKind.HyperLink,
                    () => ShowDetailedErrorInfo(exception),
                    closeAfterAction: true));
            }

            ShowGlobalErrorInfo(message, featureName, exception, infoBarUIs.ToArray());
        }

        private void LogGlobalErrorToActivityLog(string message, string? detailedError)
        {
            _ = _threadingContext.JoinableTaskFactory.RunAsync(async () =>
            {
                using var _ = _listener.BeginAsyncOperation(nameof(LogGlobalErrorToActivityLog));

                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(_threadingContext.DisposalToken);

                var activityLog = await ((IAsyncServiceProvider)_serviceProvider).GetServiceAsync<SVsActivityLog, IVsActivityLog>(_threadingContext.JoinableTaskFactory).ConfigureAwait(true);

                activityLog.LogEntry(
                    (uint)__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR,
                    nameof(VisualStudioErrorReportingService),
                    string.Join(Environment.NewLine, message, detailedError));
            });
        }
    }
}
