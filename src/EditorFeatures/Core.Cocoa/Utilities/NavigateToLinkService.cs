// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using AppKit;
using Foundation;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditorFeatures.Cocoa
{
    [ExportWorkspaceService(typeof(INavigateToLinkService), layer: ServiceLayer.Host)]
    [Shared]
    internal sealed class NavigateToLinkService : INavigateToLinkService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public NavigateToLinkService()
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

            NSWorkspace.SharedWorkspace.OpenUrl(new NSUrl(uri.AbsoluteUri));
            return SpecializedTasks.True;
        }
    }
}
