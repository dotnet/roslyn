// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.CodeActions
{
    internal static class OmniSharpCodeFixContextFactory
    {
        public static CodeFixContext CreateCodeFixContext(
            Document document,
            TextSpan span,
            ImmutableArray<Diagnostic> diagnostics,
            Action<CodeAction, ImmutableArray<Diagnostic>> registerCodeFix,
#pragma warning disable IDE0060 // Remove unused parameter
            OmniSharpCodeActionOptions options,
#pragma warning restore IDE0060 // Remove unused parameter
            CancellationToken cancellationToken)
            => new(document, span, diagnostics, registerCodeFix, CodeActionOptions.DefaultProvider, cancellationToken);

        public static CodeAnalysis.CodeRefactorings.CodeRefactoringContext CreateCodeRefactoringContext(
            Document document,
            TextSpan span,
            Action<CodeAction, TextSpan?> registerRefactoring,
#pragma warning disable IDE0060 // Remove unused parameter
            OmniSharpCodeActionOptions options,
#pragma warning restore IDE0060 // Remove unused parameter
            CancellationToken cancellationToken)
            => new(document, span, registerRefactoring, CodeActionOptions.DefaultProvider, cancellationToken);

        public static FixAllContext CreateFixAllContext(
            Document? document,
            TextSpan? diagnosticSpan,
            Project project,
            CodeFixProvider codeFixProvider,
            FixAllScope scope,
            string? codeActionEquivalenceKey,
            IEnumerable<string> diagnosticIds,
            FixAllContext.DiagnosticProvider fixAllDiagnosticProvider,
#pragma warning disable IDE0060 // Remove unused parameter
            Func<string, OmniSharpCodeActionOptions> optionsProvider,
#pragma warning restore IDE0060 // Remove unused parameter
            CancellationToken cancellationToken)
            => new(new FixAllState(
                    fixAllProvider: NoOpFixAllProvider.Instance,
                    diagnosticSpan,
                    document,
                    project,
                    codeFixProvider,
                    scope,
                    codeActionEquivalenceKey,
                    diagnosticIds,
                    fixAllDiagnosticProvider,
                    CodeActionOptions.DefaultProvider),
                  CodeAnalysisProgress.None, cancellationToken);
    }
}
