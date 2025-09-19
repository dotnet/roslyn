// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.MSBuild.ExternalAccess.Watch.Api;

internal readonly struct WatchFileGlobs
{
    internal FileGlobs UnderlyingObject { get; }

    internal WatchFileGlobs(FileGlobs underlyingObject)
    {
        UnderlyingObject = underlyingObject;
    }

    public WatchFileGlobs(ImmutableArray<string> includes, ImmutableArray<string> excludes, ImmutableArray<string> removes)
        : this(new(includes, excludes, removes))
    {

    }

    public ImmutableArray<string> Includes => UnderlyingObject.Includes;
    public ImmutableArray<string> Excludes => UnderlyingObject.Excludes;
    public ImmutableArray<string> Removes => UnderlyingObject.Removes;
}
