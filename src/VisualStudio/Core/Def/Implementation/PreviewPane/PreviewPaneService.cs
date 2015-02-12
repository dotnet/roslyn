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
            var telemetry = diagnostic == null ? false : diagnostic.Descriptor.CustomTags.Contains(WellKnownDiagnosticTags.Telemetry);

            if ((diagnostic == null) && (previewContent == null))
            {
                // Bail out in cases where there is no diagnostic (which means there is nothing to put in
                // the header section of the preview pane) as well as no preview content (i.e. no diff view).
                return null;
            }

            if ((diagnostic == null) || (diagnostic.Descriptor is TriggerDiagnosticDescriptor))
            {
                return new PreviewPane(
                    null, null, null, null, null, null, telemetry, previewContent, _serviceProvider);
            }
            else
            {
                return new PreviewPane(
                    GetSeverityIconForDiagnostic(diagnostic),
                    diagnostic.Id, diagnostic.GetMessage(),
                    diagnostic.Descriptor.MessageFormat.ToString(DiagnosticData.USCultureInfo),
                    diagnostic.Descriptor.Description.ToString(CultureInfo.CurrentUICulture),
                    diagnostic.Descriptor.HelpLinkUri, telemetry, previewContent, _serviceProvider);
            }
        }
    }
}
