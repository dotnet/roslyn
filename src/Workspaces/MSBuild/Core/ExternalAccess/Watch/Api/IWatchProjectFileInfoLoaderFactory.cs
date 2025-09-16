// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.MSBuild.ExternalAccess.Watch.Api;

internal interface IWatchProjectFileInfoLoaderFactory
{
    IWatchProjectFileInfoLoader Create(ImmutableDictionary<string, string> properties);
}
