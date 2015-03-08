// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.PreviewPane
{
    [ExportWorkspaceServiceFactory(typeof(IPreviewPaneService), ServiceLayer.Host), Shared]
    internal class PreviewPaneService : ForegroundThreadAffinitizedObject, IPreviewPaneService, IWorkspaceServiceFactory
    {
        private readonly IServiceProvider _serviceProvider;

        [ImportingConstructor]
        public PreviewPaneService(SVsServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        IWorkspaceService IWorkspaceServiceFactory.CreateService(HostWorkspaceServices workspaceServices)
        {
            return this;
        }

        private Image GetSeverityIconForDiagnostic(Diagnostic diagnostic)
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

        object IPreviewPaneService.GetPreviewPane(Diagnostic diagnostic, object previewContent)
        {
            var title = diagnostic?.GetMessage();

            if (string.IsNullOrWhiteSpace(title))
            {
                if (previewContent == null)
                {
                    // Bail out in cases where there is nothing to put in the header section
                    // of the preview pane and no preview content (i.e. no diff view) either.
                    return null;
                }

                return new PreviewPane(
                    severityIcon: null, id: null, title: null, helpMessage: null,
                    description: null, helpLink: null, telemetry: false,
                    previewContent: previewContent, serviceProvider: _serviceProvider);
            }

            return new PreviewPane(
                GetSeverityIconForDiagnostic(diagnostic),
                diagnostic.Id, title,
                diagnostic.Descriptor.MessageFormat.ToString(DiagnosticData.USCultureInfo),
                diagnostic.Descriptor.Description.ToString(CultureInfo.CurrentUICulture),
                diagnostic.Descriptor.HelpLinkUri,
                diagnostic.Descriptor.CustomTags.Contains(WellKnownDiagnosticTags.Telemetry),
                previewContent, _serviceProvider);
        }
    }
}
