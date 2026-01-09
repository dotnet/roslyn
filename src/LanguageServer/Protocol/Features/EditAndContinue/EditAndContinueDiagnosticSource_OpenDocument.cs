// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal static partial class EditAndContinueDiagnosticSource
{
    private sealed class OpenDocumentSource(Document document) : AbstractDocumentDiagnosticSource<Document>(document)
    {
        public override Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(RequestContext context, CancellationToken cancellationToken)
            => GetDiagnosticsAsync(Document, cancellationToken);

        public static async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(Document document, CancellationToken cancellationToken)
        {
            var solution = document.Project.Solution;
            var services = solution.Services;

            // Do not report EnC diagnostics for a non-host workspace, or if Hot Reload/EnC session is not active.
            if (solution.WorkspaceKind != WorkspaceKind.Host ||
                services.GetService<IEditAndContinueWorkspaceService>()?.SessionTracker is not { IsSessionActive: true } sessionStateTracker)
            {
                return [];
            }

            var applyDiagnostics = sessionStateTracker.ApplyChangesDiagnostics.WhereAsArray(static (data, id) => data.DocumentId == id, document.Id);

            var proxy = new RemoteEditAndContinueServiceProxy(services);
            var spanLocator = services.GetService<IActiveStatementSpanLocator>();

            var activeStatementSpanProvider = spanLocator != null
                ? new ActiveStatementSpanProvider((documentId, filePath, cancellationToken) => spanLocator.GetSpansAsync(solution, documentId, filePath, cancellationToken))
                : static async (_, _, _) => ImmutableArray<ActiveStatementSpan>.Empty;

            var rudeEditDiagnostics = await proxy.GetDocumentDiagnosticsAsync(document, activeStatementSpanProvider, cancellationToken).ConfigureAwait(false);

            return applyDiagnostics.AddRange(rudeEditDiagnostics);
        }
    }

    public static IDiagnosticSource CreateOpenDocumentSource(Document document)
        => new OpenDocumentSource(document);

    internal static Task<ImmutableArray<DiagnosticData>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken)
        => OpenDocumentSource.GetDiagnosticsAsync(document, cancellationToken);
}
