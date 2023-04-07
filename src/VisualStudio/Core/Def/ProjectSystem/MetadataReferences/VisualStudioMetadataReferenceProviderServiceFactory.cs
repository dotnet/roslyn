// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    [ExportWorkspaceService(typeof(IMetadataService), ServiceLayer.Host), Shared]
    internal sealed class VsMetadataService : IMetadataService
    {
        // We will defer creation of this reference manager until we have to to avoid it being constructed too
        // early and potentially causing deadlocks. We do initialize it on the UI thread in the
        // VisualStudioWorkspaceImpl.DeferredState constructor to ensure it gets created there.

        private readonly Lazy<VisualStudioMetadataReferenceManager> _referenceManager;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VsMetadataService(
            Lazy<VisualStudioMetadataReferenceManager> referenceManager)
        {
            _referenceManager = referenceManager;
        }

        public PortableExecutableReference GetReference(string resolvedPath, MetadataReferenceProperties properties)
            => _referenceManager.Value.CreateMetadataReferenceSnapshot(resolvedPath, properties);
    }
}
