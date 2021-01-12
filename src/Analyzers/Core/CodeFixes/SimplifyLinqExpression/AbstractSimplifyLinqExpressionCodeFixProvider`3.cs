// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.SimplifyLinqExpression
{
    internal abstract class AbstractSimplifyLinqExpressionCodeFixProvider<TInvocationExpressionSyntax, TSimpleNameSyntax, TExpressionSyntax> : SyntaxEditorBasedCodeFixProvider
        where TInvocationExpressionSyntax : SyntaxNode
        where TSimpleNameSyntax : SyntaxNode
        where TExpressionSyntax : SyntaxNode
    {
        private const string SimplyfyLinqAnnotationKind = "linqTracking";

        protected abstract TSimpleNameSyntax GetName(TInvocationExpressionSyntax invocationExpression);
        protected abstract TExpressionSyntax GetExpression(TExpressionSyntax invocationExpression);
        protected abstract SyntaxNode[] GetArguments(SyntaxNode invocationExpression);

        public sealed override ImmutableArray<string> FixableDiagnosticIds
           => ImmutableArray.Create(IDEDiagnosticIds.SimplifyLinqExpressionDiagnosticId);

        internal override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeQuality;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(new MyCodeAction(
                c => FixAsync(context.Document, context.Diagnostics.First(), c)),
                context.Diagnostics);
            return Task.CompletedTask;
        }

        protected override Task FixAllAsync(Document document,
                                            ImmutableArray<Diagnostic> diagnostics,
                                            SyntaxEditor editor,
                                            CancellationToken cancellationToken)
        {
            var root = editor.OriginalRoot;

            // Track all the arguments into linq methods as they could be lambdas and the contents of their bodies may change
            var invocationsAndTheirArguments = diagnostics.Select(d => (Invocation: GetInvocation(root, d), Arguments: GetArguments(root, d)));
            var argumentLookup = invocationsAndTheirArguments
                .SelectMany(x => GetSpans(x.Arguments, x.Invocation))
                .ToImmutableDictionary(x => x.ArgumentSpanStart, x => x.InvocationSpanStart);
            var annotatedRoot = root.ReplaceNodes(
                invocationsAndTheirArguments.SelectMany(x => x.Arguments),
                (original, current) =>
                {
                    // add the original span that the diagnostic was about in the data section of the annotation to use as a key later
                    var annotation = new SyntaxAnnotation(SimplyfyLinqAnnotationKind, argumentLookup[original.SpanStart].ToString());
                    return current.WithAdditionalAnnotations(annotation);
                });

            // Find the related nodes in the annotated tree
            var newNodes = diagnostics.Select(d => GetNodes(d, annotatedRoot));
            var relatedNodesByInvocationSpanStart = newNodes.ToImmutableDictionary(n => n.Invocation.SpanStart, n => (n.Expression, n.Name, n.Arguments));

            // Rewrite the expressions
            var expressionsToReWrite = newNodes.Select(x => x.Invocation).OrderByDescending(x => x.SpanStart);
            var generator = editor.Generator;
            var updatedRoot = annotatedRoot.ReplaceNodes(
                expressionsToReWrite,
                (original, current) =>
                {
                    var (expression, name, arguments) = relatedNodesByInvocationSpanStart[original.SpanStart];
                    if (original != current)
                    {
                        // Retireve arguments to the invocation by looking at the annotations and matching them via the span start
                        arguments = current.GetAnnotatedNodes(SimplyfyLinqAnnotationKind)
                            .Where(x => x.GetAnnotations(SimplyfyLinqAnnotationKind).Single().Data == original.SpanStart.ToString())
                            .ToArray();
                    }

                    return generator.InvocationExpression(
                            generator.MemberAccessExpression(expression, name),
                            arguments);
                });

            editor.ReplaceNode(root, updatedRoot);
            return Task.CompletedTask;

            IEnumerable<(int ArgumentSpanStart, int InvocationSpanStart)> GetSpans(SyntaxNode[] arguments, SyntaxNode invocationExpression)
            {
                foreach (var arg in arguments)
                {
                    yield return (arg.SpanStart, invocationExpression.SpanStart);
                }
            }

            static TInvocationExpressionSyntax GetInvocation(SyntaxNode root, Diagnostic diagnostic)
            {
                return (TInvocationExpressionSyntax)root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
            }

            SyntaxNode[] GetArguments(SyntaxNode root, Diagnostic diagnostic)
            {
                return this.GetArguments((TExpressionSyntax)root.FindNode(diagnostic.AdditionalLocations[1].SourceSpan, getInnermostNodeForTie: true));
            }

            (TInvocationExpressionSyntax Invocation, TExpressionSyntax Expression, TSimpleNameSyntax Name, SyntaxNode[] Arguments) GetNodes(Diagnostic diagnostic, SyntaxNode root)
            {
                var invocation = GetInvocation(root, diagnostic);
                var name = GetName(invocation);
                var expression = GetExpression((TExpressionSyntax)root.FindNode(diagnostic.AdditionalLocations[0].SourceSpan, getInnermostNodeForTie: true));
                var arguments = GetArguments(root, diagnostic);
                return (invocation, expression, name, arguments);
            }
        }

        private class MyCodeAction : CustomCodeActions.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(AnalyzersResources.Simplify_Linq_expression, createChangedDocument, AnalyzersResources.Simplify_Linq_expression)
            {
            }
        }
    }
}
