// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.OpenDocument;
using Microsoft.VisualStudio.LanguageServices.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal class VisualStudioOpenDocumentService : IOpenDocumentService
    {
        private readonly IServiceProvider _serviceProvider;

        public VisualStudioOpenDocumentService(SVsServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public bool OpenMetadataDocument(MetadataAsSourceFile metadataDocument)
        {
            var openDocumentService = IServiceProviderExtensions.GetService<SVsUIShellOpenDocument, IVsUIShellOpenDocument>(_serviceProvider);
            var success = openDocumentService.OpenDocumentViaProject(metadataDocument.FilePath, VSConstants.LOGVIEWID.TextView_guid, out _, out _, out _, out _);

            // Note that we intentionally do not call IVsWindowFrame.Show() since we do not want to make the file visible to the user, as per the method spec.
            return success == VSConstants.S_OK;
        }
    }
}
