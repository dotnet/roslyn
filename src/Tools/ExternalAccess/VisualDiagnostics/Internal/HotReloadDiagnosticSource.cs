// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics.Contracts;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;
using Roslyn.LanguageServer.Protocol;
using Microsoft.CodeAnalysis.LanguageServer;

namespace Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics.Internal
{
    internal class HotReloadDiagnosticSource(TextDocument document, ImmutableArray<Diagnostic> errors) : IDiagnosticSource
    {
        Task<ImmutableArray<DiagnosticData>> IDiagnosticSource.GetDiagnosticsAsync(RequestContext context, CancellationToken cancellationToken)
        {
            var result = errors.Select(e => DiagnosticData.Create(e, document)).ToImmutableArray();
            return Task.FromResult(result);
        }

        TextDocumentIdentifier? IDiagnosticSource.GetDocumentIdentifier() => new() { Uri = document.GetURI() };
        ProjectOrDocumentId IDiagnosticSource.GetId() => new(document.Id);
        Project IDiagnosticSource.GetProject() => document.Project;
        bool IDiagnosticSource.IsLiveSource() => true;
        string IDiagnosticSource.ToDisplayString() => $"{this.GetType().Name}: {document.FilePath ?? document.Name} in {document.Project.Name}";
    }
}
