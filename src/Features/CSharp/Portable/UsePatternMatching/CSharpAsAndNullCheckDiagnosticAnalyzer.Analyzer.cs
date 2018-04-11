// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            private readonly SyntaxNode _comparison;
            private readonly SyntaxNode _operand;
            private readonly SyntaxNode _localStatement;
            private readonly SyntaxNode _enclosingBlock;
            private readonly CancellationToken _cancellationToken;

            public Analyzer(
                SemanticModel semanticModel,
                ILocalSymbol localSymbol,
                SyntaxNode comparison,
                SyntaxNode operand,
                SyntaxNode localStatement,
                SyntaxNode enclosingBlock,
                CancellationToken cancellationToken)
            {
                _semanticModel = semanticModel;
                _comparison = comparison;
                _localSymbol = localSymbol;
                _operand = operand;
                _localStatement = localStatement;
                _enclosingBlock = enclosingBlock;
                _cancellationToken = cancellationToken;
            }

            public bool CanSafelyConvertToPatternMatching()
            {
                var defAssignedWhenTrue = _comparison.Kind() == SyntaxKind.NotEqualsExpression;
                foreach (var current in _comparison.Ancestors())
                {
                    switch (current.Kind())
                    {
                        case SyntaxKind.ParenthesizedExpression:
                        case SyntaxKind.CastExpression:
                            // Parentheses and cast expressions do not contribute to the flow analysis.
                            continue;

                        case SyntaxKind.LogicalNotExpression:
                            // The !-operator reverses the definitive assignment state.
                            defAssignedWhenTrue = !defAssignedWhenTrue;
                            continue;

                        case SyntaxKind.LogicalAndExpression:
                            if (!defAssignedWhenTrue)
                            {
                                return false;
                            }

                            continue;

                        case SyntaxKind.LogicalOrExpression:
                            if (defAssignedWhenTrue)
                            {
                                return false;
                            }

                            continue;

                        case SyntaxKind.ConditionalExpression:
                            var conditionalExpression = (ConditionalExpressionSyntax)current;
                            if (LocalFlowsIn(defAssignedWhenTrue
                                    ? conditionalExpression.WhenFalse
                                    : conditionalExpression.WhenTrue))
                            {
                                return false;
                            }

                            return CheckExpression(conditionalExpression);

                        case SyntaxKind.ForStatement:
                            var forStatement = (ForStatementSyntax)current;
                            if (!forStatement.Condition.Span.Contains(_comparison.Span))
                            {
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
                            return CheckExpression(expression);
                        case StatementSyntax statement:
                            return CheckStatement(statement);
                        default:
                            return CheckStatement(current.GetAncestor<StatementSyntax>());
                    }
                }

                return false;
            }

            private bool LocalFlowsIn(SyntaxNode statementOrExpression)
            {
                return statementOrExpression != null
                    && _semanticModel.AnalyzeDataFlow(statementOrExpression).DataFlowsIn.Contains(_localSymbol);
            }

            private bool CheckLoop(SyntaxNode node, StatementSyntax body, bool defAssignedWhenTrue)
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
                    // That's because in this case, unlike the original code, we're type-checking in every iteration
                    // so we do not replace a simple null check with the "is" operator if it's in a loop.
                    return false;
                }

                if (!defAssignedWhenTrue && LocalFlowsIn(body))
                {
                    return false;
                }

                return !IsAccessedOutOfScope(node);
            }

            private bool CheckExpression(ExpressionSyntax exprsesion)
            {
                // It wouldn't be safe to read after the pattern variable is
                // declared inside a sub-expression, because it would not be
                // definitely assigned after this point. It's possible to allow
                // use after assignment but it's rather unlikely to happen.
                return !IsAccessedOutOfScope(exprsesion);
            }

            private bool CheckStatement(StatementSyntax statement)
            {
                if (statement.Parent.IsKind(SyntaxKind.Block, out BlockSyntax block))
                {
                    if (IsAccessedOutOfScope(block))
                    {
                        return false;
                    }

                    var nextStatement = statement.GetNextStatement();
                    if (nextStatement != null)
                    {
                        var dataFlow = _semanticModel.AnalyzeDataFlow(nextStatement, block.Statements.Last());
                        return !dataFlow.DataFlowsIn.Contains(_localSymbol);
                    }

                    return true;
                }
                else
                {
                    return !IsAccessedOutOfScope(statement);
                }
            }

            private bool IsAccessedOutOfScope(SyntaxNode scope)
            {
                var localStatementStart = _localStatement.Span.Start;
                var variableName = _localSymbol.Name;

                foreach (var descendentNode in _enclosingBlock.DescendantNodes())
                {
                    var descendentStart = descendentNode.Span.Start;
                    if (descendentStart <= localStatementStart)
                    {
                        // We're not interested in nodes that are apeared before
                        // the local declaration statement. It's either an error
                        // or not the local reference we're looking for.
                        continue;
                    }

                    if (scope.Span.Contains(descendentNode.Span))
                    {
                        // If this is in the scope, we don't bother checking the symbol.
                        continue;
                    }

                    if (descendentNode.IsKind(SyntaxKind.IdentifierName, out IdentifierNameSyntax identifierName) &&
                        identifierName.Identifier.ValueText == variableName &&
                        _localSymbol.Equals(_semanticModel.GetSymbolInfo(identifierName, _cancellationToken).Symbol))
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
