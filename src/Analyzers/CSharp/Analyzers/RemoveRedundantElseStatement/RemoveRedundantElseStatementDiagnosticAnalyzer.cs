// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.RemoveRedundantElseStatement;

/// <summary>
/// Looks for code like:
/// <code>
///     if (a == b)
///     {
///         return c;
///     }
///     else
///     {
///         return d;
///     }
/// </code>
/// 
/// And offers to convert it to:
///
/// <code>
///     if (a == b)
///     {
///         return c;
///     }
///     
///     return d;
/// </code>
///
/// For this conversion to make sense statement(s) in each <c>if</c> and <c>else if</c> must end with a jump
/// (return, break, continue or throw) in order to preserve the program's correctness.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class RemoveRedundantElseStatementDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
{
    public RemoveRedundantElseStatementDiagnosticAnalyzer()
        : base(IDEDiagnosticIds.RemoveRedundantElseStatementDiagnosticId,
               EnforceOnBuildValues.RemoveRedundantElseStatement,
               CSharpCodeStyleOptions.PreferRemoveRedundantElseStatement,
               new LocalizableResourceString(nameof(CSharpAnalyzersResources.Remove_redundant_else_statement), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
    {
    }

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
        => context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.IfStatement);

    private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
        var codeStyleOption = context
            .GetCSharpAnalyzerOptions()
            .PreferRemoveRedundantElseStatement;

        if (!codeStyleOption.Value)
            return;

        var ifStatement = (IfStatementSyntax)context.Node;
        if (!CanSimplify(context.SemanticModel, ifStatement, out var elseClause, context.CancellationToken))
            return;

        context.ReportDiagnostic(DiagnosticHelper.Create(
            Descriptor,
            elseClause.ElseKeyword.GetLocation(),
            codeStyleOption.Notification.Severity,
            additionalLocations: ImmutableArray.Create(ifStatement.GetLocation()),
            properties: null));
    }

    public static bool CanSimplify(
        SemanticModel semanticModel,
        IfStatementSyntax ifStatement,
        [NotNullWhen(true)] out ElseClauseSyntax? elseClause,
        CancellationToken cancellationToken)
    {
        elseClause = null;
        if (ifStatement.Parent is not BlockSyntax and not SwitchSectionSyntax and not GlobalStatementSyntax)
            return false;

        elseClause = FindRedundantElse(ifStatement);
        if (elseClause is null ||
            WillCauseVariableCollision(semanticModel, ifStatement, elseClause, cancellationToken))
        {
            return false;
        }

        return true;
    }

    private static ElseClauseSyntax? FindRedundantElse(IfStatementSyntax ifStatement)
    {
        var elseClause = ifStatement.Else;
        while (elseClause is not null)
        {
            if (!AllCodePathsEndWithJump(ifStatement.Statement))
                return null;

            // Reached else not followed by an if
            if (elseClause.Statement is not IfStatementSyntax elseIfStatement)
                break;

            ifStatement = elseIfStatement;
            elseClause = elseIfStatement.Else;
        }

        return elseClause;
    }

    private static bool AllCodePathsEndWithJump(StatementSyntax statement)
    {
        if (IsJumpStatement(statement))
            return true;
 
        if (statement is IfStatementSyntax ifStatement)
        {
            // Check for nested if/else
            var redundantElse = FindRedundantElse(ifStatement);
            return redundantElse is not null && AllCodePathsEndWithJump(redundantElse.Statement);
        }

        if (statement is BlockSyntax { Statements: [.., var lastStatement] })
            return AllCodePathsEndWithJump(lastStatement);

        return false;
    }

    // Goto could be added as well
    // but it would require more analysis
    private static bool IsJumpStatement(StatementSyntax statement)
        => statement.Kind()
            is SyntaxKind.ReturnStatement
            or SyntaxKind.BreakStatement
            or SyntaxKind.ContinueStatement
            or SyntaxKind.YieldBreakStatement
            or SyntaxKind.ThrowStatement;

    private static bool WillCauseVariableCollision(SemanticModel semanticModel, IfStatementSyntax ifStatement, ElseClauseSyntax elseClause, CancellationToken cancellationToken)
    {
        if (elseClause.Statement is not BlockSyntax elseBlock)
            return false;

        var outerScope = ifStatement.Parent switch
        {
            BlockSyntax block => block,
            SwitchSectionSyntax switchSection => switchSection.Parent,
            GlobalStatementSyntax global => global.Parent,
            _ => throw ExceptionUtilities.UnexpectedValue(ifStatement.Parent?.Kind()),
        };

        var existingSymbols = semanticModel
            .GetExistingSymbols(outerScope, cancellationToken)
            .ToLookup(s => s.Name);

        var operation = semanticModel.GetRequiredOperation(elseClause.Statement, cancellationToken);

        return operation is IBlockOperation blockOperation &&
            blockOperation.Locals.Any(local => existingSymbols[local.Name].Any(other => Equals(local.ContainingSymbol, other.ContainingSymbol) && !Equals(local, other)));
    }
}
