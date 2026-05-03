// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Host;

/// <summary>
/// Provides metadata references for files on disk.
/// </summary>
internal interface IMetadataService : IWorkspaceService
{
    /// <summary>
    /// Returns a <see cref="PortableExecutableReference"/> reference backed by the file at <paramref name="resolvedPath"/>.
    /// If the file does not exist a reference is still returned but reading it will throw.
    /// </summary>
    PortableExecutableReference GetReference(string resolvedPath, MetadataReferenceProperties properties);
}
