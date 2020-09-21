// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal partial class VisualStudioErrorReportingService : IErrorReportingService
    {
        private static bool s_infoBarReported = false;

        private readonly IInfoBarService _infoBarService;

        public VisualStudioErrorReportingService(IInfoBarService infoBarService)
            => _infoBarService = infoBarService;

        public string HostDisplayName => "Visual Studio";

        public void ShowErrorInfoInActiveView(string message, params InfoBarUI[] items)
            => _infoBarService.ShowInfoBarInActiveView(message, items);

        public void ShowGlobalErrorInfo(string message, params InfoBarUI[] items)
            => _infoBarService.ShowInfoBarInGlobalView(message, items);

        public void ShowDetailedErrorInfo(Exception exception)
        {
            var errorInfo = GetFormattedExceptionStack(exception);
            new DetailedErrorInfoDialog(exception.Message, errorInfo).ShowModal();
        }

        // obsolete - will remove once we remove JsonRpcConnection
        // https://github.com/dotnet/roslyn/issues/45859
        public void ShowRemoteHostCrashedErrorInfo(Exception? exception)
        {
            if (s_infoBarReported)
            {
                return;
            }

            s_infoBarReported = true;

            // use info bar to show warning to users
            var infoBarUIs = new List<InfoBarUI>();

            infoBarUIs.Add(new InfoBarUI(
                ServicesVSResources.Learn_more,
                InfoBarUI.UIKind.HyperLink,
                () => BrowserHelper.StartBrowser(new Uri("https://go.microsoft.com/fwlink/?linkid=842308")),
                closeAfterAction: false));

            if (exception != null)
            {
                infoBarUIs.Add(new InfoBarUI(
                    WorkspacesResources.Show_Stack_Trace,
                    InfoBarUI.UIKind.HyperLink,
                    () => ShowDetailedErrorInfo(exception),
                    closeAfterAction: true));
            }

            ShowGlobalErrorInfo(
                ServicesVSResources.Unfortunately_a_process_used_by_Visual_Studio_has_encountered_an_unrecoverable_error_We_recommend_saving_your_work_and_then_closing_and_restarting_Visual_Studio,
                infoBarUIs.ToArray());
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
