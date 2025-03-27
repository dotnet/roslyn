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
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.UsePatternMatching;

/// <summary>
/// Looks for code of the form:
/// 
///     if (expr is Type)
///     {
///         var v = (Type)expr;
///     }
///     
/// and converts it to:
/// 
///     if (expr is Type v)
///     {
///     }
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpIsAndCastCheckDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
{
    public static readonly CSharpIsAndCastCheckDiagnosticAnalyzer Instance = new();

    public CSharpIsAndCastCheckDiagnosticAnalyzer()
        : base(IDEDiagnosticIds.InlineIsTypeCheckId,
               EnforceOnBuildValues.InlineIsType,
               CSharpCodeStyleOptions.PreferPatternMatchingOverIsWithCastCheck,
               new LocalizableResourceString(
                   nameof(CSharpAnalyzersResources.Use_pattern_matching), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
    {
    }

    protected override void InitializeWorker(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(context =>
        {
            // "x is Type y" is only available in C# 7.0 and above.  Don't offer this refactoring
            // in projects targeting a lesser version.
            if (context.Compilation.LanguageVersion() < LanguageVersion.CSharp7)
                return;

            // We wrap the SyntaxNodeAction within a CodeBlockStartAction, which allows us to
            // get callbacks for 'is' expression nodes, but analyze nodes across the entire code block
            // and eventually report a diagnostic on the local declaration statement node.
            // Without the containing CodeBlockStartAction, our reported diagnostic would be classified
            // as a non-local diagnostic and would not participate in lightbulb for computing code fixes.
            context.RegisterCodeBlockStartAction<SyntaxKind>(blockStartContext =>
                blockStartContext.RegisterSyntaxNodeAction(SyntaxNodeAction, SyntaxKind.IsExpression));
        });
    }

    private void SyntaxNodeAction(SyntaxNodeAnalysisContext syntaxContext)
    {
        var styleOption = syntaxContext.GetCSharpAnalyzerOptions().PreferPatternMatchingOverIsWithCastCheck;
        if (!styleOption.Value || ShouldSkipAnalysis(syntaxContext, styleOption.Notification))
        {
            // Bail immediately if the user has disabled this feature.
            return;
        }

        var isExpression = (BinaryExpressionSyntax)syntaxContext.Node;

        if (!TryGetPatternPieces(isExpression,
                out var ifStatement, out var localDeclarationStatement,
                out var declarator, out var castExpression))
        {
            return;
        }

        // Bail out if the potential diagnostic location is outside the analysis span.
        if (!syntaxContext.ShouldAnalyzeSpan(localDeclarationStatement.Span))
            return;

        // It's of the form:
        //
        //     if (expr is Type)
        //     {
        //         var v = (Type)expr;
        //     }

        // Make sure that moving 'v' to the outer scope won't cause any conflicts.

        var ifStatementScope = ifStatement.Parent.IsKind(SyntaxKind.Block)
            ? ifStatement.Parent
            : ifStatement;

        if (ContainsVariableDeclaration(ifStatementScope, declarator))
        {
            // can't switch to using a pattern here as it would cause a scoping
            // problem.
            //
            // TODO(cyrusn): Consider allowing the user to do this, but giving 
            // them an error preview.
            return;
        }

        var cancellationToken = syntaxContext.CancellationToken;
        var semanticModel = syntaxContext.SemanticModel;
        var localSymbol = (ILocalSymbol)semanticModel.GetRequiredDeclaredSymbol(declarator, cancellationToken);
        var isType = semanticModel.GetTypeInfo(castExpression.Type).Type;

        if (isType.IsNullable())
        {
            // not legal to write "if (x is int? y)"
            return;
        }

        if (isType?.TypeKind == TypeKind.Dynamic)
        {
            // Not legal to use dynamic in a pattern.
            return;
        }

        if (!localSymbol.Type.Equals(isType))
        {
            // we have something like:
            //
            //      if (x is DerivedType)
            //      {
            //          BaseType b = (DerivedType)x;
            //      }
            //
            // It's not necessarily safe to convert this to:
            //
            //      if (x is DerivedType b) { ... }
            //
            // That's because there may be later code that wants to do something like assign a 
            // 'BaseType' into 'b'.  As we've now claimed that it must be DerivedType, that 
            // won't work.  This might also cause unintended changes like changing overload
            // resolution.  So, we conservatively do not offer the change in a situation like this.
            return;
        }

        // Looks good!
        var additionalLocations = ImmutableArray.Create(
            ifStatement.GetLocation(),
            localDeclarationStatement.GetLocation());

        // Put a diagnostic with the appropriate severity on the declaration-statement itself.
        syntaxContext.ReportDiagnostic(DiagnosticHelper.Create(
            Descriptor,
            localDeclarationStatement.GetLocation(),
            styleOption.Notification,
            syntaxContext.Options,
            additionalLocations,
            properties: null));
    }

    public static bool TryGetPatternPieces(
        BinaryExpressionSyntax isExpression,
        [NotNullWhen(true)] out IfStatementSyntax? ifStatement,
        [NotNullWhen(true)] out LocalDeclarationStatementSyntax? localDeclarationStatement,
        [NotNullWhen(true)] out VariableDeclaratorSyntax? declarator,
        [NotNullWhen(true)] out CastExpressionSyntax? castExpression)
    {
        localDeclarationStatement = null;
        declarator = null;
        castExpression = null;

        // The is check has to be in an if check: "if (x is Type)
        if (!isExpression.Parent.IsKind(SyntaxKind.IfStatement, out ifStatement))
            return false;

        if (ifStatement.Statement is not BlockSyntax block)
            return false;

        if (block.Statements is [TryStatementSyntax tryStatement, ..])
            block = tryStatement.Block;

        if (block.Statements.Count == 0)
            return false;

        var firstStatement = block.Statements[0];
        if (!firstStatement.IsKind(SyntaxKind.LocalDeclarationStatement, out localDeclarationStatement))
            return false;

        if (localDeclarationStatement.Declaration.Variables.Count != 1)
            return false;

        declarator = localDeclarationStatement.Declaration.Variables[0];
        if (declarator.Initializer == null)
            return false;

        var declaratorValue = declarator.Initializer.Value.WalkDownParentheses();
        if (!declaratorValue.IsKind(SyntaxKind.CastExpression, out castExpression))
            return false;

        if (!SyntaxFactory.AreEquivalent(isExpression.Left.WalkDownParentheses(), castExpression.Expression.WalkDownParentheses(), topLevel: false) ||
            !SyntaxFactory.AreEquivalent(isExpression.Right.WalkDownParentheses(), castExpression.Type, topLevel: false))
        {
            return false;
        }

        return true;
    }

    private static bool ContainsVariableDeclaration(
        SyntaxNode scope, VariableDeclaratorSyntax variable)
    {
        var variableName = variable.Identifier.ValueText;

        using var _ = ArrayBuilder<SyntaxNode>.GetInstance(out var stack);
        stack.Push(scope);

        while (stack.TryPop(out var current))
        {
            if (current == variable)
                continue;

            if (current is VariableDeclaratorSyntax declarator &&
                declarator.Identifier.ValueText.Equals(variableName))
            {
                return true;
            }
            else if (current is SingleVariableDesignationSyntax designation &&
                designation.Identifier.ValueText.Equals(variableName))
            {
                return true;
            }

            foreach (var child in current.ChildNodesAndTokens())
            {
                if (child.AsNode(out var childNode))
                    stack.Push(childNode);
            }
        }

        return false;
    }

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;
}
