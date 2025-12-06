// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

extern alias BuildHost;

using BuildHost::Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Build.Execution;

namespace Microsoft.CodeAnalysis.ExternalAccess.HotReload.Api;

internal static class HotReloadProjectLoader
{
    public static ProjectInfo GetProjectInfo(ProjectInstance project, string languageName)
    {
        var projectFile = ProjectFile.Create(project.Pr, languageName);


    }
}
