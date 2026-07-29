// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Host;

internal interface IMetadataCacheService : IWorkspaceService
{
    bool TryGetMetadata(
        string resolvedPath,
        MetadataImageKind kind,
        [NotNullWhen(true)] out Metadata? metadata);
}
