// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.DotNet.FileBasedPrograms;

namespace Microsoft.CodeAnalysis.FileBasedPrograms;

[Export(typeof(IFileBasedProgramService)), Shared]
[ExportWorkspaceService(typeof(IFileBasedProgramService))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class FileBasedProgramService() : IFileBasedProgramService
{
    public string GetArtifactsPath(string entryPointFileFullPath, string? dotNetSubdirectory = null)
        => VirtualProjectBuilder.GetArtifactsPath(entryPointFileFullPath, dotNetSubdirectory);

    public string GetTempSubdirectory(string? dotNetSubdirectory = null)
        => VirtualProjectBuilder.GetTempSubdirectory(dotNetSubdirectory);

    public IDictionary<string, string> GetGlobalBuildProperties()
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in VirtualProjectBuilder.GetGlobalBuildProperties())
        {
            result.Add(kvp.Key, kvp.Value);
        }
        return result;
    }

    public bool IsValidEntryPointPath(string entryPointFilePath)
        => VirtualProjectBuilder.IsValidEntryPointPath(entryPointFilePath);

    public IProjectRootElement LoadFileBasedAppProject(
        IBuildService buildService,
        IProjectCollection projectCollection,
        string entryPointFilePath,
        Action<string> reportError)
    {
        var entryPointFileFullPath = Path.GetFullPath(entryPointFilePath);
        var virtualProjectBuilder = new VirtualProjectBuilder(buildService, entryPointFileFullPath, FileBasedAppConstants.CurrentTargetFramework);
        virtualProjectBuilder.CreateProjectInstance(
            projectCollection,
            (text, path, textSpan, message, innerException) => reportError($"{new SourceFile(path, text).GetLocationString(textSpan)}: {message}"),
            project: out _,
            out var projectRootElement,
            evaluatedDirectives: out _);
        return projectRootElement;
    }
}
