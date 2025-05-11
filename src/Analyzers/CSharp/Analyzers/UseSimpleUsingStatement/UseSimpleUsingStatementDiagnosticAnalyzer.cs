﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseSimpleUsingStatement;

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
internal sealed class UseSimpleUsingStatementDiagnosticAnalyzer()
    : AbstractBuiltInCodeStyleDiagnosticAnalyzer(
        IDEDiagnosticIds.UseSimpleUsingStatementDiagnosticId,
        EnforceOnBuildValues.UseSimpleUsingStatement,
        CSharpCodeStyleOptions.PreferSimpleUsingStatement,
        new LocalizableResourceString(nameof(CSharpAnalyzersResources.Use_simple_using_statement), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
        new LocalizableResourceString(nameof(CSharpAnalyzersResources.using_statement_can_be_simplified), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
{
    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(context =>
        {
            if (context.Compilation.LanguageVersion() < LanguageVersion.CSharp8)
                return;

            context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.UsingStatement);
        });
    }

    private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
        var option = context.GetCSharpAnalyzerOptions().PreferSimpleUsingStatement;
        if (!option.Value || ShouldSkipAnalysis(context, option.Notification))
            return;

        var cancellationToken = context.CancellationToken;
        var outermostUsing = (UsingStatementSyntax)context.Node;
        var semanticModel = context.SemanticModel;

        var parentBlockLike = CSharpBlockFacts.Instance.GetImmediateParentExecutableBlockForStatement(outermostUsing);

        // Don't offer on a using statement that is parented by another using statement. We'll just offer on the topmost
        // using statement.  Also, this is only offered in a block and compilation unit.  Simple using statements are
        // not allowed within switch sections.
        if (parentBlockLike is not BlockSyntax and not CompilationUnitSyntax)
            return;

        var innermostUsing = outermostUsing;

        // Check that all the immediately nested usings are convertible as well.  
        // We don't want take a sequence of nested-using and only convert some of them.
        for (var current = outermostUsing; current != null; current = current.Statement as UsingStatementSyntax)
        {
            innermostUsing = current;
            if (current.Declaration == null)
                return;
        }

        // Verify that changing this using-statement into a using-declaration will not change semantics.
        if (!PreservesSemantics(semanticModel, parentBlockLike, outermostUsing, innermostUsing, cancellationToken))
            return;

        // Converting a using-statement to a using-variable-declaration will cause the using's variables to now be
        // pushed up to the parent block's scope. This is also true for any local variables in the innermost using's
        // block. These may then collide with other variables in the block, causing an error.  Check for that and
        // bail if this happens.
        if (CausesVariableCollision(
                context.SemanticModel, parentBlockLike,
                outermostUsing, innermostUsing, cancellationToken))
        {
            return;
        }

        // Good to go!
        context.ReportDiagnostic(DiagnosticHelper.Create(
            Descriptor,
            outermostUsing.UsingKeyword.GetLocation(),
            option.Notification,
            context.Options,
            additionalLocations: [outermostUsing.GetLocation()],
            properties: null));
    }

    private static bool CausesVariableCollision(
        SemanticModel semanticModel,
        SyntaxNode parentBlockLike,
        UsingStatementSyntax outermostUsing,
        UsingStatementSyntax innermostUsing,
        CancellationToken cancellationToken)
    {
        using var _ = PooledDictionary<string, ArrayBuilder<ISymbol>>.GetInstance(out var symbolNameToExistingSymbol);

        try
        {
            foreach (var statement in CSharpBlockFacts.Instance.GetExecutableBlockStatements(parentBlockLike))
            {
                foreach (var symbol in semanticModel.GetAllDeclaredSymbols(statement, cancellationToken))
                    symbolNameToExistingSymbol.MultiAdd(symbol.Name, symbol);
            }

            for (var current = outermostUsing; current != null; current = current.Statement as UsingStatementSyntax)
            {
                // Check if the using statement itself contains variables that will collide with other variables in the
                // block.
                var usingOperation = (IUsingOperation)semanticModel.GetRequiredOperation(current, cancellationToken);
                if (DeclaredLocalCausesCollision(symbolNameToExistingSymbol, usingOperation.Locals))
                    return true;
            }

            var innerUsingOperation = (IUsingOperation)semanticModel.GetRequiredOperation(innermostUsing, cancellationToken);
            if (innerUsingOperation.Body is IBlockOperation innerUsingBlock)
                return DeclaredLocalCausesCollision(symbolNameToExistingSymbol, innerUsingBlock.Locals);

            return false;
        }
        finally
        {
            symbolNameToExistingSymbol.FreeValues();
        }
    }

    private static bool DeclaredLocalCausesCollision(Dictionary<string, ArrayBuilder<ISymbol>> symbolNameToExistingSymbol, ImmutableArray<ILocalSymbol> locals)
        => locals.Any(static (local, symbolNameToExistingSymbol) =>
           symbolNameToExistingSymbol.TryGetValue(local.Name, out var symbols) &&
           symbols.Any(otherLocal => !local.Equals(otherLocal)), symbolNameToExistingSymbol);

    private static bool PreservesSemantics(
        SemanticModel semanticModel,
        SyntaxNode parentBlockLike,
        UsingStatementSyntax outermostUsing,
        UsingStatementSyntax innermostUsing,
        CancellationToken cancellationToken)
    {
        var statements = CSharpBlockFacts.Instance.GetExecutableBlockStatements(parentBlockLike);
        var index = statements.IndexOf(outermostUsing);

        return UsingValueDoesNotLeakToFollowingStatements(semanticModel, statements, index, cancellationToken) &&
               UsingStatementDoesNotInvolveJumps(statements, index, innermostUsing);
    }

    private static bool UsingStatementDoesNotInvolveJumps(
        IReadOnlyList<StatementSyntax> parentStatements, int index, UsingStatementSyntax innermostUsing)
    {
        // Jumps are not allowed to cross a using declaration in the forward direction, and can't go back unless
        // there is a curly brace between the using and the label.
        // 
        // We conservatively implement this by disallowing the change if there are gotos/labels 
        // in the containing block, or inside the using body.  

        // Note: we only have to check up to the `using`, since the checks below in
        // UsingValueDoesNotLeakToFollowingStatements ensure that there would be no labels/gotos *after* the using
        // statement.
        for (var i = 0; i < index; i++)
        {
            var priorStatement = parentStatements[i];
            if (IsGotoOrLabeledStatement(priorStatement))
                return false;
        }

        var innerStatements = innermostUsing.Statement is BlockSyntax block
            ? block.Statements
            : new SyntaxList<StatementSyntax>(innermostUsing.Statement);

        foreach (var statement in innerStatements)
        {
            if (IsGotoOrLabeledStatement(statement))
                return false;
        }

        return true;
    }

    private static bool IsGotoOrLabeledStatement(StatementSyntax priorStatement)
        => priorStatement.Kind() is SyntaxKind.GotoStatement or
           SyntaxKind.LabeledStatement;

    private static bool UsingValueDoesNotLeakToFollowingStatements(
        SemanticModel semanticModel,
        IReadOnlyList<StatementSyntax> statements,
        int index,
        CancellationToken cancellationToken)
    {
        // Has to be one of the following forms:
        // 1. Using statement is the last statement in the parent.
        // 2. Using statement is not the last statement in parent, but is followed by something that is unaffected
        //    by simplifying the using statement.  i.e. `return`/`break`/`continue`.  *Note*.  `return expr` would
        //    *not* be ok. In that case, `expr` would now be evaluated *before* the using disposed the resource,
        //    instead of afterwards.  Effectively, the statement following cannot actually execute any code that
        //    might depend on the .Dispose method being called or not.

        // Note: we can skip local functions as moving their scope inside the using doesn't change anything.
        while (index + 1 < statements.Count && statements[index + 1] is LocalFunctionStatementSyntax)
            index++;

        // if we got to the end of the the block then this can be converted.
        if (index == statements.Count - 1)
            return true;

        // Not the last statement, get the next statement and examine that.
        var nextStatement = statements[index + 1];
        if (nextStatement is BreakStatementSyntax or ContinueStatementSyntax)
        {
            // using statement followed by break/continue.  Can convert this as executing the break/continue will
            // cause the code to exit the using scope, causing Dispose to be called at the same place as before.
            return true;
        }

        if (nextStatement is ReturnStatementSyntax returnStatement)
        {
            // using statement followed by `return`.  Can convert this as executing the `return` will cause the code
            // to exit the using scope, causing Dispose to be called at the same place as before.
            //
            // Note: the expr has to be null.  If it was non-null, then the expr would now execute before hte using
            // called 'Dispose' instead of after, potentially changing semantics.
            if (returnStatement.Expression is null)
                return true;

            // return constant;
            //
            // This is also safe to return as constants could not be affected by being inside or outside the using block.
            var constantValue = semanticModel.GetConstantValue(returnStatement.Expression, cancellationToken);
            return constantValue.HasValue;
        }

        // Add any additional cases here in the future.
        return false;
    }
}
