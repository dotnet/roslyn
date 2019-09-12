// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.UseSimpleUsingStatement
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
    internal class UseSimpleUsingStatementDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public UseSimpleUsingStatementDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseSimpleUsingStatementDiagnosticId,
                   CSharpCodeStyleOptions.PreferSimpleUsingStatement,
                   LanguageNames.CSharp,
                   new LocalizableResourceString(nameof(FeaturesResources.Use_simple_using_statement), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.using_statement_can_be_simplified), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.UsingStatement);

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var outermostUsing = (UsingStatementSyntax)context.Node;

            var syntaxTree = context.Node.SyntaxTree;
            var options = (CSharpParseOptions)syntaxTree.Options;
            if (options.LanguageVersion < LanguageVersion.CSharp8)
            {
                return;
            }

            if (!(outermostUsing.Parent is BlockSyntax parentBlock))
            {
                // Don't offer on a using statement that is parented by another using statement.
                // We'll just offer on the topmost using statement.
                return;
            }

            var innermostUsing = outermostUsing;

            // Check that all the immediately nested usings are convertible as well.  
            // We don't want take a sequence of nested-using and only convert some of them.
            for (var current = outermostUsing; current != null; current = current.Statement as UsingStatementSyntax)
            {
                innermostUsing = current;
                if (current.Declaration == null)
                {
                    return;
                }
            }

            // Verify that changing this using-statement into a using-declaration will not
            // change semantics.
            if (!PreservesSemantics(parentBlock, outermostUsing, innermostUsing))
            {
                return;
            }

            var cancellationToken = context.CancellationToken;

            // Converting a using-statement to a using-variable-declaration will cause the using's
            // variables to now be pushed up to the parent block's scope. This is also true for any
            // local variables in the innermost using's block. These may then collide with other
            // variables in the block, causing an error.  Check for that and bail if this happens.
            if (CausesVariableCollision(
                    context.SemanticModel, parentBlock,
                    outermostUsing, innermostUsing, cancellationToken))
            {
                return;
            }

            var optionSet = context.Options.GetAnalyzerOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var option = optionSet.GetOption(CSharpCodeStyleOptions.PreferSimpleUsingStatement);
            if (!option.Value)
            {
                return;
            }

            // Good to go!
            context.ReportDiagnostic(DiagnosticHelper.Create(
                Descriptor,
                outermostUsing.UsingKeyword.GetLocation(),
                option.Notification.Severity,
                additionalLocations: ImmutableArray.Create(outermostUsing.GetLocation()),
                properties: null));
        }

        private bool CausesVariableCollision(
            SemanticModel semanticModel, BlockSyntax parentBlock,
            UsingStatementSyntax outermostUsing, UsingStatementSyntax innermostUsing,
            CancellationToken cancellationToken)
        {
            var symbolNameToExistingSymbol = semanticModel.GetExistingSymbols(parentBlock, cancellationToken).ToLookup(s => s.Name);

            for (var current = outermostUsing; current != null; current = current.Statement as UsingStatementSyntax)
            {
                // Check if the using statement itself contains variables that will collide
                // with other variables in the block.
                var usingOperation = (IUsingOperation)semanticModel.GetOperation(current, cancellationToken);
                if (DeclaredLocalCausesCollision(symbolNameToExistingSymbol, usingOperation.Locals))
                {
                    return true;
                }
            }

            var innerUsingOperation = (IUsingOperation)semanticModel.GetOperation(innermostUsing, cancellationToken);
            if (innerUsingOperation.Body is IBlockOperation innerUsingBlock)
            {
                return DeclaredLocalCausesCollision(symbolNameToExistingSymbol, innerUsingBlock.Locals);
            }

            return false;
        }

        private static bool DeclaredLocalCausesCollision(ILookup<string, ISymbol> symbolNameToExistingSymbol, ImmutableArray<ILocalSymbol> locals)
            => locals.Any(local => symbolNameToExistingSymbol[local.Name].Any(otherLocal => !local.Equals(otherLocal)));

        private static bool PreservesSemantics(
            BlockSyntax parentBlock,
            UsingStatementSyntax outermostUsing,
            UsingStatementSyntax innermostUsing)
        {
            var statements = parentBlock.Statements;
            var index = statements.IndexOf(outermostUsing);

            return UsingValueDoesNotLeakToFollowingStatements(statements, index) &&
                   UsingStatementDoesNotInvolveJumps(statements, index, innermostUsing);
        }

        private static bool UsingStatementDoesNotInvolveJumps(
            SyntaxList<StatementSyntax> parentStatements, int index, UsingStatementSyntax innermostUsing)
        {
            // Jumps are not allowed to cross a using declaration in the forward direction, 
            // and can't go back unless there is a curly brace between the using and the label.
            // 
            // We conservatively implement this by disallowing the change if there are gotos/labels 
            // in the containing block, or inside the using body.  

            // Note: we only have to check up to the `using`, since the checks below in
            // UsingValueDoesNotLeakToFollowingStatements ensure that there would be no
            // labels/gotos *after* the using statement.
            for (var i = 0; i < index; i++)
            {
                var priorStatement = parentStatements[i];
                if (IsGotoOrLabeledStatement(priorStatement))
                {
                    return false;
                }
            }

            var innerStatements = innermostUsing.Statement is BlockSyntax block
                ? block.Statements
                : new SyntaxList<StatementSyntax>(innermostUsing.Statement);

            foreach (var statement in innerStatements)
            {
                if (IsGotoOrLabeledStatement(statement))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsGotoOrLabeledStatement(StatementSyntax priorStatement)
            => priorStatement.Kind() == SyntaxKind.GotoStatement ||
               priorStatement.Kind() == SyntaxKind.LabeledStatement;

        private static bool UsingValueDoesNotLeakToFollowingStatements(
            SyntaxList<StatementSyntax> statements, int index)
        {
            // Has to be one of the following forms:
            // 1. Using statement is the last statement in the parent.
            // 2. Using statement is not the last statement in parent, but is followed by 
            //    something that is unaffected by simplifying the using statement.  i.e.
            //    `return`/`break`/`continue`.  *Note*.  `return expr` would *not* be ok.
            //    In that case, `expr` would now be evaluated *before* the using disposed
            //    the resource, instead of afterwards.  Effectly, the statement following
            //    cannot actually execute any code that might depend on the .Dispose method
            //    being called or not.

            if (index == statements.Count - 1)
            {
                // very last statement in the block.  Can be converted.
                return true;
            }

            // Not the last statement, get the next statement and examine that.
            var nextStatement = statements[index + 1];
            if (nextStatement is BreakStatementSyntax ||
                nextStatement is ContinueStatementSyntax)
            {
                // using statement followed by break/continue.  Can convert this as executing 
                // the break/continue will cause the code to exit the using scope, causing 
                // Dispose to be called at the same place as before.
                return true;
            }

            if (nextStatement is ReturnStatementSyntax returnStatement &&
                returnStatement.Expression == null)
            {
                // using statement followed by `return`.  Can conver this as executing 
                // the `return` will cause the code to exit the using scope, causing 
                // Dispose to be called at the same place as before.
                //
                // Note: the expr has to be null.  If it was non-null, then the expr would
                // now execute before hte using called 'Dispose' instead of after, potentially
                // changing semantics.
                return true;
            }

            // Add any additional cases here in the future.
            return false;
        }
    }
}
