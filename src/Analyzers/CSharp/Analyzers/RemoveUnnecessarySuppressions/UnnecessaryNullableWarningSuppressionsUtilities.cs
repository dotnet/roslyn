// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

        var nodeToCheck = GetNodeToCheck(postfixUnary);
        if (nodeToCheck is null)
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

    private static SyntaxNode? GetNodeToCheck(SyntaxNode node)
    {
        var globalStatement = node.Ancestors().OfType<GlobalStatementSyntax>().FirstOrDefault();

        if (globalStatement is not null)
            return globalStatement;

        // Otherwise, find our containing code-containing member (attributes, accessors, fields, methods, properties,
        // anonymous methods), and check that entire member.  This means we only offer to remove the suppression if all
        // the suppressions in the member are unnecessary.  We need this granularity as doing things on a
        // per-suppression is just far too slow.
        //
        // We also check at this level because suppressions can have effects far outside of the containing statement (or
        // even things like the containing block.  For example: a suppression inside a block like `a = b!` can affect
        // the nullability of a variable which may be referenced far outside of the block.  So we really need to check
        // the entire code region that the suppression is in to make an accurate determination.
        var ancestor = node.Ancestors().FirstOrDefault(
            n => n is AttributeSyntax
                   or AccessorDeclarationSyntax
                   or AnonymousMethodExpressionSyntax
                   or BaseFieldDeclarationSyntax
                   or BaseMethodDeclarationSyntax
                   or BasePropertyDeclarationSyntax);

        return ancestor;
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

        using var _1 = ArrayBuilder<(PostfixUnaryExpressionSyntax suppression, SyntaxNode rewrittenAncestor)>.GetInstance(out var inGlobalStatements);
        using var _2 = ArrayBuilder<(PostfixUnaryExpressionSyntax suppression, SyntaxNode rewrittenAncestor)>.GetInstance(out var inFieldsOrProperties);
        using var _3 = ArrayBuilder<(PostfixUnaryExpressionSyntax suppression, SyntaxNode rewrittenAncestor)>.GetInstance(out var remainder);

        foreach (var (suppression, annotation) in nodeToAnnotation)
        {
            var rewritten = updateRoot.GetAnnotatedNodes(annotation).Single();
            var nodeToCheck = GetNodeToCheck(rewritten);

            if (nodeToCheck is GlobalStatementSyntax globalStatement)
            {
                inGlobalStatements.Add((suppression, globalStatement));
            }
            else if (nodeToCheck is BaseFieldDeclarationSyntax or BasePropertyDeclarationSyntax)
            {
                inFieldsOrProperties.Add((suppression, nodeToCheck));
            }
            else if (nodeToCheck != null)
            {
                remainder.Add((suppression, nodeToCheck));
            }
        }

        // Break checking into 3 phases.
        //
        // 1. Analysis of the top-level statements if any suppressions are in top level code.
        // 2. Analysis of field/property initializers.  This is necessary as we (currently) cannot ask for diagnostics
        //    for a field/property initializer directly.  The compiler will not return accurate nullable warnings for
        //    these if they are impacted by a constructor in the type.  To workaround this, we ask for diagnostics for
        //    the entire type, and filter to the spans of the field/property.
        // 3. Any remaining nodes not covered by the above.
        CheckGlobalStatements();
        CheckFieldsAndProperties();
        CheckRemainder();

        void CheckGlobalStatements()
        {
            if (inGlobalStatements.Count == 0)
                return;

            var compilationUnit = (CompilationUnitSyntax)updatedSemanticModel.SyntaxTree.GetRoot(cancellationToken);
            var globalStatements = compilationUnit.Members.OfType<GlobalStatementSyntax>();
            var span = TextSpan.FromBounds(globalStatements.First().SpanStart, globalStatements.Last().Span.End);

            AddNodesIfNoErrorsOrWarnings(
                inGlobalStatements,
                updatedSemanticModel.GetDiagnostics(span, cancellationToken));
        }

        void CheckFieldsAndProperties()
        {
            foreach (var typeDeclarationGroup in inFieldsOrProperties.GroupBy(t => t.rewrittenAncestor.FirstAncestorOrSelf<TypeDeclarationSyntax>()))
            {
                var typeDeclaration = typeDeclarationGroup.Key;
                if (typeDeclaration is null)
                    continue;

                var typeDeclarationDiagnostics = updatedSemanticModel.GetDiagnostics(typeDeclaration.Span, cancellationToken);
                CheckDiagnostics(typeDeclarationGroup, _ => typeDeclarationDiagnostics);
            }
        }

        void CheckRemainder()
        {
            CheckDiagnostics(remainder, n => updatedSemanticModel.GetDiagnostics(n.Span, cancellationToken));
        }

        void CheckDiagnostics(
            IEnumerable<(PostfixUnaryExpressionSyntax suppression, SyntaxNode rewrittenAncestor)> nodes,
            Func<SyntaxNode, ImmutableArray<Diagnostic>> computeDiagnostics)
        {
            foreach (var ancestorGroup in nodes.GroupBy(t => t.rewrittenAncestor))
            {
                var rewrittenAncestor = ancestorGroup.Key;
                var diagnostics = computeDiagnostics(rewrittenAncestor);
                var intersectingDiagnostics = diagnostics.Where(d => d.Location.SourceSpan.IntersectsWith(rewrittenAncestor.Span));
                AddNodesIfNoErrorsOrWarnings(ancestorGroup, intersectingDiagnostics);
            }
        }

        void AddNodesIfNoErrorsOrWarnings(
            IEnumerable<(PostfixUnaryExpressionSyntax suppression, SyntaxNode rewrittenAncestor)> nodes,
            IEnumerable<Diagnostic> intersectingDiagnostics)
        {
            // If there were no errors in that span after removing all the suppressions, then we can offer all of these
            // nodes up for fixing.
            if (!ContainsErrorOrWarning(intersectingDiagnostics))
                result.AddRange(nodes.Select(t => t.suppression));
        }
    }
}
