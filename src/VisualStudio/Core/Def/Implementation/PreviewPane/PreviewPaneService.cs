// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.Shell;
using IVsUIShell = Microsoft.VisualStudio.Shell.Interop.IVsUIShell;
using SVsUIShell = Microsoft.VisualStudio.Shell.Interop.SVsUIShell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.PreviewPane
{
    [ExportWorkspaceServiceFactory(typeof(IPreviewPaneService), ServiceLayer.Host), Shared]
    internal class PreviewPaneService : ForegroundThreadAffinitizedObject, IPreviewPaneService, IWorkspaceServiceFactory
    {
        private readonly IVsUIShell _uiShell;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public PreviewPaneService(IThreadingContext threadingContext, SVsServiceProvider serviceProvider)
            : base(threadingContext)
        {
            _uiShell = serviceProvider.GetService(typeof(SVsUIShell)) as IVsUIShell;
        }

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

        object IPreviewPaneService.GetPreviewPane(DiagnosticData data, IReadOnlyList<object> previewContent)
        {
            var title = data?.Message;

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
                    previewContent: previewContent, logIdVerbatimInTelemetry: false, uiShell: _uiShell);
            }

            var helpLinkUri = BrowserHelper.GetHelpLink(data);
            var helpLinkToolTip = BrowserHelper.GetHelpLinkToolTip(data.Id, helpLinkUri);

            Guid optionPageGuid = default;
            if (data.Properties.TryGetValue("OptionName", out var optionName))
            {
                data.Properties.TryGetValue("OptionLanguage", out var optionLanguage);
                optionPageGuid = GetOptionPageGuidForOptionName(optionName, optionLanguage);
            }

            return new PreviewPane(
                severityIcon: GetSeverityIconForDiagnostic(data),
                id: data.Id, title: title,
                description: data.Description.ToString(CultureInfo.CurrentUICulture),
                helpLink: helpLinkUri,
                helpLinkToolTipText: helpLinkToolTip,
                previewContent: previewContent,
                logIdVerbatimInTelemetry: data.CustomTags.Contains(WellKnownDiagnosticTags.Telemetry),
                uiShell: _uiShell,
                optionPageGuid: optionPageGuid);
        }

        private Guid GetOptionPageGuidForOptionName(string optionName, string optionLanguage)
        {
            if (optionName == nameof(SimplificationOptions.NamingPreferences))
            {
                if (optionLanguage == LanguageNames.CSharp)
                {
                    return Guid.Parse(Guids.CSharpOptionPageNamingStyleIdString);
                }
                else if (optionLanguage == LanguageNames.VisualBasic)
                {
                    return Guid.Parse(Guids.VisualBasicOptionPageNamingStyleIdString);
                }
            }
            else if (optionName == nameof(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration))
            {
                if (optionLanguage == LanguageNames.CSharp)
                {
                    return Guid.Parse(Guids.CSharpOptionPageCodeStyleIdString);
                }
                else if (optionLanguage == LanguageNames.VisualBasic)
                {
                    return Guid.Parse(Guids.VisualBasicOptionPageVBSpecificIdString);
                }
            }

            return default;
        }
    }
}
