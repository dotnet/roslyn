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
using Microsoft.CodeAnalysis.LanguageService;

namespace Microsoft.CodeAnalysis.SimplifyLinqExpression
{
    internal abstract class AbstractSimplifyLinqExpressionCodeFixProvider<TInvocationExpressionSyntax, TSimpleNameSyntax, TExpressionSyntax> : SyntaxEditorBasedCodeFixProvider
        where TExpressionSyntax : SyntaxNode
        where TInvocationExpressionSyntax : TExpressionSyntax
        where TSimpleNameSyntax : TExpressionSyntax
    {
        protected abstract ISyntaxFacts SyntaxFacts { get; }

        public sealed override ImmutableArray<string> FixableDiagnosticIds
           => ImmutableArray.Create(IDEDiagnosticIds.SimplifyLinqExpressionDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            RegisterCodeFix(context, AnalyzersResources.Simplify_LINQ_expression, nameof(AnalyzersResources.Simplify_LINQ_expression));
            return Task.CompletedTask;
        }

        protected override Task FixAllAsync(Document document,
                                            ImmutableArray<Diagnostic> diagnostics,
                                            SyntaxEditor editor,
                                            CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            var root = editor.OriginalRoot;
            var expressionsToReWrite = diagnostics.Select(d => GetInvocation(root, d)).OrderByDescending(i => i.SpanStart);
            foreach (var original in expressionsToReWrite)
            {
                editor.ReplaceNode(original, (current, generator) =>
                {
                    var invocation = (TInvocationExpressionSyntax)current;
                    var (expression, name, arguments) = FindNodes(invocation);
                    return generator.InvocationExpression(
                            generator.MemberAccessExpression(expression, name),
                            arguments);
                });
            }

            return Task.CompletedTask;

            static TInvocationExpressionSyntax GetInvocation(SyntaxNode root, Diagnostic diagnostic)
            {
                return (TInvocationExpressionSyntax)root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
            }

            (TExpressionSyntax Expression, TSimpleNameSyntax Name, SeparatedSyntaxList<SyntaxNode> Arguments) FindNodes(TInvocationExpressionSyntax current)
            {
                var memberAccess = SyntaxFacts.GetExpressionOfInvocationExpression(current);
                var name = (TSimpleNameSyntax)SyntaxFacts.GetNameOfMemberAccessExpression(memberAccess);
                var whereExpression = (TInvocationExpressionSyntax)SyntaxFacts.GetExpressionOfMemberAccessExpression(memberAccess)!;
                var arguments = SyntaxFacts.GetArgumentsOfInvocationExpression(whereExpression);
                var expression = (TExpressionSyntax)SyntaxFacts.GetExpressionOfMemberAccessExpression(SyntaxFacts.GetExpressionOfInvocationExpression(whereExpression))!;
                return (expression, name, arguments);
            }
        }
    }
}
