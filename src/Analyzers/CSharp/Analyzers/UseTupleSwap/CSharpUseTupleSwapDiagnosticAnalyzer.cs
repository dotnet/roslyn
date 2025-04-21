// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.UseTupleSwap;

/// <summary>
/// Looks for code of the form:
/// 
/// <code>
///     var temp = expr_a;
///     expr_a = expr_b;
///     expr_b = temp;
/// </code>
///
/// and converts it to:
/// 
/// <code>
///     (expr_b, expr_a) = (expr_a, expr_b);
/// </code>
/// 
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpUseTupleSwapDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
{
    public CSharpUseTupleSwapDiagnosticAnalyzer()
        : base(IDEDiagnosticIds.UseTupleSwapDiagnosticId,
               EnforceOnBuildValues.UseTupleSwap,
               CSharpCodeStyleOptions.PreferTupleSwap,
               new LocalizableResourceString(
                   nameof(CSharpAnalyzersResources.Use_tuple_to_swap_values), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
    {
    }

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(context =>
        {
            // Tuples are only available in C# 7 and above.
            var compilation = context.Compilation;
            if (compilation.LanguageVersion() < LanguageVersion.CSharp7)
                return;

            context.RegisterSyntaxNodeAction(
                AnalyzeLocalDeclarationStatement,
                SyntaxKind.LocalDeclarationStatement);
        });
    }

    private void AnalyzeLocalDeclarationStatement(SyntaxNodeAnalysisContext syntaxContext)
    {
        var cancellationToken = syntaxContext.CancellationToken;
        var styleOption = syntaxContext.GetCSharpAnalyzerOptions().PreferTupleSwap;
        if (!styleOption.Value || ShouldSkipAnalysis(syntaxContext, styleOption.Notification))
            return;

        // `var expr_temp = expr_a`;
        var localDeclarationStatement = (LocalDeclarationStatementSyntax)syntaxContext.Node;
        if (localDeclarationStatement.UsingKeyword != default ||
            localDeclarationStatement.AwaitKeyword != default)
        {
            return;
        }

        if (localDeclarationStatement.Declaration.Variables.Count != 1)
            return;

        var variableDeclarator = localDeclarationStatement.Declaration.Variables.First();
        var localDeclarationExprA = variableDeclarator.Initializer?.Value.WalkDownParentheses();
        if (localDeclarationExprA == null)
            return;

        // `expr_a = expr_b`;
        var firstAssignmentStatement = localDeclarationStatement.GetNextStatement();
        if (!IsSimpleAssignment(firstAssignmentStatement, out var firstAssignmentExprA, out var firstAssignmentExprB))
            return;

        // `expr_b = expr_temp;`
        var secondAssignmentStatement = firstAssignmentStatement.GetNextStatement();
        if (!IsSimpleAssignment(secondAssignmentStatement, out var secondAssignmentExprB, out var secondAssignmentExprTemp))
            return;

        if (!localDeclarationExprA.IsEquivalentTo(firstAssignmentExprA, topLevel: false))
            return;

        if (!firstAssignmentExprB.IsEquivalentTo(secondAssignmentExprB, topLevel: false))
            return;

        if (secondAssignmentExprTemp is not IdentifierNameSyntax { Identifier: var secondAssignmentExprTempIdentifier })
            return;

        if (variableDeclarator.Identifier.ValueText != secondAssignmentExprTempIdentifier.ValueText)
            return;

        // Can't swap ref-structs.
        var semanticModel = syntaxContext.SemanticModel;
        var local = (ILocalSymbol)semanticModel.GetRequiredDeclaredSymbol(variableDeclarator, cancellationToken);
        if (local.Type.IsRefLikeType || local.Type.RequiresUnsafeModifier())
            return;

        var additionalLocations = ImmutableArray.Create(
            localDeclarationStatement.GetLocation(),
            firstAssignmentStatement.GetLocation(),
            secondAssignmentStatement.GetLocation());

        // If the diagnostic is not hidden, then just place the user visible part
        // on the local being initialized with the lambda.
        syntaxContext.ReportDiagnostic(DiagnosticHelper.Create(
            Descriptor,
            localDeclarationStatement.GetFirstToken().GetLocation(),
            styleOption.Notification,
            syntaxContext.Options,
            additionalLocations,
            properties: null));
    }

    private static bool IsSimpleAssignment(
        [NotNullWhen(true)] StatementSyntax? assignmentStatement,
        [NotNullWhen(true)] out ExpressionSyntax? left,
        [NotNullWhen(true)] out ExpressionSyntax? right)
    {
        left = null;
        right = null;
        if (assignmentStatement == null)
            return false;

        if (assignmentStatement is not ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax(SyntaxKind.SimpleAssignmentExpression) assignment })
            return false;

        left = assignment.Left.WalkDownParentheses();
        right = assignment.Right.WalkDownParentheses();

        return left is not RefExpressionSyntax && right is not RefExpressionSyntax;
    }
}
