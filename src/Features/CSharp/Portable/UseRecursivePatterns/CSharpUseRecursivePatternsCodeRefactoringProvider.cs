// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.UseRecursivePatterns
{
    using static AnalyzedNode;
    using static SyntaxFactory;

    [ExportCodeRefactoringProvider(LanguageNames.CSharp,
         Name = nameof(CSharpUseRecursivePatternsRefactoringProvider)), Shared]
    internal sealed class CSharpUseRecursivePatternsRefactoringProvider : CodeRefactoringProvider
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]",
            Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpUseRecursivePatternsRefactoringProvider()
        {
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, textSpan, cancellationToken) = context;
            if (!textSpan.IsEmpty)
                return;

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var node = root.FindNode(textSpan);

            var replacementFunc = GetReplacementFunc(node, semanticModel, document, cancellationToken);
            if (replacementFunc is null)
                return;

            context.RegisterRefactoring(
                new MyCodeAction(CSharpAnalyzersResources.Use_pattern_matching, replacementFunc));
        }

        private static Func<CancellationToken, Task<Document>>? GetReplacementFunc(
            SyntaxNode node, SemanticModel semanticModel, Document document, CancellationToken cancellationToken)
        {
            return node switch
            {
                ExpressionSyntax n => VisitExpression(n),
                CasePatternSwitchLabelSyntax n => VisitCasePatternSwitchLabel(n),
                SwitchExpressionArmSyntax n => VisitSwitchExpressionArm(n),
                WhenClauseSyntax { Parent: CasePatternSwitchLabelSyntax n } => VisitCasePatternSwitchLabel(n),
                WhenClauseSyntax { Parent: SwitchExpressionArmSyntax n } => VisitSwitchExpressionArm(n),
                _ => null,
            };

            Func<CancellationToken, Task<Document>>? VisitSwitchExpressionArm(SwitchExpressionArmSyntax node)
            {
                var analyzedNode = AnalyzeNode(node, semanticModel, cancellationToken);
                if (analyzedNode is null)
                    return null;

                return cancellationToken => document.ReplaceNodeAsync(node, node
                    .WithPattern(analyzedNode
                        .AsPatternSyntax(out var expression)
                        .ConvertToSingleLine()
                        .WithTriviaFrom(node.Pattern))
                    .WithWhenClause(expression is null ? null : WhenClause(expression))
                    .WithAdditionalAnnotations(Formatter.Annotation), cancellationToken);
            }

            Func<CancellationToken, Task<Document>>? VisitCasePatternSwitchLabel(CasePatternSwitchLabelSyntax node)
            {
                var analyzedNode = AnalyzeNode(node, semanticModel, cancellationToken);
                if (analyzedNode is null)
                    return null;

                return cancellationToken => document.ReplaceNodeAsync(node, node
                    .WithPattern(analyzedNode
                        .AsPatternSyntax(out var expression)
                        .ConvertToSingleLine()
                        .WithTriviaFrom(node.Pattern))
                    .WithWhenClause(expression is null ? null : WhenClause(expression))
                    .WithAdditionalAnnotations(Formatter.Annotation), cancellationToken);
            }

            Func<CancellationToken, Task<Document>>? VisitExpression(ExpressionSyntax node)
            {
                var expression = GetTopmostExpression(node);
                if (expression is null)
                    return null;

                var analyzedNode = AnalyzeNode(expression, semanticModel, cancellationToken);
                if (analyzedNode is null)
                    return null;

                return cancellationToken => document.ReplaceNodeAsync(expression, analyzedNode
                    .AsExpressionSyntax()
                    .WrapPropertyPatternClauses()
                    .WithTriviaFrom(expression)
                    .WithAdditionalAnnotations(Formatter.Annotation), cancellationToken);
            }
        }

        private static ExpressionSyntax? GetTopmostExpression(ExpressionSyntax node)
        {
            for (SyntaxNode? current = node; current != null; current = current.GetParent(ascendOutOfTrivia: true))
            {
                if (!(current is ExpressionSyntax expr))
                    break;

                if (expr.IsTopmostExpression())
                    return expr;
            }

            return null;
        }

        private static AnalyzedNode? AnalyzeNode(SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (node.ContainsDiagnostics)
                return null;

            var operation = semanticModel.GetOperation(node, cancellationToken);
            if (operation is null)
                return null;

            var analyzedNode = AnalyzedNodeFactory.Visit(operation);
            if (analyzedNode is null)
                return null;

            var expandedNode = AnalyzedNodeExpander.Expand(analyzedNode);
            var reducedNode = AnalyzedNodeReducer.Reduce(expandedNode);

            if (reducedNode.Equals(analyzedNode))
                return null;

            if (reducedNode is OperationEvaluation)
                return null;

            if (HasIllegalPatternVariables(reducedNode))
                return null;

            return reducedNode;
        }

        private static bool HasIllegalPatternVariables(AnalyzedNode node)
        {
            return node is Pair p
                ? HasIllegalPatternVariables(p.Pattern, isTopLevel: true)
                : HasIllegalPatternVariables(node, isTopLevel: false);

            static bool HasIllegalPatternVariables(AnalyzedNode node, bool permitDesignations = true, bool isTopLevel = false)
            {
                return node switch
                {
                    Not p => HasIllegalPatternVariables(p.Operand, permitDesignations: isTopLevel),
                    OrSequence p => p.Nodes.Any(item => HasIllegalPatternVariables(item, permitDesignations: false)),
                    AndSequence p => p.Nodes.Any(item => HasIllegalPatternVariables(item, permitDesignations)),
                    Pair pair => HasIllegalPatternVariables(pair.Pattern, permitDesignations),
                    Variable _ when !permitDesignations => true,
                    _ => false
                };
            }
        }

        private sealed class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}
