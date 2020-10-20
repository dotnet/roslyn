// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal class VisualStudioMetadataAsSourceFileLSPService : IMetadataAsSourceFileLSPService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IMetadataAsSourceFileService _metadataAsSourceFileService;

        public VisualStudioMetadataAsSourceFileLSPService(SVsServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            var componentModel = IServiceProviderExtensions.GetService<SComponentModel, IComponentModel>(_serviceProvider);
            _metadataAsSourceFileService = componentModel.GetService<IMetadataAsSourceFileService>();
        }

        public async Task<MetadataAsSourceFile?> GetAndOpenGeneratedFileAsync(ISymbol symbol, Project project, CancellationToken cancellationToken)
        {
            var result = await _metadataAsSourceFileService.GetGeneratedFileAsync(
                project, symbol, allowDecompilation: false, cancellationToken).ConfigureAwait(false);

            var openDocumentService = IServiceProviderExtensions.GetService<SVsUIShellOpenDocument, IVsUIShellOpenDocument>(_serviceProvider);
            openDocumentService.OpenDocumentViaProject(result.FilePath, VSConstants.LOGVIEWID.TextView_guid, out _, out _, out _, out _);

            // Note that we intentionally do not call IVsWindowFrame.Show() since we do not want to make the file visible to the user, as per the method spec.
            return result;
        }
    }
}
