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
    ///     using (var a = b)
    ///     using (var c = d)
    ///     using (var e = f)
    ///     {
    ///     }
    ///     ```
    /// 
    /// And offers to convert it to:
    ///
    ///     ```c#
    ///     using var a = b;
    ///     using var c = d;
    ///     using var e = f;
    ///     ```
    ///
    /// (this of course works in the case where there is only one using).
    /// 
    /// A few design decisions:
    ///     
    /// 1. We only offer this if the entire group of usings in a nested stack can be
    ///    converted.  We don't want to take a nice uniform group and break it into
    ///    a combination of using-statements and using-declarations.  That may feel 
    ///    less pleasant to the user than just staying uniform.
    /// 
    /// 2. We're conservative about converting.  Because `using`s may be critical for
    ///    program correctness, we only convert when we're absolutely *certain* that
    ///    semantics will not change.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class RemoveRedundantElseStatementDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public RemoveRedundantElseStatementDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseSimpleUsingStatementDiagnosticId,
                   EnforceOnBuildValues.UseSimpleUsingStatement,
                   CSharpCodeStyleOptions.PreferSimpleUsingStatement,
                   new LocalizableResourceString(nameof(CSharpAnalyzersResources.Use_simple_using_statement), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
                   new LocalizableResourceString(nameof(CSharpAnalyzersResources.using_statement_can_be_simplified), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
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
            var outerMostIf = (IfStatementSyntax)context.Node;
            var parent = outerMostIf.Parent;

            if (parent is not BlockSyntax block)
            {
                // Don't offer on if statement that is part of an else clause
                return;
            }

            var option = context.GetCSharpAnalyzerOptions().PreferSimpleUsingStatement;
            if (!option.Value)
                return;

            var @else = LastElseClause(outerMostIf);
            if (@else != null)
            {
                context.ReportDiagnostic(DiagnosticHelper.Create(
                    Descriptor,
                    @else.ElseKeyword.GetLocation(),
                    option.Notification.Severity,
                    additionalLocations: ImmutableArray.Create(@else.GetLocation()),
                    properties: null));
            }
        }

        private ElseClauseSyntax? LastElseClause(IfStatementSyntax ifStatement)
        {
            var @else = ifStatement.Else;

            var endsWithReturn = EndsWithReturn(ifStatement.Statement);

            if (!endsWithReturn || @else is null)
            {
                return null;
            }

            if (@else.Statement is IfStatementSyntax elseIfStatement)
            {
                return LastElseClause(elseIfStatement);
            }

            return @else;
        }
        
        private bool EndsWithReturn(StatementSyntax? statement)
        {
            return statement switch
            {
                BlockSyntax block => block.Statements.LastOrDefault() is ReturnStatementSyntax,
                ReturnStatementSyntax => true,
                _ => false,
            };
        }
    }
}
