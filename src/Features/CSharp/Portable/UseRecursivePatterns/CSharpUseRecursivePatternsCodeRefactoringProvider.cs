// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseRecursivePatterns
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(CSharpUseRecursivePatternsCodeRefactoringProvider)), Shared]
    internal sealed partial class CSharpUseRecursivePatternsCodeRefactoringProvider : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var span = context.Span;
            if (span.Length > 0)
            {
                return;
            }

            var document = context.Document;
            var cancellationToken = context.CancellationToken;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var node = GetOutermostExpression(root.FindToken(span.Start));
            if (node is null)
            {
                return;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            // First, we make a tree of comparisons and pattern-matches from source.
            // For example, if we have:
            //
            //      e1 is T v && v.Property == true
            //
            // We produce a tree of the following form:
            //
            //      Conjunction(
            //          PatternMatch(e1,
            //              Conjunction(TypePattern(T), VarPattern(v))),
            //          PatternMatch(v.Property, ConstantPattern(true)))
            //
            var analyzedNode = Analyzer.Analyze(node, semanticModel);
            if (analyzedNode is null)
            {
                return;
            }

            // Then, we try to combine common expressions to rewrite matches as a recursive-pattern.
            // For example, the above tree would be reduced to:
            //
            //      PatternMatch(e1,
            //          Conjunction(
            //              Conjunction(TypePattern(T), VarPattern(v)),
            //              PatternMatch(Property, ConstantPattern(true)))
            //
            // Which will be rewritten to:
            //
            //      e1 is T {Property: true} v
            //
            var reducedNode = Reducer.Reduce(analyzedNode, semanticModel);
            if (reducedNode is null)
            {
                return;
            }

            context.RegisterRefactoring(new MyCodeAction(
                c => document.ReplaceNodeAsync(node, Rewrite(reducedNode, node)
                .WithAdditionalAnnotations(Formatter.Annotation), c)));
        }

        private static SyntaxNode Rewrite(AnalyzedNode reducedNode, SyntaxNode sourceNode)
        {
            switch (sourceNode.Kind())
            {
                case SyntaxKind.CasePatternSwitchLabel:
                    // In case we had a when-clause, we make a conjunction
                    // of the pattern on the left and the condition on the right.
                    // For example, if we have:
                    //
                    //      case T v when v.Property == 42
                    //
                    // We would have a tree of the following form:
                    //
                    //      Conjunction(
                    //          Conjunction(TypePattern(T), VarPattern(v))
                    //          PatternMatch(v.Property, ConstantPattern(42))
                    //
                    // which will be reduced to:
                    //
                    //      Conjunction(
                    //          Conjunction(TypePattern(T), VarPattern(v))
                    //          PatternMatch(Property, ConstantPattern(42))
                    //
                    // We'll try to rewrite this as a CasePatternSwitchLabel which leaves us with:
                    //
                    //      case T {Property: 42} v:
                    //
                    // Note that we expect this to be a conjunction node because
                    // if this is a non-trivial reduction of a pattern and expression
                    // we will must definitely end up with a conjunction of nodes.
                    return ((Conjunction)reducedNode).AsCasePatternSwitchLabelSyntax();

                case SyntaxKind.SwitchExpressionArm:
                    return ((Conjunction)reducedNode).AsSwitchExpressionArmSyntax(
                        // Pass the switch arm expression as-is since it's a part of the node.
                        ((SwitchExpressionArmSyntax)sourceNode).Expression);

                case SyntaxKind.LogicalAndExpression:
                case SyntaxKind.EqualsExpression:
                case SyntaxKind.IsExpression:
                case SyntaxKind.IsPatternExpression:
                    // Otherwise, we do normal rewriting as an expression.
                    return reducedNode.AsExpressionSyntax();

                case var value:
                    throw ExceptionUtilities.UnexpectedValue(value);
            }
        }

        private static SyntaxNode GetOutermostExpression(SyntaxToken token)
        {
            var node = token.Parent;
            if (token.IsKind(SyntaxKind.WhenKeyword))
            {
                if (node.IsParentKind(SyntaxKind.CasePatternSwitchLabel, SyntaxKind.SwitchExpressionArm))
                {
                    return node.Parent;
                }

                return null;
            }

            SyntaxNode previous = null;
            while (node != null)
            {
                switch (node.Kind())
                {
                    case SyntaxKind.ParenthesizedExpression:
                        node = node.Parent;
                        continue;
                    case SyntaxKind.LogicalAndExpression:
                    case SyntaxKind.EqualsExpression:
                    case SyntaxKind.IsExpression:
                    case SyntaxKind.IsPatternExpression:
                        // Walk up the tree to catch all applicable expressions.
                        // If we fail, we'll return the last node we found.
                        previous = node;
                        node = node.Parent;
                        continue;
                }

                break;
            }

            return previous;
        }

        private sealed class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base("Use recursive patterns", createChangedDocument)
            {
            }
        }
    }
}
