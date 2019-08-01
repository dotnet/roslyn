// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.UsePatternMatching
{
    internal partial class CSharpAsAndNullCheckDiagnosticAnalyzer
    {
        private readonly struct Analyzer
        {
            private readonly SemanticModel _semanticModel;
            private readonly ILocalSymbol _localSymbol;
            private readonly ExpressionSyntax _comparison;
            private readonly ExpressionSyntax _operand;
            private readonly SyntaxNode _localStatement;
            private readonly SyntaxNode _enclosingBlock;
            private readonly CancellationToken _cancellationToken;

            private Analyzer(
                SemanticModel semanticModel,
                ILocalSymbol localSymbol,
                ExpressionSyntax comparison,
                ExpressionSyntax operand,
                SyntaxNode localStatement,
                SyntaxNode enclosingBlock,
                CancellationToken cancellationToken)
            {
                Debug.Assert(semanticModel != null);
                Debug.Assert(localSymbol != null);
                Debug.Assert(comparison != null);
                Debug.Assert(operand != null);
                Debug.Assert(localStatement.IsKind(SyntaxKind.LocalDeclarationStatement));
                Debug.Assert(enclosingBlock.IsKind(SyntaxKind.Block, SyntaxKind.SwitchSection));

                _semanticModel = semanticModel;
                _comparison = comparison;
                _localSymbol = localSymbol;
                _operand = operand;
                _localStatement = localStatement;
                _enclosingBlock = enclosingBlock;
                _cancellationToken = cancellationToken;
            }

            public static bool CanSafelyConvertToPatternMatching(
                SemanticModel semanticModel,
                ILocalSymbol localSymbol,
                ExpressionSyntax comparison,
                ExpressionSyntax operand,
                SyntaxNode localStatement,
                SyntaxNode enclosingBlock,
                CancellationToken cancellationToken)
            {
                var analyzer = new Analyzer(semanticModel, localSymbol, comparison, operand, localStatement, enclosingBlock, cancellationToken);
                return analyzer.CanSafelyConvertToPatternMatching();
            }

            // To convert a null-check to pattern-matching, we should make sure of a few things:
            //
            //      (1) The pattern variable may not be used before the point of declaration.
            //
            //          {
            //              var use = t;
            //              if (x is T t) {}
            //          }
            //
            //      (2) The pattern variable may not be used outside of the new scope which
            //          is determined by the parent statement.
            //
            //          {
            //              if (x is T t) {}
            //          }
            //
            //          var use = t;
            //
            //      (3) The pattern variable may not be used before assignment in opposite
            //          branches, if any.
            //
            //          {
            //              if (x is T t) {}
            //              var use = t;
            //          }
            //
            // We walk up the tree from the point of null-check and see if any of the above is violated.
            private bool CanSafelyConvertToPatternMatching()
            {
                // Keep track of whether the pattern variable is definitely assigned when false/true.
                // We start by the null-check itself, if it's compared with '==', the pattern variable
                // will be definitely assigned when false, because we wrap the is-operator in a !-operator.
                var defAssignedWhenTrue = _comparison.IsKind(SyntaxKind.NotEqualsExpression, SyntaxKind.IsExpression);

                foreach (var current in _comparison.Ancestors())
                {
                    // Checking for any conditional statement or expression that could possibly
                    // affect or determine the state of definite-assignment of the pattern variable.
                    switch (current.Kind())
                    {
                        case SyntaxKind.LogicalAndExpression when !defAssignedWhenTrue:
                        case SyntaxKind.LogicalOrExpression when defAssignedWhenTrue:
                            // Since the pattern variable is only definitely assigned if the pattern
                            // succeeded, in the following cases it would not be safe to use pattern-matching.
                            // For example:
                            //
                            //      if ((x = o as string) == null && SomeExpression)
                            //      if ((x = o as string) != null || SomeExpression)
                            //
                            // Here, x would never be definitely assigned if pattern-matching were used.
                            return false;

                        case SyntaxKind.LogicalAndExpression:
                        case SyntaxKind.LogicalOrExpression:

                        // Parentheses and cast expressions do not contribute to the flow analysis.
                        case SyntaxKind.ParenthesizedExpression:
                        case SyntaxKind.CastExpression:

                        // Skip over declaration parts to get to the parenting statement
                        // which might be a for-statement or a local declaration statement.
                        case SyntaxKind.EqualsValueClause:
                        case SyntaxKind.VariableDeclarator:
                        case SyntaxKind.VariableDeclaration:
                            continue;

                        case SyntaxKind.LogicalNotExpression:
                            // The !-operator negates the definitive assignment state.
                            defAssignedWhenTrue = !defAssignedWhenTrue;
                            continue;

                        case SyntaxKind.ConditionalExpression:
                            var conditionalExpression = (ConditionalExpressionSyntax)current;
                            if (LocalFlowsIn(defAssignedWhenTrue
                                    ? conditionalExpression.WhenFalse
                                    : conditionalExpression.WhenTrue))
                            {
                                // In a conditional expression, the pattern variable
                                // would not be definitely assigned in the opposite branch.
                                return false;
                            }

                            return CheckExpression(conditionalExpression);

                        case SyntaxKind.ForStatement:
                            var forStatement = (ForStatementSyntax)current;
                            if (!forStatement.Condition.Span.Contains(_comparison.Span))
                            {
                                // In a for-statement, only the condition expression
                                // can make this definitely assigned in the loop body.
                                return false;
                            }

                            return CheckLoop(forStatement, forStatement.Statement, defAssignedWhenTrue);

                        case SyntaxKind.WhileStatement:
                            var whileStatement = (WhileStatementSyntax)current;
                            return CheckLoop(whileStatement, whileStatement.Statement, defAssignedWhenTrue);

                        case SyntaxKind.IfStatement:
                            var ifStatement = (IfStatementSyntax)current;
                            var oppositeStatement = defAssignedWhenTrue
                                ? ifStatement.Else?.Statement
                                : ifStatement.Statement;

                            if (oppositeStatement != null)
                            {
                                var dataFlow = _semanticModel.AnalyzeDataFlow(oppositeStatement);
                                if (dataFlow.DataFlowsIn.Contains(_localSymbol))
                                {
                                    // Access before assignment is not safe in the opposite branch
                                    // as the variable is not definitely assgined at this point.
                                    // For example:
                                    //
                                    //    if (o is string x) { }
                                    //    else { Use(x); }
                                    //
                                    return false;
                                }

                                if (dataFlow.AlwaysAssigned.Contains(_localSymbol))
                                {
                                    // If the variable is always assigned here, we don't need to check
                                    // subsequent statements as it's definitely assigned afterwards.
                                    // For example:
                                    //
                                    //     if (o is string x) { }
                                    //     else { x = null; }
                                    //
                                    return true;
                                }
                            }

                            if (!defAssignedWhenTrue &&
                                !_semanticModel.AnalyzeControlFlow(ifStatement.Statement).EndPointIsReachable)
                            {
                                // Access before assignment here is only valid if we have a negative
                                // pattern-matching in an if-statement with an unreachable endpoint.
                                // For example:
                                //
                                //      if (!(o is string x)) {
                                //        return;
                                //      }
                                //
                                //      // The 'return' statement above ensures x is definitely assigned here
                                //      Console.WriteLine(x);
                                //
                                return true;
                            }

                            return CheckStatement(ifStatement);
                    }

                    switch (current)
                    {
                        case ExpressionSyntax expression:
                            // If we reached here, it means we have a sub-expression that
                            // does not garantee definite assignment. We should make sure that
                            // the pattern variable is not used outside of the expression boundaries.
                            return CheckExpression(expression);

                        case StatementSyntax statement:
                            // If we reached here, it means that the null-check is appeared in
                            // a statement. In that case, the variable would be actually in the
                            // scope in subsequent statements, but not definitely assigned.
                            // Therefore, we should ensure that there is no use before assignment.
                            return CheckStatement(statement);
                    }

                    // Bail out for error cases and unhandled cases.
                    break;
                }

                return false;
            }

            private bool CheckLoop(SyntaxNode statement, StatementSyntax body, bool defAssignedWhenTrue)
            {
                if (_operand.Kind() == SyntaxKind.IdentifierName)
                {
                    // We have something like:
                    //
                    //      var x = e as T;
                    //      while (b != null) { ... }
                    //
                    // It's not necessarily safe to convert this to:
                    //
                    //      while (x is T b) { ... }
                    //
                    // That's because in this case, unlike the original code, we're
                    // type-checking in every iteration, so we do not replace a
                    // simple null check with the "is" operator if it's in a loop.
                    return false;
                }

                if (!defAssignedWhenTrue && LocalFlowsIn(body))
                {
                    // If the local is accessed before assignment
                    // in the loop body, we should make sure that
                    // the variable is definitely assigned by then.
                    return false;
                }

                // The scope of the pattern variables for loops
                // does not leak out of the loop statement.
                return !IsAccessedOutOfScope(scope: statement);
            }

            private bool CheckExpression(ExpressionSyntax exprsesion)
            {
                // It wouldn't be safe to read after the pattern variable is
                // declared inside a sub-expression, because it would not be
                // definitely assigned after this point. It's possible to allow
                // use after assignment but it's rather unlikely to happen.
                return !IsAccessedOutOfScope(scope: exprsesion);
            }

            private bool CheckStatement(StatementSyntax statement)
            {
                Debug.Assert(statement != null);

                // This is either an embedded statement or parented by a block.
                // If we're parented by a block, then that block will be the scope
                // of the new variable. Otherwise the scope is the statement itself.
                if (statement.Parent.IsKind(SyntaxKind.Block, out BlockSyntax block))
                {
                    // Check if the local is accessed before assignment 
                    // in the subsequent statements. If so, this can't
                    // be converted to pattern-matching.
                    if (LocalFlowsIn(firstStatement: statement.GetNextStatement(),
                                     lastStatement: block.Statements.Last()))
                    {
                        return false;
                    }

                    return !IsAccessedOutOfScope(scope: block);
                }
                else
                {
                    return !IsAccessedOutOfScope(scope: statement);
                }
            }

            private bool IsAccessedOutOfScope(SyntaxNode scope)
            {
                Debug.Assert(scope != null);

                var localStatementStart = _localStatement.SpanStart;
                var comparisonSpanStart = _comparison.SpanStart;
                var variableName = _localSymbol.Name;
                var scopeSpan = scope.Span;

                // Iterate over all descendent nodes to find possible out-of-scope references.
                foreach (var descendentNode in _enclosingBlock.DescendantNodes())
                {
                    var descendentNodeSpanStart = descendentNode.SpanStart;
                    if (descendentNodeSpanStart <= localStatementStart)
                    {
                        // We're not interested in nodes that are apeared before
                        // the local declaration statement. It's either an error
                        // or not the local reference we're looking for.
                        continue;
                    }

                    if (descendentNodeSpanStart >= comparisonSpanStart && scopeSpan.Contains(descendentNode.Span))
                    {
                        // If this is in the scope and after null-check, we don't bother checking the symbol.
                        continue;
                    }

                    if (descendentNode.IsKind(SyntaxKind.IdentifierName, out IdentifierNameSyntax identifierName) &&
                        identifierName.Identifier.ValueText == variableName &&
                        _localSymbol.Equals(_semanticModel.GetSymbolInfo(identifierName, _cancellationToken).Symbol))
                    {
                        // If we got here, it means we have a local
                        // reference out of scope of the pattern variable.
                        return true;
                    }
                }

                // Either no reference were found, or all
                // references were inside the given scope.
                return false;
            }

            private bool LocalFlowsIn(SyntaxNode statementOrExpression)
            {
                if (statementOrExpression == null)
                {
                    return false;
                }

                if (statementOrExpression.ContainsDiagnostics)
                {
                    return false;
                }

                return _semanticModel.AnalyzeDataFlow(statementOrExpression).DataFlowsIn.Contains(_localSymbol);
            }

            private bool LocalFlowsIn(StatementSyntax firstStatement, StatementSyntax lastStatement)
            {
                if (firstStatement == null || lastStatement == null)
                {
                    return false;
                }

                if (firstStatement.ContainsDiagnostics || lastStatement.ContainsDiagnostics)
                {
                    return false;
                }

                return _semanticModel.AnalyzeDataFlow(firstStatement, lastStatement).DataFlowsIn.Contains(_localSymbol);
            }
        }
    }
}
