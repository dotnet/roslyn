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

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryUnsafeModifier;

internal static class UnnecessaryUnsafeModifierUtilities
{
    private static bool ContainsError(IEnumerable<Diagnostic> diagnostics)
        => diagnostics.Any(d => d.Severity is DiagnosticSeverity.Error);

    private static bool ShouldAnalyzeNode(SemanticModel semanticModel, SyntaxNode declaration, CancellationToken cancellationToken)
    {
        if (ContainsError(declaration.GetDiagnostics()))
            return false;

        var semanticDiagnostics = semanticModel.GetDiagnostics(declaration.Span, cancellationToken);
        if (ContainsError(semanticDiagnostics))
            return false;

        return true;
    }

    public static void AddUnnecessaryNodes(
        SemanticModel semanticModel,
        ArrayBuilder<SyntaxNode> result,
        CancellationToken cancellationToken)
    {
        using var _1 = ArrayBuilder<SyntaxNode>.GetInstance(out var nodesToCheck);

        var originalTree = semanticModel.SyntaxTree;
        var originalRoot = originalTree.GetRoot(cancellationToken);

        foreach (var existingNode in originalRoot.DescendantNodes())
        {
            if (existingNode is MemberDeclarationSyntax declaration &&
                declaration.Modifiers.Any(SyntaxKind.UnsafeKeyword) &&
                ShouldAnalyzeNode(semanticModel, declaration, cancellationToken))
            {
                nodesToCheck.Add(declaration);
            }
            else if (existingNode is LocalFunctionStatementSyntax localFunction &&
                     localFunction.Modifiers.Any(SyntaxKind.UnsafeKeyword) &&
                     ShouldAnalyzeNode(semanticModel, localFunction, cancellationToken))
            {
                nodesToCheck.Add(localFunction);
            }
        }

        if (nodesToCheck.IsEmpty)
            return;

        using var _2 = PooledDictionary<SyntaxNode, SyntaxAnnotation>.GetInstance(out var nodeToAnnotation);
        foreach (var node in nodesToCheck)
            nodeToAnnotation[node] = new SyntaxAnnotation();

        ForkSemanticModelAndCheckNodes(semanticModel, nodesToCheck, nodeToAnnotation, result, cancellationToken);
    }

    private static void ForkSemanticModelAndCheckNodes(
        SemanticModel semanticModel,
        ArrayBuilder<SyntaxNode> nodesToCheck,
        PooledDictionary<SyntaxNode, SyntaxAnnotation> nodeToAnnotation,
        ArrayBuilder<SyntaxNode> result,
        CancellationToken cancellationToken)
    {
        var originalTree = semanticModel.SyntaxTree;
        var originalRoot = originalTree.GetRoot(cancellationToken);

        var updatedTree = originalTree.WithRootAndOptions(
            originalRoot.ReplaceNodes(
                nodesToCheck,
                (original, current) => WithoutUnsafeModifier(current).WithAdditionalAnnotations(nodeToAnnotation[original])),
            originalTree.Options);
        var updateRoot = updatedTree.GetRoot(cancellationToken);
        var updatedCompilation = semanticModel.Compilation
            .ReplaceSyntaxTree(originalTree, updatedTree)
            .WithOptions(semanticModel.Compilation.Options.WithSpecificDiagnosticOptions([]));
        var updatedSemanticModel = updatedCompilation.GetSemanticModel(updatedTree);

        foreach (var (originalNode, annotation) in nodeToAnnotation)
        {
            var newNode = updateRoot.GetAnnotatedNodes(annotation).Single();

            var updatedDiagnostics = updatedSemanticModel.GetDiagnostics(newNode.Span, cancellationToken);
            if (ContainsError(updatedDiagnostics))
                continue;

            result.Add(originalNode);
        }
    }

    private static SyntaxNode WithoutUnsafeModifier(SyntaxNode node)
        => node switch
        {
            MemberDeclarationSyntax memberDeclaration => memberDeclaration.WithModifiers(GetNewModifierList(memberDeclaration)),
            LocalFunctionStatementSyntax localFunction => localFunction.WithModifiers(GetNewModifierList(localFunction)),
            _ => throw ExceptionUtilities.UnexpectedValue(node)
        };

    private static SyntaxTokenList GetNewModifierList(SyntaxNode node)
        => GetModifiers(node).Remove(GetUnsafeModifier(node));

    private static SyntaxTokenList GetModifiers(SyntaxNode node)
        => node switch
        {
            MemberDeclarationSyntax memberDeclaration => memberDeclaration.Modifiers,
            LocalFunctionStatementSyntax localFunction => localFunction.Modifiers,
            _ => throw ExceptionUtilities.UnexpectedValue(node)
        };

    public static SyntaxToken GetUnsafeModifier(SyntaxNode node)
        => GetModifiers(node).First(m => m.IsKind(SyntaxKind.UnsafeKeyword));
}
