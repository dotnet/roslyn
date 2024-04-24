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

namespace Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics.Internal
{
    internal class HotReloadDiagnosticSource(IHotReloadDiagnosticSource source) : IDiagnosticSource
    {
        async Task<ImmutableArray<DiagnosticData>> IDiagnosticSource.GetDiagnosticsAsync(RequestContext context, CancellationToken cancellationToken)
        {
            var diagnostics = await source.GetDiagnosticsAsync(new HotReloadRequestContext(context), cancellationToken).ConfigureAwait(false);
            var document = source.TextDocument;
            var result = diagnostics.Select(e => DiagnosticData.Create(e, document)).ToImmutableArray();
            return result;
        }

        TextDocumentIdentifier? IDiagnosticSource.GetDocumentIdentifier() => new() { Uri = source.TextDocument.GetURI() };
        ProjectOrDocumentId IDiagnosticSource.GetId() => new(source.TextDocument.Id);
        Project IDiagnosticSource.GetProject() => source.TextDocument.Project;
        bool IDiagnosticSource.IsLiveSource() => true;
        string IDiagnosticSource.ToDisplayString() => $"{this.GetType().Name}: {source.TextDocument.FilePath ?? source.TextDocument.Name} in {source.TextDocument.Project.Name}";
    }
}
