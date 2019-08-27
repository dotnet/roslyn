// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertSwitchStatementToExpression
{
    using Constants = ConvertSwitchStatementToExpressionConstants;

    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal sealed partial class ConvertSwitchStatementToExpressionCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.ConvertSwitchStatementToExpressionDiagnosticId);

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeStyle;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(
                new MyCodeAction(c => FixAsync(context.Document, context.Diagnostics.First(), c)),
                context.Diagnostics);
            return Task.CompletedTask;
        }

        protected override async Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            using var spansDisposer = ArrayBuilder<TextSpan>.GetInstance(diagnostics.Length, out var spans);
            foreach (var diagnostic in diagnostics)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var span = diagnostic.AdditionalLocations[0].SourceSpan;
                if (spans.Any((s, nodeSpan) => s.Contains(nodeSpan), span))
                {
                    // Skip nested switch expressions in case of a fix-all operation.
                    continue;
                }

                spans.Add(span);

                var properties = diagnostic.Properties;
                var nodeToGenerate = (SyntaxKind)int.Parse(properties[Constants.NodeToGenerateKey]);
                var shouldRemoveNextStatement = bool.Parse(properties[Constants.ShouldRemoveNextStatementKey]);

                var switchStatement = (SwitchStatementSyntax)editor.OriginalRoot.FindNode(span);
                editor.ReplaceNode(switchStatement,
                    Rewriter.Rewrite(switchStatement, semanticModel, editor,
                        nodeToGenerate, shouldMoveNextStatementToSwitchExpression: shouldRemoveNextStatement)
                    .WithAdditionalAnnotations(Formatter.Annotation));

                if (shouldRemoveNextStatement)
                {
                    // Already morphed into the top-level switch expression.
                    var nextStatement = switchStatement.GetNextStatement();
                    Debug.Assert(nextStatement.IsKind(SyntaxKind.ThrowStatement, SyntaxKind.ReturnStatement));
                    editor.RemoveNode(nextStatement);
                }
            }
        }

        private sealed class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(CSharpFeaturesResources.Convert_switch_statement_to_expression, createChangedDocument)
            {
            }
        }
    }
}
