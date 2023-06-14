// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.RemoveRedundantElseStatement
{
    /// <summary>
    /// Looks for code like:
    ///
    ///     ```c#
    ///     if (a == b)
    ///     {
    ///         return c;
    ///     }
    ///     else
    ///     {
    ///         return d;
    ///     }
    ///     ```
    /// 
    /// And offers to convert it to:
    ///
    ///     ```c#
    ///     if (a == b)
    ///     {
    ///         return c;
    ///     }
    ///     
    ///     return d;
    ///     ```
    ///
    /// For this conversion to make sense statement(s) in each `if` and `else if`
    /// must end with a jump (return, break, continue or throw) 
    /// in order to preserve the program's correctness.
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
            => DiagnosticAnalyzerCategory.SyntaxTreeWithoutSemanticsAnalysis;

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

            if (ifStatement.Parent is not BlockSyntax and not GlobalStatementSyntax and not SwitchSectionSyntax)
            {
                return;
            }

            var redundantElse = FindRedundantElse(ifStatement);

            if (redundantElse is null || WillCauseVariableCollision(context.SemanticModel, ifStatement, redundantElse, context.CancellationToken))
            {
                return;
            }

            context.ReportDiagnostic(DiagnosticHelper.Create(
                Descriptor,
                redundantElse.ElseKeyword.GetLocation(),
                codeStyleOption.Notification.Severity,
                additionalLocations: ImmutableArray.Create(ifStatement.GetLocation(), redundantElse.GetLocation()),
                properties: null));
        }

        private static ElseClauseSyntax? FindRedundantElse(IfStatementSyntax ifStatement)
        {
            var elseClause = ifStatement.Else;

            while (elseClause is not null)
            {
                //var endsWithJump = ifStatement.Statement switch
                //{
                //    BlockSyntax block => IsJumpStatement(block.Statements.LastOrDefault()),
                //    _ => IsJumpStatement(ifStatement.Statement),
                //};

                //// doing this only makes sense when every if ends with a jump
                //if (!endsWithJump)
                //{
                //    return null;
                //}

                if (!AllCodePathsEndWithJump(ifStatement.Statement))
                {
                    return null;
                }

                // reached else not followed by an if
                if (elseClause.Statement is not IfStatementSyntax elseIfStatement)
                {
                    break;
                }

                ifStatement = elseIfStatement;
                elseClause = elseIfStatement.Else;
            }

            return elseClause;
        }

        private static bool AllCodePathsEndWithJump(StatementSyntax statement)
        {
            if (IsJumpStatement(statement))
            {
                return true;
            }
            else if (statement is IfStatementSyntax ifStatement)
            {
                var redundantElse = FindRedundantElse(ifStatement);
                return redundantElse is not null && AllCodePathsEndWithJump(redundantElse.Statement);
            }

            return statement switch
            {
                BlockSyntax block => AllCodePathsEndWithJump(block.Statements.LastOrDefault()),
                WhileStatementSyntax whileStatement => AllCodePathsEndWithJump(whileStatement.Statement),
                ForStatementSyntax forStatement => AllCodePathsEndWithJump(forStatement.Statement),
                CommonForEachStatementSyntax commonForEach => AllCodePathsEndWithJump(commonForEach.Statement),
                _ => false,
            };
        }

        private static bool IsJumpStatement(StatementSyntax? statement)
        {
            // 
            return statement is
                ReturnStatementSyntax or
                BreakStatementSyntax or
                ThrowStatementSyntax or
                ContinueStatementSyntax;
        }

        private static bool WillCauseVariableCollision(SemanticModel semanticModel, IfStatementSyntax ifStatement, ElseClauseSyntax elseClause, CancellationToken cancellationToken)
        {
            if (elseClause.Statement is not BlockSyntax elseBlock)
            {
                return false;
            }

            var outerScope = ifStatement?.Parent switch
            {
                BlockSyntax block => block,
                SwitchSectionSyntax switchSection => switchSection.Parent,
                GlobalStatementSyntax global => global.Parent,
                _ => throw new ArgumentException(nameof(ifStatement.Parent))
            };

            var existingSymbols = semanticModel
                .GetExistingSymbols(outerScope, cancellationToken)
                .ToLookup(s => s.Name);

            var operation = semanticModel.GetRequiredOperation(elseClause.Statement, cancellationToken);

            return operation is IBlockOperation blockOperation &&
                blockOperation.Locals.Any(local => existingSymbols[local.Name].Any(other => local != other));
        }
    }
}
