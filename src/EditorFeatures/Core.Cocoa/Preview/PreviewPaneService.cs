// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Preview
{
    [ExportWorkspaceServiceFactory(typeof(IPreviewPaneService), ServiceLayer.Host), Shared]
    internal class PreviewPaneService : ForegroundThreadAffinitizedObject, IPreviewPaneService, IWorkspaceServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public PreviewPaneService(IThreadingContext threadingContext)
            : base(threadingContext)
        {
        }

        IWorkspaceService IWorkspaceServiceFactory.CreateService(HostWorkspaceServices workspaceServices)
        {
            return this;
        }

        object? IPreviewPaneService.GetPreviewPane(DiagnosticData? data, IReadOnlyList<object>? previewContent)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.Message))
            {
                if (previewContent == null)
                {
                    // Bail out in cases where there is nothing to put in the header section
                    // of the preview pane and no preview content (i.e. no diff view) either.
                    return null;
                }

                return new PreviewPane(id: null, title: null, helpLink: null, helpLinkToolTipText: null, previewContent);
            }

            if (previewContent == null)
            {
                // TODO: Mac, if we have title but no content, we should still display title/help link...
                return null;
            }

            var helpLinkUri = data.GetValidHelpLinkUri();

            return new PreviewPane(
                id: data.Id,
                title: data.Message,
                helpLink: helpLinkUri,
                helpLinkToolTipText: (helpLinkUri != null) ? string.Format(EditorFeaturesResources.Get_help_for_0, data.Id) : null,
                previewContent: previewContent);
        }
    }
}
