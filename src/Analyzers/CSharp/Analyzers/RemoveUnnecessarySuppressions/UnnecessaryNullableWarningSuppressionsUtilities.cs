// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnnecessarySuppressions;

internal static class UnnecessaryNullableWarningSuppressionsUtilities
{
    private static bool ContainsErrorOrWarning(IEnumerable<Diagnostic> diagnostics)
        => diagnostics.Any(d => d.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning);

    private static bool ShouldAnalyzeNode(SemanticModel semanticModel, PostfixUnaryExpressionSyntax node, CancellationToken cancellationToken)
    {
        if (node is not PostfixUnaryExpressionSyntax(SyntaxKind.SuppressNullableWarningExpression) postfixUnary)
            return false;

        if (ContainsErrorOrWarning(postfixUnary.GetDiagnostics()))
            return false;

        var semanticDiagnostics = semanticModel.GetDiagnostics(postfixUnary.Span, cancellationToken);
        if (ContainsErrorOrWarning(semanticDiagnostics))
            return false;

        return true;
    }

    public static void AddUnnecessaryNodes(
        SemanticModel semanticModel,
        ArrayBuilder<PostfixUnaryExpressionSyntax> result,
        CancellationToken cancellationToken)
    {
        using var _1 = ArrayBuilder<PostfixUnaryExpressionSyntax>.GetInstance(out var nodesToCheck);

        var originalTree = semanticModel.SyntaxTree;
        var originalRoot = originalTree.GetRoot(cancellationToken);

        foreach (var existingNode in originalRoot.DescendantNodes())
        {
            if (existingNode is PostfixUnaryExpressionSyntax postfixUnary &&
                ShouldAnalyzeNode(semanticModel, postfixUnary, cancellationToken))
            {
                nodesToCheck.Add(postfixUnary);
            }
        }

        if (nodesToCheck.IsEmpty)
            return;

        using var _2 = PooledDictionary<PostfixUnaryExpressionSyntax, SyntaxAnnotation>.GetInstance(out var nodeToAnnotation);
        foreach (var node in nodesToCheck)
            nodeToAnnotation[node] = new SyntaxAnnotation();

        ForkSemanticModelAndCheckNodes(semanticModel, nodesToCheck, nodeToAnnotation, result, cancellationToken);
    }

    private static void ForkSemanticModelAndCheckNodes(
        SemanticModel semanticModel,
        ArrayBuilder<PostfixUnaryExpressionSyntax> nodesToCheck,
        PooledDictionary<PostfixUnaryExpressionSyntax, SyntaxAnnotation> nodeToAnnotation,
        ArrayBuilder<PostfixUnaryExpressionSyntax> result,
        CancellationToken cancellationToken)
    {
        var originalTree = semanticModel.SyntaxTree;
        var originalRoot = originalTree.GetRoot(cancellationToken);

        var updatedTree = originalTree.WithRootAndOptions(
            originalRoot.ReplaceNodes(
                nodesToCheck,
                (original, current) => current.Operand.WithAdditionalAnnotations(nodeToAnnotation[original])),
            originalTree.Options);
        var updateRoot = updatedTree.GetRoot(cancellationToken);
        var updatedCompilation = semanticModel.Compilation
            .ReplaceSyntaxTree(originalTree, updatedTree)
            .WithOptions(semanticModel.Compilation.Options.WithSpecificDiagnosticOptions([]));
        var updatedSemanticModel = updatedCompilation.GetSemanticModel(updatedTree);

        foreach (var (node, annotation) in nodeToAnnotation)
        {
            var updatedNode = updateRoot.GetAnnotatedNodes(annotation).Single();
            var updatedDiagnostics = updatedSemanticModel.GetDiagnostics(GetSpanToCheck(updatedNode), cancellationToken);
            if (!ContainsErrorOrWarning(updatedDiagnostics))
                result.Add(node);
        }
    }

    private static TextSpan GetSpanToCheck(SyntaxNode updatedNode)
    {
        var firstAncestor = updatedNode.Ancestors().FirstOrDefault(
            n => n is StatementSyntax or ArrowExpressionClauseSyntax or BaseMethodDeclarationSyntax or BasePropertyDeclarationSyntax);

        if (firstAncestor is StatementSyntax containingStatement)
        {
            if (containingStatement.Parent is GlobalStatementSyntax globalStatement)
            {
                var compilationUnit = (CompilationUnitSyntax)globalStatement.GetRequiredParent();
                return TextSpan.FromBounds(globalStatement.SpanStart, compilationUnit.Members.OfType<GlobalStatementSyntax>().Last().Span.End);
            }

            if (containingStatement.Parent is BlockSyntax block)
                return TextSpan.FromBounds(containingStatement.SpanStart, block.Statements.Last().Span.End);
            else if (containingStatement.Parent is SwitchSectionSyntax switchSection)
                return TextSpan.FromBounds(containingStatement.SpanStart, switchSection.Statements.Last().Span.End);
            else
                return containingStatement.Span;
        }

        if (firstAncestor is not null)
            return firstAncestor.Span;

        return updatedNode.Span;
    }
}
