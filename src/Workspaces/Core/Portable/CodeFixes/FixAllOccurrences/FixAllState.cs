// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes;

internal sealed partial class FixAllState : CommonFixAllState<CodeFixProvider, FixAllProvider, FixAllState>
{
    public override FixAllKind FixAllKind => FixAllKind.CodeFix;

    public FixAllContext.DiagnosticProvider DiagnosticProvider { get; }

    public ImmutableHashSet<string> DiagnosticIds { get; }

    // Note: DiagnosticSpan can be null from the back-compat public constructor of FixAllContext.
    public TextSpan? DiagnosticSpan { get; }

    internal FixAllState(
        FixAllProvider fixAllProvider,
        TextSpan? diagnosticSpan,
        Document? document,
        Project project,
        CodeFixProvider codeFixProvider,
        FixAllScope scope,
        string? codeActionEquivalenceKey,
        IEnumerable<string> diagnosticIds,
        FixAllContext.DiagnosticProvider fixAllDiagnosticProvider)
        : base(fixAllProvider, document, project, codeFixProvider, scope, codeActionEquivalenceKey)
    {
        // We need the trigger diagnostic span for span based fix all scopes, i.e. FixAllScope.ContainingMember and FixAllScope.ContainingType
        Debug.Assert(diagnosticSpan.HasValue || scope is not FixAllScope.ContainingMember or FixAllScope.ContainingType);

        DiagnosticSpan = diagnosticSpan;
        DiagnosticIds = [.. diagnosticIds];
        DiagnosticProvider = fixAllDiagnosticProvider;
    }

    internal bool IsFixMultiple => DiagnosticProvider is FixMultipleDiagnosticProvider;

    protected override FixAllState With(Document? document, Project project, FixAllScope scope, string? codeActionEquivalenceKey)
        => new(
            FixAllProvider,
            DiagnosticSpan,
            document,
            project,
            Provider,
            scope,
            codeActionEquivalenceKey,
            DiagnosticIds,
            DiagnosticProvider);

    #region FixMultiple

    internal static FixAllState Create(
        FixAllProvider fixAllProvider,
        ImmutableDictionary<Document, ImmutableArray<Diagnostic>> diagnosticsToFix,
        CodeFixProvider codeFixProvider,
        string? codeActionEquivalenceKey)
    {
        var triggerDocument = diagnosticsToFix.First().Key;
        var diagnosticSpan = diagnosticsToFix.First().Value.FirstOrDefault()?.Location.SourceSpan;
        var diagnosticIds = GetDiagnosticsIds(diagnosticsToFix.Values);
        var diagnosticProvider = new FixMultipleDiagnosticProvider(diagnosticsToFix);
        return new FixAllState(
            fixAllProvider,
            diagnosticSpan,
            triggerDocument,
            triggerDocument.Project,
            codeFixProvider,
            FixAllScope.Custom,
            codeActionEquivalenceKey,
            diagnosticIds,
            diagnosticProvider);
    }

    internal static FixAllState Create(
        FixAllProvider fixAllProvider,
        ImmutableDictionary<Project, ImmutableArray<Diagnostic>> diagnosticsToFix,
        CodeFixProvider codeFixProvider,
        string? codeActionEquivalenceKey)
    {
        var triggerProject = diagnosticsToFix.First().Key;
        var diagnosticIds = GetDiagnosticsIds(diagnosticsToFix.Values);
        var diagnosticProvider = new FixMultipleDiagnosticProvider(diagnosticsToFix);
        return new FixAllState(
            fixAllProvider,
            diagnosticSpan: null,
            document: null,
            triggerProject,
            codeFixProvider,
            FixAllScope.Custom,
            codeActionEquivalenceKey,
            diagnosticIds,
            diagnosticProvider);
    }

    private static ImmutableHashSet<string> GetDiagnosticsIds(IEnumerable<ImmutableArray<Diagnostic>> diagnosticsCollection)
    {
        var uniqueIds = ImmutableHashSet.CreateBuilder<string>();
        foreach (var diagnostics in diagnosticsCollection)
        {
            foreach (var diagnostic in diagnostics)
            {
                uniqueIds.Add(diagnostic.Id);
            }
        }

        return uniqueIds.ToImmutable();
    }

    #endregion
}
