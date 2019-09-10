// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    /// <summary>
    /// Helper type to show remote host crash info bar
    /// </summary>
    internal static class RemoteHostCrashInfoBar
    {
        // OOP killed more info page link
        private const string OOPKilledMoreInfoLink = "https://go.microsoft.com/fwlink/?linkid=842308";

        private static bool s_infoBarReported = false;

        public static void ShowInfoBar(Workspace workspace, Exception exception = null)
        {
            // use info bar to show warning to users
            if (workspace == null || s_infoBarReported)
            {
                return;
            }

            s_infoBarReported = true;

            // use info bar to show warning to users
            var infoBarUIs = new List<InfoBarUI>();

            infoBarUIs.Add(
                new InfoBarUI(ServicesVSResources.Learn_more, InfoBarUI.UIKind.HyperLink, () =>
                    BrowserHelper.StartBrowser(new Uri(OOPKilledMoreInfoLink)), closeAfterAction: false));

            var service = workspace.Services.GetService<IRemoteHostClientService>();
            var allowRestarting = workspace.Options.GetOption(RemoteHostOptions.RestartRemoteHostAllowed);
            if (allowRestarting && service != null)
            {
                // this is hidden restart option. by default, user can't restart remote host that got killed
                // by users
                infoBarUIs.Add(
                    new InfoBarUI("Restart external process", InfoBarUI.UIKind.Button, () =>
                    {
                        // start off new remote host
                        var unused = service.RequestNewRemoteHostAsync(CancellationToken.None);
                        s_infoBarReported = false;
                    }, closeAfterAction: true));
            }

            if (exception != null)
            {
                var errorReportingService = workspace.Services.GetService<IErrorReportingService>();
                infoBarUIs.Add(
                    new InfoBarUI(WorkspacesResources.Show_Stack_Trace, InfoBarUI.UIKind.HyperLink, () =>
                        errorReportingService.ShowDetailedErrorInfo(exception), closeAfterAction: true));
            }

            workspace.Services.GetService<IErrorReportingService>().ShowGlobalErrorInfo(
                ServicesVSResources.Unfortunately_a_process_used_by_Visual_Studio_has_encountered_an_unrecoverable_error_We_recommend_saving_your_work_and_then_closing_and_restarting_Visual_Studio,
                infoBarUIs.ToArray());
        }
    }
}
