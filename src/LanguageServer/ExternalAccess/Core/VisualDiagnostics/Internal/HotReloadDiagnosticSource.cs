// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics.Contracts;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics.Internal;

internal sealed class HotReloadDiagnosticSource(IHotReloadDiagnosticSource source, TextDocument textDocument) : IDiagnosticSource
{
    public async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(RequestContext context, CancellationToken cancellationToken)
    {
        var diagnostics = await source.GetDiagnosticsAsync(new HotReloadRequestContext(context), cancellationToken).ConfigureAwait(false);
        var result = diagnostics.SelectAsArray(diagnostic => DiagnosticData.Create(diagnostic, textDocument));
        return result;
    }

    public TextDocumentIdentifier? GetDocumentIdentifier() => new() { DocumentUri = textDocument.GetURI() };
    public ProjectOrDocumentId GetId() => new(textDocument.Id);
    public Project GetProject() => textDocument.Project;
    public string ToDisplayString() => $"{this.GetType().Name}: {textDocument.FilePath ?? textDocument.Name} in {textDocument.Project.Name}";
}
