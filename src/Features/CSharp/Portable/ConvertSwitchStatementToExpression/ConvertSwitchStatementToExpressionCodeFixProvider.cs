// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
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
            var spans = ArrayBuilder<TextSpan>.GetInstance();
            foreach (var diagnostic in diagnostics)
            {
                var node = editor.OriginalRoot.FindNode(diagnostic.Location.SourceSpan);
                if (spans.Any((span, nodeSpan) => span.Contains(nodeSpan), node.Span))
                {
                    continue;
                }

                spans.Add(node.Span);
                editor.ReplaceNode(node, (@switch, _) => Rewriter.Rewrite(@switch).WithAdditionalAnnotations(Formatter.Annotation));

                var nextStatement = ((SwitchStatementSyntax)node).GetNextStatement();
                if (nextStatement.IsKind(SyntaxKind.ThrowStatement, SyntaxKind.ReturnStatement) &&
                    // e.g. not unreachable which means we had a non-exhastive switch
                    !nextStatement.ContainsDiagnostics)
                {
                    // Already morphed into the top-level switch expression.
                    editor.RemoveNode(nextStatement);
                }
            }

            spans.Free();
            return Task.CompletedTask;
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
