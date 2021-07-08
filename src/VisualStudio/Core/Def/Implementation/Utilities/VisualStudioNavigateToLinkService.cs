// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Shell;
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

            StartBrowser(uri);
            return SpecializedTasks.True;
        }

        public static void StartBrowser(string uri)
            => VsShellUtilities.OpenSystemBrowser(uri);

        public static void StartBrowser(Uri uri)
            => VsShellUtilities.OpenSystemBrowser(uri.AbsoluteUri);
    }
}
