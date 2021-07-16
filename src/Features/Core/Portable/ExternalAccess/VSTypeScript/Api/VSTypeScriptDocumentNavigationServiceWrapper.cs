// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal readonly struct VSTypeScriptDocumentNavigationServiceWrapper
    {
        private readonly IDocumentNavigationService _underlyingObject;

        public VSTypeScriptDocumentNavigationServiceWrapper(IDocumentNavigationService underlyingObject)
            => _underlyingObject = underlyingObject;

        public static VSTypeScriptDocumentNavigationServiceWrapper Create(Workspace workspace)
            => new(workspace.Services.GetRequiredService<IDocumentNavigationService>());

        [Obsolete("Call overload that takes a CancellationToken", error: false)]
        public bool TryNavigateToPosition(Workspace workspace, DocumentId documentId, int position, int virtualSpace = 0, OptionSet? options = null)
            => this.TryNavigateToPosition(workspace, documentId, position, virtualSpace, options, CancellationToken.None);

        /// <inheritdoc cref="IDocumentNavigationService.TryNavigateToPosition"/>
        public bool TryNavigateToPosition(Workspace workspace, DocumentId documentId, int position, int virtualSpace, OptionSet? options, CancellationToken cancellationToken)
            => _underlyingObject.TryNavigateToPosition(workspace, documentId, position, virtualSpace, options, cancellationToken);
    }
}
