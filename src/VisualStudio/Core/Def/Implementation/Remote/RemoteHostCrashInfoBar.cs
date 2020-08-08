// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host;
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

        public static void ShowInfoBar(HostWorkspaceServices services, Exception? exception = null)
        {
            if (s_infoBarReported)
            {
                return;
            }

            s_infoBarReported = true;

            // use info bar to show warning to users
            var infoBarUIs = new List<InfoBarUI>();

            infoBarUIs.Add(
                new InfoBarUI(ServicesVSResources.Learn_more, InfoBarUI.UIKind.HyperLink, () =>
                    BrowserHelper.StartBrowser(new Uri(OOPKilledMoreInfoLink)), closeAfterAction: false));

            var errorReportingService = services.GetRequiredService<IErrorReportingService>();

            if (exception != null)
            {
                infoBarUIs.Add(
                    new InfoBarUI(WorkspacesResources.Show_Stack_Trace, InfoBarUI.UIKind.HyperLink, () =>
                        errorReportingService.ShowDetailedErrorInfo(exception), closeAfterAction: true));
            }

            errorReportingService.ShowGlobalErrorInfo(
                ServicesVSResources.Unfortunately_a_process_used_by_Visual_Studio_has_encountered_an_unrecoverable_error_We_recommend_saving_your_work_and_then_closing_and_restarting_Visual_Studio,
                infoBarUIs.ToArray());
        }
    }
}
