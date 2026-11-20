// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

namespace Microsoft.CodeAnalysis.LanguageServer.FileBasedPrograms;

[Export(typeof(IDiagnosticSourceProvider)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class FileBasedProgramsDiagnosticSourceProvider()
    : AbstractProjectLoadDiagnosticSourceProvider("FileBasedProgramsProjectLoad")
{
    protected override LanguageServerProjectLoader? GetProjectLoader(RequestContext context)
        => (LanguageServerProjectLoader?)context.GetService<ILspMiscellaneousFilesWorkspaceProvider>();
}
