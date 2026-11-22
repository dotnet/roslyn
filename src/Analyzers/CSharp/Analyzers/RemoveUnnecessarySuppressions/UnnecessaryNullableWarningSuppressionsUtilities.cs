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
        using var _4 = ArrayBuilder<(PostfixUnaryExpressionSyntax suppression, SyntaxNode rewrittenAncestor)>.GetInstance(out var inFieldBackedGetters);

        foreach (var (suppression, annotation) in nodeToAnnotation)
        {
            var rewritten = updateRoot.GetAnnotatedNodes(annotation).Single();
            var nodeToCheck = GetNodeToCheck(rewritten);

            if (nodeToCheck is GlobalStatementSyntax globalStatement)
            {
                inGlobalStatements.Add((suppression, globalStatement));
            }
            else if (GetFieldBackedPropertyOfGetter(nodeToCheck) != null)
            {
                inFieldBackedGetters.Add((suppression, nodeToCheck!));
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
        // 3. Analysis of the getters of properties that use the `field` keyword.  A suppression there can decide
        //    whether the property is null-resilient, and so whether the compiler reports CS9264 for it.  That warning
        //    is reported at the property or at a constructor's exit points, not in the getter, so check the whole type
        //    for warnings about the property.
        // 4. Any remaining nodes not covered by the above.
        CheckGlobalStatements();
        CheckFieldsAndProperties();
        CheckFieldBackedGetters();
        CheckRemainder();

        void CheckGlobalStatements()
        {
            if (inGlobalStatements.Count == 0)
                return;

            // If we're in a global statement, check all global statements from the start of the first one to the end of
            // the last one.
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

        void CheckFieldBackedGetters()
        {
            foreach (var getterGroup in inFieldBackedGetters.GroupBy(t => t.rewrittenAncestor))
            {
                var getter = getterGroup.Key;
                var property = GetFieldBackedPropertyOfGetter(getter)!;
                if (property.FirstAncestorOrSelf<TypeDeclarationSyntax>() is not { } typeDeclaration ||
                    updatedSemanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken) is not { } type)
                {
                    continue;
                }

                var propertyNameSpan = property.Identifier.Span;
                bool IsAboutProperty(Diagnostic diagnostic)
                    => diagnostic.AdditionalLocations.Any(l => l.SourceTree == updatedTree && l.SourceSpan == propertyNameSpan);

                using var _ = ArrayBuilder<Diagnostic>.GetInstance(out var diagnostics);
                diagnostics.AddRange(updatedSemanticModel.GetDiagnostics(typeDeclaration.Span, cancellationToken).Where(d =>
                    d.Location.SourceSpan.IntersectsWith(getter.Span) || IsAboutProperty(d)));

                // Constructors in other declarations of a partial type report their warnings there, so check those too.
                foreach (var constructor in type.Constructors)
                {
                    foreach (var reference in constructor.DeclaringSyntaxReferences)
                    {
                        if (reference.SyntaxTree == updatedTree && typeDeclaration.Span.Contains(reference.Span))
                            continue;

                        var constructorSemanticModel = reference.SyntaxTree == updatedTree
                            ? updatedSemanticModel
                            : updatedCompilation.GetSemanticModel(reference.SyntaxTree);
                        diagnostics.AddRange(constructorSemanticModel.GetDiagnostics(reference.Span, cancellationToken).Where(IsAboutProperty));
                    }
                }

                AddNodesIfNoErrorsOrWarnings(getterGroup, diagnostics);
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
            IEnumerable<(PostfixUnaryExpressionSyntax suppression, SyntaxNode rewrittenAncestor)> group,
            IEnumerable<Diagnostic> updatedDiagnostics)
        {
            if (ContainsErrorOrWarning(updatedDiagnostics))
                return;

            // If there were no errors in that span after removing all the suppressions, then we can offer all of these
            // nodes up for fixing.
            foreach (var (suppressionNode, _) in group)
                result.Add(suppressionNode);
        }
    }

    /// <summary>
    /// If <paramref name="nodeToCheck"/> is the <c>get</c> accessor, or the expression body, of a property that uses the
    /// <c>field</c> keyword, returns that property.
    /// </summary>
    private static PropertyDeclarationSyntax? GetFieldBackedPropertyOfGetter(SyntaxNode? nodeToCheck)
    {
        var property = nodeToCheck switch
        {
            AccessorDeclarationSyntax { Parent.Parent: PropertyDeclarationSyntax accessorProperty } accessor
                when accessor.IsKind(SyntaxKind.GetAccessorDeclaration) => accessorProperty,
            PropertyDeclarationSyntax { ExpressionBody: not null } expressionBodiedProperty => expressionBodiedProperty,
            _ => null,
        };

        return property != null && property.DescendantNodes().Any(n => n.IsKind(SyntaxKind.FieldExpression))
            ? property
            : null;
    }

    private static SyntaxNode? GetNodeToCheck(SyntaxNode updatedNode)
    {
        var globalStatement = updatedNode.Ancestors().OfType<GlobalStatementSyntax>().FirstOrDefault();

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
        var ancestor = updatedNode.Ancestors().FirstOrDefault(
            n => n is AttributeSyntax
                   or AccessorDeclarationSyntax
                   or AnonymousMethodExpressionSyntax
                   or BaseFieldDeclarationSyntax
                   or BaseMethodDeclarationSyntax
                   or BasePropertyDeclarationSyntax);

        return ancestor;
    }
}
