// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

/// <summary>
/// Base class, responsible for walking the entire tree, and finding methods to analyze.
/// </summary>
internal abstract class AbstractRemoveUnnecessaryAsyncModifierDiagnosticAnalyzer(
    string diagnosticId,
    EnforceOnBuild enforceOnBuild) : AbstractBuiltInCodeStyleDiagnosticAnalyzer(
        diagnosticId,
        enforceOnBuild,
        option: null,
        new LocalizableResourceString(nameof(AnalyzersResources.Make_method_synchronous), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        new LocalizableResourceString(nameof(AnalyzersResources.Method_can_be_made_synchronous), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
{
    protected abstract bool ShouldAnalyze(SemanticModel semanticModel, SyntaxNode methodLike, CancellationToken cancellationToken);

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
        => context.RegisterSemanticModelAction(AnalyzeSemanticModel);

    protected static bool IsInterfaceImplementationOrOverride(IMethodSymbol methodSymbol)
        => methodSymbol.IsOverride || methodSymbol.ExplicitOrImplicitInterfaceImplementations().Length > 0;

    private void AnalyzeSemanticModel(SemanticModelAnalysisContext context)
    {
        if (ShouldSkipAnalysis(context, notification: null))
            return;

        var cancellationToken = context.CancellationToken;
        var semanticModel = context.SemanticModel;
        var syntaxTree = semanticModel.SyntaxTree;

        var root = syntaxTree.GetRoot(cancellationToken);
        using var _1 = ArrayBuilder<SyntaxNode>.GetInstance(out var stack);
        using var _2 = ArrayBuilder<SyntaxNode>.GetInstance(out var currentMethodStack);
        stack.Push(root);

        while (stack.TryPop(out var current))
        {
            // If it is an async-method that this analyzer cares about, check to see if it has any 'await' expressions
            // in it or not.
            if (IsMethodLike(current) && HasAsyncModifier(current) && ShouldAnalyze(semanticModel, current, cancellationToken))
            {
                CheckForNoAwaitExpressions(current);
            }
            else
            {
                // Otherwise, just keep descending, looking for things to analyze.
                foreach (var child in current.ChildNodesAndTokens().Reverse())
                {
                    if (child.AsNode(out var childNode))
                        stack.Push(childNode);
                }
            }
        }

        static bool IsMethodLike(SyntaxNode current)
            => current is MethodDeclarationSyntax or LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax;

        static bool HasAsyncModifier(SyntaxNode methodLike)
            => GetAsyncModifier(methodLike) != default;

        static SyntaxToken GetAsyncModifier(SyntaxNode methodLike)
            => methodLike switch
            {
                MethodDeclarationSyntax methodDeclaration => GetAsyncModifierToken(methodDeclaration.Modifiers),
                LocalFunctionStatementSyntax localFunctionStatement => GetAsyncModifierToken(localFunctionStatement.Modifiers),
                AnonymousFunctionExpressionSyntax anonymousFunctionExpression => GetAsyncModifierToken(anonymousFunctionExpression.Modifiers),
                _ => default
            };

        static SyntaxToken GetAsyncModifierToken(SyntaxTokenList modifiers)
        {
            foreach (var modifier in modifiers)
            {
                if (modifier.Kind() == SyntaxKind.AsyncKeyword)
                    return modifier;
            }

            return default;
        }

        void CheckForNoAwaitExpressions(SyntaxNode methodLike)
        {
            currentMethodStack.Clear();
            currentMethodStack.Push(methodLike);

            var seenAwait = false;

            while (currentMethodStack.TryPop(out var current))
            {
                if (current != methodLike && IsMethodLike(current))
                {
                    // don't recurse into nested methods.  Each is their own analysis context for checking for an
                    // 'await'. Instead, push it back onto the main stack to analyze later.
                    stack.Push(current);
                }
                else
                {
                    // Walk every child to see if we have an await.  Note, we don't stop when we see an await, as we
                    // still need to see if we run into nested methods that need to be analyzed.
                    seenAwait = seenAwait || IsAwait(current);

                    foreach (var child in current.ChildNodesAndTokens().Reverse())
                    {
                        if (child.AsNode(out var childNode))
                            currentMethodStack.Push(childNode);
                    }
                }
            }

            if (!seenAwait)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    this.Descriptor,
                    GetAsyncModifier(methodLike).GetLocation()));
            }
        }

        static bool IsAwait(SyntaxNode node)
            => node switch
            {
                AwaitExpressionSyntax => true,
                CommonForEachStatementSyntax foreachStatement => foreachStatement.AwaitKeyword != default,
                UsingStatementSyntax usingStatement => usingStatement.AwaitKeyword != default,
                LocalDeclarationStatementSyntax localDeclaration => localDeclaration.AwaitKeyword != default,
                _ => false,
            };
    }
}

/// <summary>
/// Analyzer specific to reporting for normal methods (not interface-impl or override) or lambdas.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpRemoveUnnecessaryAsyncModifierDiagnosticAnalyzer() : AbstractRemoveUnnecessaryAsyncModifierDiagnosticAnalyzer(
    IDEDiagnosticIds.RemoveUnnecessaryAsyncModifier,
    EnforceOnBuildValues.RemoveUnnecessaryAsyncModifier)
{
    protected override bool ShouldAnalyze(SemanticModel semanticModel, SyntaxNode methodLike, CancellationToken cancellationToken)
    {
        if (methodLike is MethodDeclarationSyntax methodDeclaration)
        {
            // For methods, check if it's an interface/override, and analyze it according to which analyzer we are.
            var methodSymbol = semanticModel.GetRequiredDeclaredSymbol(methodDeclaration, cancellationToken);
            return !IsInterfaceImplementationOrOverride(methodSymbol);
        }
        else
        {
            // Have a lambda.  These are only reported for the normal case.  Not for the interface/override case.
            return true;
        }
    }
}

/// <summary>
/// Analyzer specific to reporting for interface-impl or override methods only.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpRemoveUnnecessaryAsyncModifierInterfaceImplementationOrOverrideDiagnosticAnalyzer() : AbstractRemoveUnnecessaryAsyncModifierDiagnosticAnalyzer(
    IDEDiagnosticIds.RemoveUnnecessaryAsyncModifierInterfaceImplementationOrOverride,
    EnforceOnBuildValues.RemoveUnnecessaryAsyncModifierInterfaceImplementationOrOverride)
{
    protected override bool ShouldAnalyze(SemanticModel semanticModel, SyntaxNode methodLike, CancellationToken cancellationToken)
    {
        if (methodLike is MethodDeclarationSyntax methodDeclaration)
        {
            // For methods, check if it's an interface/override, and analyze it according to which analyzer we are.
            var methodSymbol = semanticModel.GetRequiredDeclaredSymbol(methodDeclaration, cancellationToken);
            return IsInterfaceImplementationOrOverride(methodSymbol);
        }
        else
        {
            // Have a lambda.  These are only reported for the normal case.  Not for the interface/override case.
            return false;
        }
    }
}
