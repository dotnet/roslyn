// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Utilities
{
    [ExportWorkspaceService(typeof(INavigateToLinkService), layer: ServiceLayer.Host)]
    [Shared]
    internal sealed class VisualStudioNavigateToLinkService : INavigateToLinkService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioNavigateToLinkService()
        {
        }

        public Task<bool> TryNavigateToLinkAsync(Uri uri, CancellationToken cancellationToken)
        {
            if (!uri.IsAbsoluteUri)
            {
                return SpecializedTasks.False;
            }

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                return SpecializedTasks.False;
            }

            BrowserHelper.StartBrowser(uri);
            return SpecializedTasks.True;
        }
    }
}
