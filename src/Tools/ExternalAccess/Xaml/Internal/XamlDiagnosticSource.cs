// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Xaml;

internal sealed class XamlDiagnosticSource(IXamlDiagnosticSource xamlDiagnosticSource, TextDocument document) : IDiagnosticSource
{
    bool IDiagnosticSource.IsLiveSource() => true;
    Project IDiagnosticSource.GetProject() => document.Project;
    ProjectOrDocumentId IDiagnosticSource.GetId() => new(document.Id);
    TextDocumentIdentifier? IDiagnosticSource.GetDocumentIdentifier() => new() { Uri = document.GetURI() };
    string IDiagnosticSource.ToDisplayString() => $"{this.GetType().Name}: {document.FilePath ?? document.Name} in {document.Project.Name}";

    async Task<ImmutableArray<DiagnosticData>> IDiagnosticSource.GetDiagnosticsAsync(RequestContext context, CancellationToken cancellationToken)
    {
        var xamlRequestContext = XamlRequestContext.FromRequestContext(context);
        var diagnostics = await xamlDiagnosticSource.GetDiagnosticsAsync(xamlRequestContext, cancellationToken).ConfigureAwait(false);
        var result = diagnostics.Select(e => DiagnosticData.Create(e, document)).ToImmutableArray();
        return result;
    }
}
