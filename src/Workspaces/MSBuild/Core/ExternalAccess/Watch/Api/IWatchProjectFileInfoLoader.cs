// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.MSBuild.ExternalAccess.Watch.Api;

internal interface IWatchProjectFileInfoLoader
{
    Task<LoadProjectFileInfosResult> LoadProjectFileInfosAsync(
        string projectFilePath,
        string languageName,
        CancellationToken cancellationToken);

    Task<string?> TryGetProjectOutputPathAsync(string projectFilePath, CancellationToken cancellationToken);
}

internal readonly record struct LoadProjectFileInfosResult(
    ImmutableArray<WatchProjectFileInfo> ProjectFileInfos,
    ImmutableArray<WatchDiagnosticLogItem> DiagnosticItems);
