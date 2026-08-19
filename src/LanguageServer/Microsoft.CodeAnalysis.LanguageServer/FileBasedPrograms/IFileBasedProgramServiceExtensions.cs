// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.FileBasedPrograms;

namespace Microsoft.CodeAnalysis.LanguageServer.FileBasedPrograms;

internal static class IFileBasedProgramServiceExtensions
{
    internal static string GetDiscoveryCacheDirectory(this IFileBasedProgramService fileBasedProgramService, string workspaceFolder)
        => fileBasedProgramService.GetArtifactsPath(workspaceFolder, "runfile-discovery");

    internal static string GetDiscoveryCacheRootDirectory(this IFileBasedProgramService fileBasedProgramService)
        => fileBasedProgramService.GetTempSubdirectory("runfile-discovery");
}
