// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Composition;
using System.Globalization;
using AppKit;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Imaging;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Preview
{
    [ExportWorkspaceServiceFactory(typeof(IPreviewPaneService), ServiceLayer.Host), Shared]
    internal class PreviewPaneService : ForegroundThreadAffinitizedObject, IPreviewPaneService, IWorkspaceServiceFactory
    {
        private readonly IImageService imageService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public PreviewPaneService(IThreadingContext threadingContext, IImageService imageService)
            : base(threadingContext)
        {
            this.imageService = imageService;
        }

        IWorkspaceService IWorkspaceServiceFactory.CreateService(HostWorkspaceServices workspaceServices)
        {
            return this;
        }

#pragma warning disable IDE0051 // Remove unused private members
        private NSImage GetSeverityIconForDiagnostic(DiagnosticData diagnostic)
#pragma warning restore IDE0051 // Remove unused private members
        {
            int? moniker = null;
            switch (diagnostic.Severity)
            {
                case DiagnosticSeverity.Error:
                    moniker = KnownImageIds.StatusError;
                    break;
                case DiagnosticSeverity.Warning:
                    moniker = KnownImageIds.StatusWarning;
                    break;
                case DiagnosticSeverity.Info:
                    moniker = KnownImageIds.StatusInformation;
                    break;
                case DiagnosticSeverity.Hidden:
                    moniker = KnownImageIds.StatusHidden;
                    break;
            }

            if (moniker.HasValue)
            {
                return (NSImage)imageService.GetImage(new ImageId(KnownImageIds.ImageCatalogGuid, moniker.Value));
            }

            return null;
        }

        object IPreviewPaneService.GetPreviewPane(
            DiagnosticData data, IReadOnlyList<object> previewContent)
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
                    previewContent: previewContent, logIdVerbatimInTelemetry: false);
            }
            else
            {
                if (previewContent == null)
                {
                    // TODO: Mac, if we have title but no content, we should still display title/help link...
                    return null;
                }
            }

            var helpLinkUri = BrowserHelper.GetHelpLink(data);
            var helpLinkToolTip = BrowserHelper.GetHelpLinkToolTip(data.Id, helpLinkUri);

            Guid optionPageGuid = default;
            if (data.Properties.TryGetValue("OptionName", out _))
            {
                data.Properties.TryGetValue("OptionLanguage", out _);
                throw new NotImplementedException();
            }

            return new PreviewPane(
                severityIcon: null,//TODO: Mac GetSeverityIconForDiagnostic(diagnostic),
                id: data.Id, title: title,
                description: data.Description.ToString(CultureInfo.CurrentUICulture),
                helpLink: helpLinkUri,
                helpLinkToolTipText: helpLinkToolTip,
                previewContent: previewContent,
                logIdVerbatimInTelemetry: data.CustomTags.Contains(WellKnownDiagnosticTags.Telemetry),
                optionPageGuid: optionPageGuid);
        }
    }
}
