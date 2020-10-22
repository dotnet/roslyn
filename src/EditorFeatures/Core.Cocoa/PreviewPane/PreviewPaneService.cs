// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Globalization;
using System.Linq;
using AppKit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.PreviewPane
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

        private NSImage GetSeverityIconForDiagnostic(DiagnosticData diagnostic)
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
            if (data.Properties.TryGetValue("OptionName", out var optionName))
            {
                data.Properties.TryGetValue("OptionLanguage", out var optionLanguage);
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
