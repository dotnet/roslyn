// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host;
using Microsoft.DotNet.FileBasedPrograms;

namespace Microsoft.CodeAnalysis.FileBasedPrograms;

internal interface IFileBasedProgramService : IWorkspaceService
{
    IDictionary<string, string> GetGlobalBuildProperties();

    bool IsValidEntryPointPath(string entryPointFilePath);

    IProjectRootElement LoadFileBasedAppProject(
        IBuildService buildService,
        IProjectCollection projectCollection,
        string entryPointFilePath,
        Action<string> reportError);
}
