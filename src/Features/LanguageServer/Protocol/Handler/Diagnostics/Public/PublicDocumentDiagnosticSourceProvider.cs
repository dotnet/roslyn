// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.DiagnosticSources;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.Public;

[ExportDiagnosticSourceProvider, Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class PublicDocumentDiagnosticSourceProvider(
    [Import] IGlobalOptionService globalOptions,
    [Import] IDiagnosticAnalyzerService diagnosticAnalyzerService) : IDiagnosticSourceProvider
{
    public const string NonLocalSource = "nonLocal_B69807DB-28FB-4846-884A-1152E54C8B62";
    private static readonly ImmutableArray<string> sourceNames = [NonLocalSource];

    public bool IsDocument => true;
    public ImmutableArray<string> SourceNames => sourceNames;

    public ValueTask<ImmutableArray<IDiagnosticSource>> CreateDiagnosticSourcesAsync(RequestContext context, string sourceName, CancellationToken cancellationToken)
    {
        var nonLocalDocumentDiagnostics = sourceName == NonLocalSource;
        var result = DocumentDiagnosticSourceProvider.GetDiagnosticSources(diagnosticAnalyzerService, DiagnosticKind.All, nonLocalDocumentDiagnostics, taskList: false, context, globalOptions);
        return new(result);
    }
}
