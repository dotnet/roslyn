// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    internal class RemoveRedundantElseStatementDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public RemoveRedundantElseStatementDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.RemoveRedundantElseStatementDiagnosticId,
                   EnforceOnBuildValues.RemoveRedundantElseStatement,
                   CSharpCodeStyleOptions.PreferRemoveRedundantElseStatement,
                   new LocalizableResourceString(nameof(CSharpAnalyzersResources.Remove_redundant_else_statement), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(context =>
            {
                context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.IfStatement);
            });
        }

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var option = context.GetCSharpAnalyzerOptions().PreferRemoveRedundantElseStatement;
            if (!option.Value)
                return;

            var ifStatement = (IfStatementSyntax)context.Node;

            if (ifStatement.Parent is ElseClauseSyntax)
            {
                // Only offer on top most if
                return;
            }

            var redundantElse = FindRedundantElse(ifStatement);
            if (redundantElse is not null)
            {
                context.ReportDiagnostic(DiagnosticHelper.Create(
                    Descriptor,
                    redundantElse.ElseKeyword.GetLocation(),
                    option.Notification.Severity,
                    additionalLocations: ImmutableArray.Create(redundantElse.GetLocation()),
                    properties: null));
            }
        }

        private static ElseClauseSyntax? FindRedundantElse(IfStatementSyntax ifStatement)
        {
            var @else = ifStatement.Else;

            while (@else is not null)
            {
                var endsWithJump = ifStatement.Statement switch
                {
                    BlockSyntax block => IsJumpStatement(block.Statements.LastOrDefault()),
                    _ => IsJumpStatement(ifStatement.Statement),
                };

                // doing this only makes sense when every if ends with a jump
                if (!endsWithJump)
                {
                    return null;
                }

                if (@else.Statement is IfStatementSyntax elseIfStatement)
                {
                    @else = elseIfStatement.Else;
                }

                // reached else not followed by an if
                break;
            }

            return @else;
        }
        private static bool IsJumpStatement(StatementSyntax? statement)
        {
            return statement is
                ReturnStatementSyntax or
                BreakStatementSyntax or
                ThrowStatementSyntax or
                ContinueStatementSyntax;
        }
    }
}
