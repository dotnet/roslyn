// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.SimplifyLinqExpression
{
    internal abstract class AbstractSimplifyLinqExpressionCodeFixProvider<TInvocationExpressionSyntax, TSimpleNameSyntax, TExpressionSyntax, TArgumentListSyntax> : SyntaxEditorBasedCodeFixProvider
        where TExpressionSyntax : SyntaxNode
        where TArgumentListSyntax : SyntaxNode
        where TInvocationExpressionSyntax : TExpressionSyntax
        where TSimpleNameSyntax : TExpressionSyntax
    {
        private const string SimplyfyLinqAnnotationKind = "linqTracking";

        protected abstract TSimpleNameSyntax GetName(TInvocationExpressionSyntax invocationExpression);
        protected abstract SeparatedSyntaxList<SyntaxNode> GetArguments(TArgumentListSyntax invocationExpression);
        protected abstract ISyntaxFacts SyntaxFacts { get; }

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
            var invocationsAndTheirArguments = diagnostics.Select(d => (Invocation: GetInvocation(root, d), ArgumentList: GetArgumentList(root, d)));
            var argumentLookup = invocationsAndTheirArguments
                .Select(x => (ArgumentSpanStart: x.ArgumentList.SpanStart, InvocationSpanStart: x.Invocation.SpanStart))
                .ToImmutableDictionary(x => x.ArgumentSpanStart, x => x.InvocationSpanStart);
            var annotatedRoot = root.ReplaceNodes(
                invocationsAndTheirArguments.Select(x => x.ArgumentList),
                (original, current) =>
                {
                    // add the original span that the diagnostic was about in the data section of the annotation to use as a key later
                    var annotation = new SyntaxAnnotation(SimplyfyLinqAnnotationKind, argumentLookup[original.SpanStart].ToString());
                    return current.WithAdditionalAnnotations(annotation);
                });

            // Find the related nodes in the annotated tree
            var newNodes = diagnostics.Select(d => GetNodes(d, annotatedRoot));
            var relatedNodesByInvocationSpanStart = newNodes.ToImmutableDictionary(n => n.Invocation.SpanStart, n => (n.Expression, n.Name, n.ArgumentList));

            // Rewrite the expressions
            var expressionsToReWrite = newNodes.Select(x => x.Invocation).OrderByDescending(x => x.SpanStart);
            var generator = editor.Generator;
            var updatedRoot = annotatedRoot.ReplaceNodes(
                expressionsToReWrite,
                (original, current) =>
                {
                    var (expression, name, argumentList) = relatedNodesByInvocationSpanStart[original.SpanStart];
                    if (original != current)
                    {
                        // Retireve arguments to the invocation by looking at the annotations and matching them via the span start
                        argumentList = (TArgumentListSyntax)current.GetAnnotatedNodes(SimplyfyLinqAnnotationKind)
                            .Single(x => x.GetAnnotations(SimplyfyLinqAnnotationKind).Single().Data == original.SpanStart.ToString());
                    }

                    return generator.InvocationExpression(
                            generator.MemberAccessExpression(expression, name),
                            GetArguments(argumentList)).WithTriviaFrom(current);
                });

            editor.ReplaceNode(root, updatedRoot);
            return Task.CompletedTask;

            static TInvocationExpressionSyntax GetInvocation(SyntaxNode root, Diagnostic diagnostic)
            {
                return (TInvocationExpressionSyntax)root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
            }

            TArgumentListSyntax GetArgumentList(SyntaxNode root, Diagnostic diagnostic)
            {
                return (TArgumentListSyntax)root.FindNode(diagnostic.AdditionalLocations[1].SourceSpan, getInnermostNodeForTie: true);
            }

            (TInvocationExpressionSyntax Invocation, TExpressionSyntax Expression, TSimpleNameSyntax Name, TArgumentListSyntax ArgumentList) GetNodes(Diagnostic diagnostic, SyntaxNode root)
            {
                var invocation = GetInvocation(root, diagnostic);
                var name = GetName(invocation);
                var invocationExpression = (TInvocationExpressionSyntax)root.FindNode(diagnostic.AdditionalLocations[0].SourceSpan, getInnermostNodeForTie: true);
                var memberAccessExpression = SyntaxFacts.GetExpressionOfInvocationExpression(invocationExpression);
                var expression = (TExpressionSyntax)SyntaxFacts.GetExpressionOfMemberAccessExpression(memberAccessExpression)!;
                var arguments = GetArgumentList(root, diagnostic);
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
