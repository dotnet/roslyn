// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UseThrowExpression
{
    [ExportCodeFixProvider(LanguageNames.CSharp,
        Name = PredefinedCodeFixProviderNames.UseThrowExpression), Shared]
    internal partial class UseThrowExpressionCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        public UseThrowExpressionCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.UseThrowExpressionDiagnosticId);

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeStyle;

        protected override bool IncludeDiagnosticDuringFixAll(Diagnostic diagnostic)
            => !diagnostic.Descriptor.CustomTags.Contains(WellKnownDiagnosticTags.Unnecessary);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(
                new MyCodeAction(c => FixAsync(context.Document, diagnostic, c)),
                diagnostic);

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
                var ifStatement = (IfStatementSyntax)root.FindNode(diagnostic.AdditionalLocations[0].SourceSpan);
                var throwStatementExpression = root.FindNode(diagnostic.AdditionalLocations[1].SourceSpan);
                var assignmentValue = root.FindNode(diagnostic.AdditionalLocations[2].SourceSpan);
                var blockStatement = ifStatement.ChildNodes().OfType<BlockSyntax>().First();
                var allTrivia = new List<SyntaxTrivia>();

                allTrivia.AddRange(ifStatement.CloseParenToken.TrailingTrivia);
                allTrivia.AddRange(blockStatement.OpenBraceToken.TrailingTrivia);
                allTrivia.AddRange(throwStatementExpression.GetTrailingTrivia());

                var triviaToAdd = new List<SyntaxTrivia>(new[] { SyntaxFactory.ElasticSpace });
                allTrivia.ForEach(x =>
                {
                    if (x.IsKind(SyntaxKind.SingleLineCommentTrivia) || x.IsKind(SyntaxKind.MultiLineCommentTrivia))
                    {
                        triviaToAdd.Add(x);
                        triviaToAdd.Add(SyntaxFactory.ElasticCarriageReturnLineFeed);
                    }
                });

                // First, remove the if-statement entirely.
                editor.RemoveNode(ifStatement);

                var newNode = generator.CoalesceExpression(assignmentValue,
                    generator.ThrowExpression(throwStatementExpression));

                // Now, update the assignment value to go from 'a' to 'a ?? throw ...'
                // Copying all comment trivia from the old statements.
                editor.ReplaceNode(assignmentValue, newNode.WithTrailingTrivia(triviaToAdd).WithTrailingTrivia());
            }

            return Task.CompletedTask;
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(
                Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(FeaturesResources.Use_throw_expression, createChangedDocument)
            {
            }
        }
    }
}
