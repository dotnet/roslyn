// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.DotNet.FileBasedPrograms;

namespace Microsoft.CodeAnalysis.FileBasedPrograms;

internal interface IFileBasedProgramService : IWorkspaceService
{
    string GetArtifactsPath(string entryPointFileFullPath, string? dotNetSubdirectory = null);

    string GetTempSubdirectory(string? dotNetSubdirectory = null);
    IDictionary<string, string> GetGlobalBuildProperties();

    bool IsValidEntryPointPath(string entryPointFilePath);

    ValueTask<IProjectRootElement> LoadFileBasedAppProjectAsync(
        IBuildService buildService,
        IProjectCollection projectCollection,
        string entryPointFilePath,
        Action<string> reportError);

    /// <summary>
    /// If 'document[position]' is within a file-based app directive such as '#:include', get the resolved (absolute) file paths which are referenced by the directive.
    /// </summary>
    Task<ImmutableArray<string>> GetFileLevelDirectiveTargetPathsAsync(Document document, int position, CancellationToken cancellationToken);
}
