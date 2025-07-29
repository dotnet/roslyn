// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.MSBuild;

[DataContract]
internal sealed class MSBuildLocation(string path, string version)
{
    /// <summary>
    /// This is the path to the directory containing the MSBuild binaries.
    /// </summary>
    /// <remarks>
    /// When running on .NET this will be the path to the SDK required for loading projects.
    /// </remarks>
    [DataMember(Order = 0)]
    public string Path { get; } = path;

    /// <summary>
    /// This is the version of MSBuild at this location.
    /// </summary>
    [DataMember(Order = 1)]
    public string Version { get; } = version;
}
