// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.DiagnosticSources;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

[ExportDiagnosticSourceProvider, Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class WorkspaceDiagnosticSourceProvider(
    [Import] IDiagnosticAnalyzerService diagnosticAnalyzerService,
    [Import] IGlobalOptionService globalOptions)
    : AbstractWorkspaceDiagnosticSourceProvider(diagnosticAnalyzerService, globalOptions,
        [PullDiagnosticCategories.EditAndContinue, PullDiagnosticCategories.WorkspaceDocumentsAndProject])
{
}
