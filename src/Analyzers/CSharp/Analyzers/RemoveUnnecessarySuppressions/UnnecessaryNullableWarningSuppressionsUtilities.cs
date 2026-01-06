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
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnnecessarySuppressions;

internal static class UnnecessaryNullableWarningSuppressionsUtilities
{
    private static bool ContainsErrorOrWarning(IEnumerable<Diagnostic> diagnostics)
        => diagnostics.Any(d => d.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning);

    private static bool ShouldAnalyzeNode(SemanticModel semanticModel, PostfixUnaryExpressionSyntax node, CancellationToken cancellationToken)
    {
        if (node is not PostfixUnaryExpressionSyntax(SyntaxKind.SuppressNullableWarningExpression) postfixUnary)
            return false;

        var spanToCheck = GetSpanToCheck(postfixUnary);
        if (spanToCheck is null)
            return false;

        // If there are any syntax or semantic diagnostics already in this node, then ignore it.  We can't make a good
        // judgement on how necessary the suppression is if there are other problems.
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

        using var _1 = ArrayBuilder<PostfixUnaryExpressionSyntax>.GetInstance(out var inGlobalStatements);
        using var _2 = ArrayBuilder<(PostfixUnaryExpressionSyntax suppression, SyntaxNode rewritten)>.GetInstance(out var inFieldsOrProperties);
        using var _3 = ArrayBuilder<(PostfixUnaryExpressionSyntax suppression, SyntaxNode rewritten)>.GetInstance(out var remainder);

        foreach (var (suppression, annotation) in nodeToAnnotation)
        {
            var rewritten = updateRoot.GetAnnotatedNodes(annotation).Single();
            var globalStatement = rewritten.Ancestors().OfType<GlobalStatementSyntax>().FirstOrDefault();

            if (globalStatement is not null)
            {
                inGlobalStatements.Add(suppression);
            }
            else
            {
                // Otherwise, find our containing code-containing member (attributes, accessors, fields, methods, properties,
                // anonymous methods), and check that entire member.  This means we only offer to remove the suppression if all
                // the suppressions in the member are unnecessary.  We need this granularity as doing things on a
                // per-suppression is just far too slow.
                //
                // We also check at this level because suppressions can have effects far outside of the containing statement (or
                // even things like the containing block.  For example: a suppression inside a block like `a = b!` can affect
                // the nullability of a variable which may be referenced far outside of the block.  So we really need to check
                // the entire code region that the suppression is in to make an accurate determination.
                var ancestor = rewritten.Ancestors().FirstOrDefault(
                    n => n is AttributeSyntax
                           or AccessorDeclarationSyntax
                           or AnonymousMethodExpressionSyntax
                           or BaseFieldDeclarationSyntax
                           or BaseMethodDeclarationSyntax
                           or BasePropertyDeclarationSyntax);

                if (ancestor is BaseFieldDeclarationSyntax or BasePropertyDeclarationSyntax)
                {
                    inFieldsOrProperties.Add((suppression, ancestor));
                }
                else if (ancestor != null)
                {
                    remainder.Add((suppression, ancestor));
                }
            }
        }

        updatedNodes.AddRange(nodeToAnnotation.Select(kvp => (kvp.Key, updateRoot.GetAnnotatedNodes(kvp.Value).Single())));

        var containingTypes = nodeToAnnotation.Select(kvp => updateRoot.GetAnnotatedNodes(kvp.Value).Single().FirstAncestorOrSelf<TypeDeclarationSyntax>()).WhereNotNull().Distinct();
        foreach (var type in containingTypes)
        {
            var diagnostics2 = updatedSemanticModel.GetDiagnostics(type.Span, cancellationToken);
            Console.WriteLine(diagnostics2);
        }

        CheckGlobalStatements();
        CheckFieldsAndProperties();
        CheckRemainder();

        // Group nodes by the span we need to check for errors/warnings. That way we only need to get the diagnostics
        // once per span instead of once per node.
        foreach (var group in nodeToAnnotation.GroupBy(tuple => GetSpanToCheck(updateRoot.GetAnnotatedNodes(tuple.Value).Single())))
        {
            var groupSpan = group.Key;
            if (groupSpan is null)
                continue;

            var updatedDiagnostics = updatedSemanticModel.GetDiagnostics(groupSpan, cancellationToken);
            if (ContainsErrorOrWarning(updatedDiagnostics))
                continue;

            // If there were no errors in that span after removing all the suppressions, then we can offer all of these
            // nodes up for fixing.
            foreach (var (suppressionNode, _) in group)
                result.Add(suppressionNode);
        }

        void CheckGlobalStatements()
        {
            if (inGlobalStatements.Count == 0)
                return;

            var compilationUnit = (CompilationUnitSyntax)updatedSemanticModel.SyntaxTree.GetRoot(cancellationToken);
            var globalStatements = compilationUnit.Members.OfType<GlobalStatementSyntax>();
            var span = TextSpan.FromBounds(globalStatements.First().SpanStart, globalStatements.Last().Span.End);

            var updatedDiagnostics = updatedSemanticModel.GetDiagnostics(span, cancellationToken);
            if (ContainsErrorOrWarning(updatedDiagnostics))
                return;

            // If there were no errors in that span after removing all the suppressions, then we can offer all of these
            // nodes up for fixing.
            result.AddRange(inGlobalStatements);
        }

        void CheckFieldsAndProperties()
        {

        }
    }

    private static TextSpan? GetSpanToCheck(SyntaxNode updatedNode)
    {
        // If we're in a global statement, check all global statements from the start of this one to the end of the last one.
        var globalStatement = updatedNode.Ancestors().OfType<GlobalStatementSyntax>().FirstOrDefault();
        if (globalStatement is not null)
        {
            var compilationUnit = (CompilationUnitSyntax)globalStatement.GetRequiredParent();
            return TextSpan.FromBounds(globalStatement.SpanStart, compilationUnit.Members.OfType<GlobalStatementSyntax>().Last().Span.End);
        }


        return ancestor?.Span;
    }
}
