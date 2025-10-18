// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryUnsafeModifier;

internal static class UnnecessaryUnsafeModifierUtilities
{
    private enum NodeGroup
    {
        LocalFunctions,
        MemberDeclarations,
        TypeDeclarations,
    }

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
        var originalTree = semanticModel.SyntaxTree;
        var originalRoot = originalTree.GetRoot(cancellationToken);

        using var _1 = ArrayBuilder<SyntaxNode>.GetInstance(out var nodesToCheck);

        foreach (var existingNode in originalRoot.DescendantNodes())
        {
            if (existingNode is not MemberDeclarationSyntax and not LocalFunctionStatementSyntax)
                continue;

            if (GetUnsafeModifier(existingNode) != default &&
                ShouldAnalyzeNode(semanticModel, existingNode, cancellationToken))
            {
                nodesToCheck.Add(existingNode);
            }
        }

        // We actually process things in three passes.  That way We can tell if containing unsafe modifiers are
        // unnecessary, even if inner ones are necessary.  For example, consider an unsafe type with an unsafe method
        // inside of it.  We don't want to remove both 'unsafe' modifiers and have them both be considered necessary
        // just because the method one was actually the important one.

        foreach (var group in nodesToCheck.GroupBy(node =>
            node switch
            {
                LocalFunctionStatementSyntax => NodeGroup.LocalFunctions,
                TypeDeclarationSyntax => NodeGroup.TypeDeclarations,
                _ => NodeGroup.MemberDeclarations,
            }))
        {
            AddUnnecessaryNodes(semanticModel, group, result, cancellationToken);
        }
    }

    public static void AddUnnecessaryNodes(
        SemanticModel semanticModel,
        IEnumerable<SyntaxNode> nodesToCheck,
        ArrayBuilder<SyntaxNode> result,
        CancellationToken cancellationToken)
    {
        using var _2 = PooledDictionary<SyntaxNode, SyntaxAnnotation>.GetInstance(out var nodeToAnnotation);
        foreach (var node in nodesToCheck)
            nodeToAnnotation[node] = new SyntaxAnnotation();

        ForkSemanticModelAndCheckNodes(semanticModel, nodesToCheck, nodeToAnnotation, result, cancellationToken);
    }

    private static void ForkSemanticModelAndCheckNodes(
        SemanticModel semanticModel,
        IEnumerable<SyntaxNode> nodesToCheck,
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
    {
        foreach (var modifier in GetModifiers(node))
        {
            if (modifier.Kind() == SyntaxKind.UnsafeKeyword)
                return modifier;
        }

        return default;
    }
}
