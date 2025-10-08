// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.MSBuild.ExternalAccess.Watch.Api;

/// <summary>
/// Represents a reference to another project file.
/// </summary>
internal readonly struct WatchProjectFileReference
{
    internal ProjectFileReference UnderlyingObject { get; }

    internal WatchProjectFileReference(ProjectFileReference underlyingObject)
    {
        UnderlyingObject = underlyingObject;
    }

    public WatchProjectFileReference(string path, ImmutableArray<string> aliases, bool referenceOutputAssembly)
        : this(new(path, aliases, referenceOutputAssembly))
    {
    }

    /// <summary>
    /// The path on disk to the other project file. 
    /// This path may be relative to the referencing project's file or an absolute path.
    /// </summary>
    public string Path => UnderlyingObject.Path;

    /// <summary>
    /// The aliases assigned to this reference, if any.
    /// </summary>
    public ImmutableArray<string> Aliases => UnderlyingObject.Aliases;

    /// <summary>
    /// The value of "ReferenceOutputAssembly" metadata.
    /// </summary>
    public bool ReferenceOutputAssembly => UnderlyingObject.ReferenceOutputAssembly;
}
