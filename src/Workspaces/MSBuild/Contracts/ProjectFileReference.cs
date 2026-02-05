// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.MSBuild;

/// <summary>
/// Represents a reference to another project file.
/// </summary>
[DataContract]
internal sealed class ProjectFileReference
{
    /// <summary>
    /// The path on disk to the other project file. 
    /// This path may be relative to the referencing project's file or an absolute path.
    /// </summary>
    [DataMember(Order = 0)]
    public string Path { get; }

    /// <summary>
    /// The aliases assigned to this reference, if any.
    /// </summary>
    [DataMember(Order = 1)]
    public string[] Aliases { get; }

    /// <summary>
    /// The value of "ReferenceOutputAssembly" metadata.
    /// </summary>
    [DataMember(Order = 2)]
    public bool ReferenceOutputAssembly { get; }

    public ProjectFileReference(string path, string[] aliases, bool referenceOutputAssembly)
    {
        Debug.Assert(aliases is not null);

        Path = path;
        Aliases = aliases;
        ReferenceOutputAssembly = referenceOutputAssembly;
    }
}
