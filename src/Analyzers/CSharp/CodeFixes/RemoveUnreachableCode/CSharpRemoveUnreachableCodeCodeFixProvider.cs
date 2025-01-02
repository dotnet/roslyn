// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnreachableCode;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.RemoveUnreachableCode), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpRemoveUnreachableCodeCodeFixProvider() : SyntaxEditorBasedCodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        [IDEDiagnosticIds.RemoveUnreachableCodeDiagnosticId];

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics[0];

        // Only the first reported unreachable line will have a squiggle.  On that line, make the code action normal
        // priority as the user is likely bringing up the lightbulb to fix the squiggle.  On all the other lines
        // make the code action low priority as it's definitely helpful, but shouldn't interfere with anything else
        // the user is doing.
        var priority = IsSubsequentSection(diagnostic)
            ? CodeActionPriority.Low
            : CodeActionPriority.Default;

        RegisterCodeFix(context, CSharpCodeFixesResources.Remove_unreachable_code, nameof(CSharpCodeFixesResources.Remove_unreachable_code), priority);

        return Task.CompletedTask;
    }

    protected override bool IncludeDiagnosticDuringFixAll(Diagnostic diagnostic)
        => !IsSubsequentSection(diagnostic);

    private static bool IsSubsequentSection(Diagnostic diagnostic)
        => diagnostic.Properties.ContainsKey(CSharpRemoveUnreachableCodeDiagnosticAnalyzer.IsSubsequentSection);

    protected override Task FixAllAsync(
        Document document,
        ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor,
        CancellationToken cancellationToken)
    {
        foreach (var diagnostic in diagnostics)
        {
            var firstUnreachableStatementLocation = diagnostic.AdditionalLocations[0];
            var firstUnreachableStatement = CSharpRemoveUnreachableCodeDiagnosticAnalyzer.TryGetContainingStatement(
                firstUnreachableStatementLocation.FindNode(getInnermostNodeForTie: true, cancellationToken));

            if (firstUnreachableStatement is null)
                continue;

            RemoveStatement(editor, firstUnreachableStatement);

            var sections = RemoveUnreachableCodeHelpers.GetSubsequentUnreachableSections(firstUnreachableStatement);
            foreach (var section in sections)
            {
                foreach (var statement in section)
                {
                    RemoveStatement(editor, statement);
                }
            }
        }

        return Task.CompletedTask;

        // Local function
        static void RemoveStatement(SyntaxEditor editor, SyntaxNode statement)
        {
            if (statement.Parent?.Kind()
                    is not SyntaxKind.Block
                    and not SyntaxKind.SwitchSection
                    and not SyntaxKind.GlobalStatement)
            {
                editor.ReplaceNode(statement, SyntaxFactory.Block());
            }
            else
            {
                editor.RemoveNode(statement, SyntaxRemoveOptions.KeepUnbalancedDirectives);
            }
        }
    }
}
