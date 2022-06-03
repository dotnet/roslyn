// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal readonly struct VSTypeScriptDocumentNavigationServiceWrapper
    {
        private readonly IDocumentNavigationService _underlyingObject;
        private readonly IWorkspaceThreadingServiceProvider _threadingProvider;

        public VSTypeScriptDocumentNavigationServiceWrapper(
            IDocumentNavigationService underlyingObject,
            IWorkspaceThreadingServiceProvider threadingProvider)
        {
            _underlyingObject = underlyingObject;
            _threadingProvider = threadingProvider;
        }

        public static VSTypeScriptDocumentNavigationServiceWrapper Create(Workspace workspace)
            => new(workspace.Services.GetRequiredService<IDocumentNavigationService>(),
                   workspace.Services.GetRequiredService<IWorkspaceThreadingServiceProvider>());

        [Obsolete("Call overload that takes a CancellationToken", error: false)]
        public bool TryNavigateToPosition(Workspace workspace, DocumentId documentId, int position, int virtualSpace = 0, OptionSet? options = null)
            => this.TryNavigateToPosition(workspace, documentId, position, virtualSpace, options, CancellationToken.None);

        [Obsolete("Call overload that doesn't take options", error: false)]
        public bool TryNavigateToPosition(Workspace workspace, DocumentId documentId, int position, int virtualSpace, OptionSet? options, CancellationToken cancellationToken)
        {
            var obj = _underlyingObject;
            return _threadingProvider.Service.Run(async () =>
            {
                var location = await obj.GetLocationForPositionAsync(
                    workspace, documentId, position, virtualSpace, cancellationToken).ConfigureAwait(false);
                return location != null &&
                    await location.NavigateToAsync(NavigationOptions.Default, cancellationToken).ConfigureAwait(false);
            });
        }

        public bool TryNavigateToPosition(Workspace workspace, DocumentId documentId, int position, int virtualSpace, CancellationToken cancellationToken)
        {
            var obj = _underlyingObject;
            return _threadingProvider.Service.Run(async () =>
            {
                var location = await obj.GetLocationForPositionAsync(
                    workspace, documentId, position, virtualSpace, cancellationToken).ConfigureAwait(false);
                return location != null &&
                    await location.NavigateToAsync(NavigationOptions.Default, cancellationToken).ConfigureAwait(false);
            });
        }
    }
}
