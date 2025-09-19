// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.MSBuild.ExternalAccess.Watch.Api;

internal readonly record struct WatchPackageReference
{
    internal PackageReference UnderlyingObject { get; }

    internal WatchPackageReference(PackageReference underlyingObject)
    {
        UnderlyingObject = underlyingObject;
    }

    public WatchPackageReference(string name, string versionRange)
        : this(new(name, versionRange))
    {
    }

    public string Name => UnderlyingObject.Name;
    public string VersionRange => UnderlyingObject.VersionRange;
}
