// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Testing.Extensions;

internal static class FixAllContextExtensions
{
    private static readonly Func<Document?, TextSpan?, CodeFixProvider, FixAllScope, string?, IEnumerable<string>, DiagnosticSeverity, FixAllContext.DiagnosticProvider, CancellationToken, FixAllContext> s_createFixAllContextDocument;
    private static readonly Func<Project, CodeFixProvider, FixAllScope, string?, IEnumerable<string>, DiagnosticSeverity, FixAllContext.DiagnosticProvider, CancellationToken, FixAllContext> s_createFixAllContextProject;

    static FixAllContextExtensions()
    {
        var constructorInfo = typeof(FixAllContext).GetConstructor(new[] { typeof(Document), typeof(TextSpan?), typeof(CodeFixProvider), typeof(FixAllScope), typeof(string), typeof(IEnumerable<string>), typeof(DiagnosticSeverity), typeof(ImmutableArray<DiagnosticAnalyzer>), typeof(FixAllContext.DiagnosticProvider), typeof(CancellationToken) });
        if (constructorInfo is not null)
        {
            s_createFixAllContextDocument = (document, diagnosticSpan, codeFixProvider, scope, codeActionEquivalenceKey, diagnosticIds, minimumSeverity, fixAllDiagnosticProvider, cancellationToken) =>
            {
                return (FixAllContext)Activator.CreateInstance(typeof(FixAllContext), document, diagnosticSpan, codeFixProvider, scope, codeActionEquivalenceKey, diagnosticIds, minimumSeverity, fixAllDiagnosticProvider, cancellationToken)!;
            };
        }
        else
        {
            constructorInfo = typeof(FixAllContext).GetConstructor(new[] { typeof(Document), typeof(TextSpan?), typeof(CodeFixProvider), typeof(FixAllScope), typeof(string), typeof(IEnumerable<string>), typeof(ImmutableArray<DiagnosticAnalyzer>), typeof(FixAllContext.DiagnosticProvider), typeof(CancellationToken) });
            if (constructorInfo is not null)
            {
                s_createFixAllContextDocument = (document, diagnosticSpan, codeFixProvider, scope, codeActionEquivalenceKey, diagnosticIds, minimumSeverity, fixAllDiagnosticProvider, cancellationToken) =>
                {
                    return (FixAllContext)Activator.CreateInstance(typeof(FixAllContext), document, diagnosticSpan, codeFixProvider, scope, codeActionEquivalenceKey, diagnosticIds, fixAllDiagnosticProvider, cancellationToken)!;
                };
            }
            else
            {
                s_createFixAllContextDocument = (document, diagnosticSpan, codeFixProvider, scope, codeActionEquivalenceKey, diagnosticIds, minimumSeverity, fixAllDiagnosticProvider, cancellationToken) =>
                    new FixAllContext(document, codeFixProvider, scope, codeActionEquivalenceKey, diagnosticIds, fixAllDiagnosticProvider, cancellationToken);
            }
        }

        constructorInfo = typeof(FixAllContext).GetConstructor(new[] { typeof(Project), typeof(CodeFixProvider), typeof(FixAllScope), typeof(string), typeof(IEnumerable<string>), typeof(DiagnosticSeverity), typeof(ImmutableArray<DiagnosticAnalyzer>), typeof(FixAllContext.DiagnosticProvider), typeof(CancellationToken) });
        if (constructorInfo is not null)
        {
            s_createFixAllContextProject = (project, codeFixProvider, scope, codeActionEquivalenceKey, diagnosticIds, minimumSeverity, fixAllDiagnosticProvider, cancellationToken) =>
            {
                return (FixAllContext)Activator.CreateInstance(typeof(FixAllContext), project, codeFixProvider, scope, codeActionEquivalenceKey, diagnosticIds, minimumSeverity, fixAllDiagnosticProvider, cancellationToken)!;
            };
        }
        else
        {
            s_createFixAllContextProject = (project, codeFixProvider, scope, codeActionEquivalenceKey, diagnosticIds, minimumSeverity, fixAllDiagnosticProvider, cancellationToken) =>
                new FixAllContext(project, codeFixProvider, scope, codeActionEquivalenceKey, diagnosticIds, fixAllDiagnosticProvider, cancellationToken);
        }
    }

    public static FixAllContext Create(
        Document document,
        TextSpan? diagnosticSpan,
        CodeFixProvider codeFixProvider,
        FixAllScope scope,
        string? codeActionEquivalenceKey,
        IEnumerable<string> diagnosticIds,
        DiagnosticSeverity minimumSeverity,
        FixAllContext.DiagnosticProvider fixAllDiagnosticProvider,
        CancellationToken cancellationToken)
    {
        return s_createFixAllContextDocument(document, diagnosticSpan, codeFixProvider, scope, codeActionEquivalenceKey, diagnosticIds, minimumSeverity, fixAllDiagnosticProvider, cancellationToken);
    }

    public static FixAllContext Create(
        Project project,
        CodeFixProvider codeFixProvider,
        FixAllScope scope,
        string? codeActionEquivalenceKey,
        IEnumerable<string> diagnosticIds,
        DiagnosticSeverity minimumSeverity,
        FixAllContext.DiagnosticProvider fixAllDiagnosticProvider,
        CancellationToken cancellationToken)
    {
        return s_createFixAllContextProject(project, codeFixProvider, scope, codeActionEquivalenceKey, diagnosticIds, minimumSeverity, fixAllDiagnosticProvider, cancellationToken);
    }
}
