// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.PreviewPane
{
    [ExportWorkspaceServiceFactory(typeof(IPreviewPaneService), ServiceLayer.Host), Shared]
    internal class PreviewPaneService : ForegroundThreadAffinitizedObject, IPreviewPaneService, IWorkspaceServiceFactory
    {
        IWorkspaceService IWorkspaceServiceFactory.CreateService(HostWorkspaceServices workspaceServices)
        {
            return this;
        }

        private static Image GetSeverityIconForDiagnostic(DiagnosticData diagnostic)
        {
            ImageMoniker? moniker = null;
            switch (diagnostic.Severity)
            {
                case DiagnosticSeverity.Error:
                    moniker = KnownMonikers.StatusError;
                    break;
                case DiagnosticSeverity.Warning:
                    moniker = KnownMonikers.StatusWarning;
                    break;
                case DiagnosticSeverity.Info:
                    moniker = KnownMonikers.StatusInformation;
                    break;
                case DiagnosticSeverity.Hidden:
                    moniker = KnownMonikers.StatusHidden;
                    break;
            }

            if (moniker.HasValue)
            {
                return new CrispImage
                {
                    Moniker = moniker.Value
                };
            }

            return null;
        }

        private static Uri GetHelpLink(DiagnosticData diagnostic, string language, string projectType, out string helpLinkToolTipText)
        {
            var isBing = false;
            helpLinkToolTipText = string.Empty;

            Uri helpLink;
            if (!BrowserHelper.TryGetUri(diagnostic.HelpLink, out helpLink))
            {
                // We use the ENU version of the message for bing search.
                helpLink = BrowserHelper.CreateBingQueryUri(diagnostic.Id, diagnostic.ENUMessageForBingSearch, language, projectType);
                isBing = true;
            }

            // We make sure not to use Uri.AbsoluteUri for the url displayed in the tooltip so that the url displayed in the tooltip stays human readable.
            if (helpLink != null)
            {
                helpLinkToolTipText =
                    string.Format(ServicesVSResources.DiagnosticIdHyperlinkTooltipText, diagnostic.Id,
                        isBing ? ServicesVSResources.FromBing : null, Environment.NewLine, helpLink);
            }

            return helpLink;
        }

        object IPreviewPaneService.GetPreviewPane(DiagnosticData diagnostic, string language, string projectType, IList<object> previewContent)
        {
            var title = diagnostic?.Message;

            if (string.IsNullOrWhiteSpace(title))
            {
                if (previewContent == null)
                {
                    // Bail out in cases where there is nothing to put in the header section
                    // of the preview pane and no preview content (i.e. no diff view) either.
                    return null;
                }

                return new PreviewPane(
                    severityIcon: null, id: null, title: null, description: null, helpLink: null, helpLinkToolTipText: null,
                    previewContent: previewContent, logIdVerbatimInTelemetry: false);
            }

            var helpLinkToolTipText = string.Empty;
            Uri helpLink = GetHelpLink(diagnostic, language, projectType, out helpLinkToolTipText);

            return new PreviewPane(
                severityIcon: GetSeverityIconForDiagnostic(diagnostic),
                id: diagnostic.Id, title: title,
                description: diagnostic.Description.ToString(CultureInfo.CurrentUICulture),
                helpLink: helpLink,
                helpLinkToolTipText: helpLinkToolTipText,
                previewContent: previewContent,
                logIdVerbatimInTelemetry: diagnostic.CustomTags.Contains(WellKnownDiagnosticTags.Telemetry));
        }
    }
}
