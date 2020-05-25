// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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
            => new VSTypeScriptDocumentNavigationServiceWrapper(workspace.Services.GetRequiredService<IDocumentNavigationService>());

        public bool TryNavigateToPosition(Workspace workspace, DocumentId documentId, int position, int virtualSpace = 0, OptionSet? options = null)
            => _underlyingObject.TryNavigateToPosition(workspace, documentId, position, virtualSpace, options);
    }
}
