// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UseThrowExpression
{
    [ExportCodeFixProvider(LanguageNames.CSharp,
        Name = PredefinedCodeFixProviderNames.UseThrowExpression), Shared]
    internal class UseThrowExpressionCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public UseThrowExpressionCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.UseThrowExpressionDiagnosticId);

        protected override bool IncludeDiagnosticDuringFixAll(Diagnostic diagnostic)
            => !diagnostic.Descriptor.ImmutableCustomTags().Contains(WellKnownDiagnosticTags.Unnecessary);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            RegisterCodeFix(context, AnalyzersResources.Use_throw_expression, nameof(AnalyzersResources.Use_throw_expression));
            return Task.CompletedTask;
        }

        protected override Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            var generator = editor.Generator;
            var root = editor.OriginalRoot;

            foreach (var diagnostic in diagnostics)
            {
                var ifStatement = root.FindNode(diagnostic.AdditionalLocations[0].SourceSpan);
                var throwStatementExpression = root.FindNode(diagnostic.AdditionalLocations[1].SourceSpan);
                var assignmentValue = root.FindNode(diagnostic.AdditionalLocations[2].SourceSpan);
                var expressionStatement = assignmentValue.GetAncestor<ExpressionStatementSyntax>();
                var identifierName = expressionStatement.DescendantNodes().ElementAt(1);

                var block = ((IfStatementSyntax)ifStatement).Statement.ChildNodes().FirstOrDefault();
                var closeBrace = ((IfStatementSyntax)ifStatement).CloseParenToken;

                var triviaList = new SyntaxTriviaList()
                    .AddRange(closeBrace.GetAllTrailingTrivia().Where(t => !t.IsWhitespaceOrEndOfLine()))
                    .AddRange(block.GetLeadingTrivia().Where(t => !t.IsWhitespaceOrEndOfLine()))
                    .AddRange(block.GetTrailingTrivia().Where(t => !t.IsWhitespaceOrEndOfLine()));

                // First, remove the if-statement entirely.
                editor.RemoveNode(ifStatement);

                // Now, update the assignment value to go from 'a' to 'a ?? throw ...'.
                if (triviaList.IsEmpty())
                {
                    editor.ReplaceNode(assignmentValue,
                        generator.CoalesceExpression(assignmentValue,
                        generator.ThrowExpression(throwStatementExpression)));
                }
                else
                {
                    triviaList = triviaList.AddRange(block.GetTrailingTrivia().Where(t => t.IsEndOfLine()));

                    var newExpression = generator.ExpressionStatement(generator.AssignmentStatement(identifierName,
                        generator.CoalesceExpression(assignmentValue,
                        generator.ThrowExpression(throwStatementExpression)))).WithTrailingTrivia(triviaList).WithAppendedTrailingTrivia();

                    editor.ReplaceNode(expressionStatement, newExpression);
                }
            }

            return Task.CompletedTask;
        }
    }
}
