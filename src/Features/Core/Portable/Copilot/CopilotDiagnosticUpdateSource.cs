// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Copilot;

/// <summary>
/// Diagnostic update source to report Copilot code analysis diagnostics in the error list
/// in Non-LSP pull diagnostics mode.
/// </summary>
[Export(typeof(CopilotDiagnosticUpdateSource)), Shared]
internal sealed class CopilotDiagnosticUpdateSource : IDiagnosticUpdateSource
{
    private readonly ConditionalWeakTable<Document, ConcurrentDictionary<string, ImmutableArray<DiagnosticData>>> _diagnosticsByPromptTitle = new();

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CopilotDiagnosticUpdateSource(IDiagnosticUpdateSourceRegistrationService registrationService)
    {
        registrationService.Register(this);
    }

    public event EventHandler<ImmutableArray<DiagnosticsUpdatedArgs>>? DiagnosticsUpdated;
    public event EventHandler? DiagnosticsCleared;

    /// <summary>
    /// This implementation reports diagnostics via <see cref="DiagnosticsUpdated"/> event.
    /// </summary>
    public bool SupportGetDiagnostics => false;

    public ValueTask<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(Workspace workspace, ProjectId? projectId, DocumentId? documentId, object? id, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default)
        => new(ImmutableArray<DiagnosticData>.Empty);

    public void ClearDiagnostics()
    {
        DiagnosticsCleared?.Invoke(this, EventArgs.Empty);
#if NET
        _diagnosticsByPromptTitle.Clear();
#endif
    }

    /// <summary>
    /// Reports given set of document diagnostics. 
    /// </summary>
    public void ReportDiagnostics(Document document, string promptTitle, ImmutableArray<DiagnosticData> diagnostics)
    {
        Debug.Assert(diagnostics.All(d => d.DocumentId == document.Id));

        var updateEvent = DiagnosticsUpdated;
        if (updateEvent == null)
        {
            return;
        }

        var diagnosticsMap = _diagnosticsByPromptTitle.GetOrCreateValue(document);
        diagnosticsMap[promptTitle] = diagnostics;
        var allDiagnostics = diagnosticsMap.SelectManyAsArray(kvp => kvp.Value);

        using var argsBuilder = TemporaryArray<DiagnosticsUpdatedArgs>.Empty;

        argsBuilder.Add(DiagnosticsUpdatedArgs.DiagnosticsCreated(
            (this, document.Id),
            document.Project.Solution.Workspace,
            document.Project.Solution,
            document.Project.Id,
            document.Id,
            allDiagnostics));

        updateEvent(this, argsBuilder.ToImmutableAndClear());
    }
}
