// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal partial class VisualStudioErrorReportingService : IErrorReportingService
    {
        private readonly IInfoBarService _infoBarService;

        public VisualStudioErrorReportingService(IInfoBarService infoBarService)
            => _infoBarService = infoBarService;

        public string HostDisplayName => "Visual Studio";

        public void ShowGlobalErrorInfo(string message, params InfoBarUI[] items)
        {
            _infoBarService.ShowInfoBar(message, items);

            // Have to use KeyValueLogMessage so it gets reported in telemetry
            Logger.Log(FunctionId.VS_ErrorReportingService_ShowGlobalErrorInfo, message, LogLevel.Information);
        }

        public void ShowDetailedErrorInfo(Exception exception)
        {
            var errorInfo = GetFormattedExceptionStack(exception);
            new DetailedErrorInfoDialog(exception.Message, errorInfo).ShowModal();
        }

        public void ShowFeatureNotAvailableErrorInfo(string message, Exception? exception)
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

            ShowGlobalErrorInfo(message, infoBarUIs.ToArray());
        }
    }
}
