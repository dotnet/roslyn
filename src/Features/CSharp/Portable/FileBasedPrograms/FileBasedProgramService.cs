// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
    public IProjectRootElement LoadFileBasedAppProject(
        IBuildService buildService,
        IProjectCollection projectCollection,
        string entryPointFilePath,
        Action<string> reportError)
    {
        var entryPointFileFullPath = Path.GetFullPath(entryPointFilePath);
        // TODO: don't hardcode TFM
        var virtualProjectBuilder = new VirtualProjectBuilder(buildService, entryPointFileFullPath, "net10.0");
        virtualProjectBuilder.CreateProjectInstance(
            projectCollection,
            (text, path, textSpan, message, innerException) => reportError($"{new SourceFile(path, text).GetLocationString(textSpan)}: {message}"),
            project: out _,
            out var projectRootElement,
            evaluatedDirectives: out _);
        return projectRootElement;
    }
}
