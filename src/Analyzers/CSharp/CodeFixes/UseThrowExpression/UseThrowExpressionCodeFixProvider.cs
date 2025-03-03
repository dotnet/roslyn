// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseThrowExpression;

[ExportCodeFixProvider(LanguageNames.CSharp,
    Name = PredefinedCodeFixProviderNames.UseThrowExpression), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed partial class UseThrowExpressionCodeFixProvider() : SyntaxEditorBasedCodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds
        => [IDEDiagnosticIds.UseThrowExpressionDiagnosticId];

    protected override bool IncludeDiagnosticDuringFixAll(Diagnostic diagnostic)
        => !diagnostic.Descriptor.ImmutableCustomTags().Contains(WellKnownDiagnosticTags.Unnecessary);

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, AnalyzersResources.Use_throw_expression, nameof(AnalyzersResources.Use_throw_expression));
        return Task.CompletedTask;
    }

    protected override Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor, CancellationToken cancellationToken)
    {
        var generator = editor.Generator;
        var root = editor.OriginalRoot;

        foreach (var diagnostic in diagnostics)
        {
            var ifStatement = root.FindNode(diagnostic.AdditionalLocations[0].SourceSpan);
            var throwStatementExpression = root.FindNode(diagnostic.AdditionalLocations[1].SourceSpan);
            var assignmentValue = root.FindNode(diagnostic.AdditionalLocations[2].SourceSpan);
            var assignmentExpressionStatement = root.FindNode(diagnostic.AdditionalLocations[3].SourceSpan);

            // First, remote the if-statement entirely.
            editor.RemoveNode(ifStatement);

            // Now, update the assignment value to go from 'a' to 'a ?? throw ...'.
            editor.ReplaceNode(assignmentValue,
                generator.CoalesceExpression(assignmentValue,
                generator.ThrowExpression(throwStatementExpression)));

            // Move any trailing trivia after the `throw new Exception(); // comment`

            if (throwStatementExpression.Parent is ThrowStatementSyntax throwStatement &&
                throwStatement.GetTrailingTrivia().Any(t => t.IsSingleOrMultiLineComment()))
            {
                if (assignmentExpressionStatement.GetTrailingTrivia().Any(t => t.IsSingleOrMultiLineComment()))
                {
                    // Assignment already has trailing trivia.  Move the comments above it instead.
                    editor.ReplaceNode(
                        assignmentExpressionStatement,
                        (current, _) => current.WithLeadingTrivia(current.GetLeadingTrivia().Concat(throwStatement.GetTrailingTrivia())));
                }
                else
                {
                    editor.ReplaceNode(
                        assignmentExpressionStatement,
                        (current, _) => current.WithTrailingTrivia(throwStatement.GetTrailingTrivia()));
                }
            }
        }

        return Task.CompletedTask;
    }
}
