// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.MetadataAsSource;

#nullable enable

namespace Microsoft.CodeAnalysis.OpenDocument
{
    internal interface IOpenDocumentService : IWorkspaceService
    {
        /// <summary>
        /// Opens the metadata file silently.
        /// </summary>
        /// <returns>true on success.</returns>
        /// <remarks>
        /// Silently opens the file, meaning that the user may not even realize the file has been opened as we
        /// intentionally do not make it visible to them.
        /// </remarks>
        bool OpenMetadataDocument(MetadataAsSourceFile metadataDocument);
    }
}
