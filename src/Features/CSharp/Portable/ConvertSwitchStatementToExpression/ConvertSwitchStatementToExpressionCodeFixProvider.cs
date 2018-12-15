// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertSwitchStatementToExpression
{
    using static SyntaxFactory;

    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal sealed partial class ConvertSwitchStatementToExpressionCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.ConvertSwitchStatementToExpressionDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(
                new MyCodeAction(c => FixAsync(context.Document, context.Diagnostics.First(), c)),
                context.Diagnostics);
            return Task.CompletedTask;
        }

        protected override Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var root = editor.OriginalRoot;
            foreach (var diagnostic in diagnostics)
            {
                var node = root.FindNode(diagnostic.Location.SourceSpan);
                editor.ReplaceNode(node, (currentStatement, _) =>
                {
                    var switchStatement = (SwitchStatementSyntax)currentStatement;
                    var switchExpression = Rewriter.Rewrite(switchStatement, out var assignmentTargetsOpt);
                    var finalStatement = GetFinalStatement(switchExpression, assignmentTargetsOpt);
                    return finalStatement.WithAdditionalAnnotations(Formatter.Annotation);
                });
            }

            return Task.CompletedTask;
        }

        private StatementSyntax GetFinalStatement(ExpressionSyntax switchExpression, List<ExpressionSyntax> assignmentTargetsOpt)
        {
            if (assignmentTargetsOpt is null)
            {
                return ReturnStatement(switchExpression);
            }

            Debug.Assert(assignmentTargetsOpt.Count >= 1);
            return ExpressionStatement(
                AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                    left: assignmentTargetsOpt.Count == 1
                        ? assignmentTargetsOpt[0]
                        : TupleExpression(SeparatedList(assignmentTargetsOpt.Select(Argument))),
                    right: switchExpression));
        }

        private sealed class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument) :
                base(CSharpFeaturesResources.Convert_switch_statement_to_expression, createChangedDocument)
            {
            }
        }
    }
}
