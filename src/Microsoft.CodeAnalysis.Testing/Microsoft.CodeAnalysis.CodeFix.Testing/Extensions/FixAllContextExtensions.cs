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
    private static readonly Func<Document?, TextSpan?, CodeFixProvider, FixAllScope, string?, IEnumerable<string>, FixAllContext.DiagnosticProvider, CancellationToken, FixAllContext> s_createFixAllContextDocument;

    static FixAllContextExtensions()
    {
        var constructorInfo = typeof(CompilationWithAnalyzers).GetConstructor(new[] { typeof(Document), typeof(TextSpan?), typeof(CodeFixProvider), typeof(FixAllScope), typeof(string), typeof(IEnumerable<string>), typeof(ImmutableArray<DiagnosticAnalyzer>), typeof(FixAllContext.DiagnosticProvider), typeof(CancellationToken) });
        if (constructorInfo is not null)
        {
            s_createFixAllContextDocument = (document, diagnosticSpan, codeFixProvider, scope, codeActionEquivalenceKey, diagnosticIds, fixAllDiagnosticProvider, cancellationToken) =>
            {
                return (FixAllContext)Activator.CreateInstance(typeof(FixAllContext), document, diagnosticSpan, codeFixProvider, scope, codeActionEquivalenceKey, diagnosticIds, fixAllDiagnosticProvider, cancellationToken)!;
            };
        }
        else
        {
            s_createFixAllContextDocument = (document, diagnosticSpan, codeFixProvider, scope, codeActionEquivalenceKey, diagnosticIds, fixAllDiagnosticProvider, cancellationToken) =>
                new FixAllContext(document, codeFixProvider, scope, codeActionEquivalenceKey, diagnosticIds, fixAllDiagnosticProvider, cancellationToken);
        }
    }

    public static FixAllContext Create(
        Document document,
        TextSpan? diagnosticSpan,
        CodeFixProvider codeFixProvider,
        FixAllScope scope,
        string? codeActionEquivalenceKey,
        IEnumerable<string> diagnosticIds,
        FixAllContext.DiagnosticProvider fixAllDiagnosticProvider,
        CancellationToken cancellationToken)
    {
        return s_createFixAllContextDocument(document, diagnosticSpan, codeFixProvider, scope, codeActionEquivalenceKey, diagnosticIds, fixAllDiagnosticProvider, cancellationToken);
    }
}
