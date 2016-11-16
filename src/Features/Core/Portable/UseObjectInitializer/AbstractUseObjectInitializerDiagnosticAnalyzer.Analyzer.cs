// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using System.Linq;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.UseObjectInitializer
{
    internal partial class AbstractUseObjectInitializerDiagnosticAnalyzer<
        TSyntaxKind,
        TExpressionSyntax,
        TStatementSyntax,
        TObjectCreationExpressionSyntax,
        TMemberAccessExpressionSyntax,
        TAssignmentStatementSyntax,
        TVariableDeclaratorSyntax>
    {
        private struct Analyzer
        {
            private readonly ISyntaxFactsService _syntaxFacts;
            private readonly TObjectCreationExpressionSyntax _objectCreationExpression;

            private TStatementSyntax _containingStatement;
            private SyntaxNodeOrToken _valuePattern;

            public Analyzer(
                ISyntaxFactsService syntaxFacts,
                TObjectCreationExpressionSyntax objectCreationExpression) : this()
            {
                _syntaxFacts = syntaxFacts;
                _objectCreationExpression = objectCreationExpression;
            }

            internal AnalysisResult? Analyze()
            {
                if (_syntaxFacts.GetObjectCreationInitializer(_objectCreationExpression) != null)
                {
                    // Don't bother if this already has an initializer.
                    return null;
                }

                _containingStatement = _objectCreationExpression.FirstAncestorOrSelf<TStatementSyntax>();
                if (_containingStatement == null)
                {
                    return null;
                }

                ObjectInitializerKind kind;
                if (TryInitializeVariableDeclarationCase())
                {
                    kind = ObjectInitializerKind.VariableDeclaration;
                }
                else if (TryInitializeAssignmentCase())
                {
                    kind = ObjectInitializerKind.Assignment;
                }
                else
                {
                    return null;
                }

                var containingBlock = _containingStatement.Parent;
                var foundStatement = false;

                var matches = ArrayBuilder<Match>.GetInstance();
                HashSet<string> seenNames = null;

                var index = 0;
                foreach (var child in containingBlock.ChildNodesAndTokens())
                {
                    if (!foundStatement)
                    {
                        if (child == _containingStatement)
                        {
                            foundStatement = true;
                        }

                        index++;
                        continue;
                    }

                    if (child.IsToken)
                    {
                        break;
                    }

                    var statement = child.AsNode() as TAssignmentStatementSyntax;
                    if (statement == null)
                    {
                        break;
                    }

                    if (!_syntaxFacts.IsSimpleAssignmentStatement(statement))
                    {
                        break;
                    }

                    _syntaxFacts.GetPartsOfAssignmentStatement(
                        statement, out var left, out var right);

                    var rightExpression = right as TExpressionSyntax;
                    var leftMemberAccess = left as TMemberAccessExpressionSyntax;

                    if (!_syntaxFacts.IsSimpleMemberAccessExpression(leftMemberAccess))
                    {
                        break;
                    }

                    var expression = (TExpressionSyntax)_syntaxFacts.GetExpressionOfMemberAccessExpression(leftMemberAccess);
                    if (!ValuePatternMatches(expression))
                    {
                        break;
                    }

                    // Don't offer this fix if the value we're initializing is itself referenced
                    // on the RHS of the assignment.  For example:
                    //
                    //      var v = new X();
                    //      v.Prop = v.Prop.WithSomething();
                    //
                    // Or with
                    //
                    //      v = new X();
                    //      v.Prop = v.Prop.WithSomething();
                    //
                    // In the first case, 'v' is being initialized, and so will not be available 
                    // in the object initializer we create.
                    // 
                    // In the second case we'd change semantics because we'd access the old value 
                    // before the new value got written.
                    if (ExpressionContainsValuePattern(rightExpression))
                    {
                        break;
                    }

                    // If we have code like "x.v = .Length.ToString()"
                    // then we don't want to change this into:
                    //
                    //      var x = new Whatever() With { .v = .Length.ToString() }
                    //
                    // The problem here is that .Length will change it's meaning to now refer to the 
                    // object that we're creating in our object-creation expression.
                    if (ImplicitMemberAccessWouldBeAffected(rightExpression))
                    {
                        break;
                    }

                    // found a match!
                    seenNames = seenNames ?? new HashSet<string>();

                    // If we see an assignment to the same property/field, we can't convert it
                    // to an initializer.
                    var name = _syntaxFacts.GetNameOfMemberAccessExpression(leftMemberAccess);
                    var identifier = _syntaxFacts.GetIdentifierOfSimpleName(name);
                    if (!seenNames.Add(identifier.ValueText))
                    {
                        break;
                    }

                    matches.Add(new Match(statement, leftMemberAccess, rightExpression));
                    index++;
                }

                return new AnalysisResult(
                    containingBlock, index, kind,
                    matches.ToImmutableAndFree());
            }

            private bool ImplicitMemberAccessWouldBeAffected(SyntaxNode node)
            {
                if (node != null)
                {
                    foreach (var child in node.ChildNodesAndTokens())
                    {
                        if (child.IsNode)
                        {
                            if (ImplicitMemberAccessWouldBeAffected(child.AsNode()))
                            {
                                return true;
                            }
                        }
                    }

                    if (_syntaxFacts.IsSimpleMemberAccessExpression(node))
                    {
                        var expression = _syntaxFacts.GetExpressionOfMemberAccessExpression(
                            node, allowImplicitTarget: true);

                        // If we're implicitly referencing some target that is before the 
                        // object creation expression, then our semantics will change.
                        if (expression != null && expression.SpanStart < _objectCreationExpression.SpanStart)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            private bool ExpressionContainsValuePattern(TExpressionSyntax expression)
            {
                foreach (var subExpression in expression.DescendantNodesAndSelf().OfType<TExpressionSyntax>())
                {
                    if (!_syntaxFacts.IsNameOfMemberAccessExpression(subExpression))
                    {
                        if (ValuePatternMatches(subExpression))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            private bool ValuePatternMatches(TExpressionSyntax expression)
            {
                if (_valuePattern.IsToken)
                {
                    return _syntaxFacts.IsIdentifierName(expression) &&
                        _syntaxFacts.AreEquivalent(
                            _valuePattern.AsToken(),
                            _syntaxFacts.GetIdentifierOfSimpleName(expression));
                }
                else
                {
                    return _syntaxFacts.AreEquivalent(
                        _valuePattern.AsNode(), expression);
                }
            }

            private bool TryInitializeAssignmentCase()
            {
                if (!_syntaxFacts.IsSimpleAssignmentStatement(_containingStatement))
                {
                    return false;
                }

                _syntaxFacts.GetPartsOfAssignmentStatement(
                    _containingStatement, out var left, out var right);
                if (right != _objectCreationExpression)
                {
                    return false;
                }

                _valuePattern = left;
                return true;
            }

            private bool TryInitializeVariableDeclarationCase()
            {
                if (!_syntaxFacts.IsLocalDeclarationStatement(_containingStatement))
                {
                    return false;
                }

                var containingDeclarator = _objectCreationExpression.FirstAncestorOrSelf<TVariableDeclaratorSyntax>();
                if (containingDeclarator == null)
                {
                    return false;
                }

                if (!_syntaxFacts.IsDeclaratorOfLocalDeclarationStatement(containingDeclarator, _containingStatement))
                {
                    return false;
                }

                _valuePattern = _syntaxFacts.GetIdentifierOfVariableDeclarator(containingDeclarator);
                return true;
            }
        }

        internal struct Match
        {
            public readonly TAssignmentStatementSyntax Statement;
            public readonly TMemberAccessExpressionSyntax MemberAccessExpression;
            public readonly TExpressionSyntax Initializer;

            public Match(
                TAssignmentStatementSyntax statement,
                TMemberAccessExpressionSyntax memberAccessExpression,
                TExpressionSyntax initializer)
            {
                Statement = statement;
                MemberAccessExpression = memberAccessExpression;
                Initializer = initializer;
            }
        }

        internal enum ObjectInitializerKind
        {
            // var v = new O() ...
            VariableDeclaration,

            // v = new O() ...
            Assignment,
        }

        internal struct AnalysisResult
        {
            public readonly SyntaxNode BlockNode;
            public readonly int ContainingStatementIndex;
            public readonly ObjectInitializerKind Kind;
            public readonly ImmutableArray<Match> Matches;

            public AnalysisResult(SyntaxNode blockNode, int containingStatementIndex, ObjectInitializerKind kind, ImmutableArray<Match> matches)
            {
                BlockNode = blockNode;
                ContainingStatementIndex = containingStatementIndex;
                Kind = kind;
                Matches = matches;
            }
        }
    }
}