// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Debugging
{
    internal partial class CSharpProximityExpressionsService
    {
        internal class Worker(SyntaxTree syntaxTree, int position)
        {
            private readonly SyntaxTree _syntaxTree = syntaxTree;
            private readonly int _position = position;

            private StatementSyntax _parentStatement;
            private SyntaxToken _token;
            private readonly List<string> _expressions = new List<string>();

            internal IList<string> Do(CancellationToken cancellationToken)
            {
                // First, find the containing statement.  We'll want to add the expressions in this
                // statement to the result.
                _token = _syntaxTree.GetRoot(cancellationToken).FindToken(_position);
                _parentStatement = _token.GetAncestor<StatementSyntax>();
                if (_parentStatement == null)
                {
                    return null;
                }

                AddRelevantExpressions(_parentStatement, _expressions, includeDeclarations: false);
                AddPrecedingRelevantExpressions();
                AddFollowingRelevantExpressions(cancellationToken);
                AddCurrentDeclaration();
                AddMethodParameters();
                AddIndexerParameters();
                AddCatchParameters();
                AddThisExpression();
                AddValueExpression();

                var result = _expressions.Distinct().Where(e => e.Length > 0).ToList();
                return result.Count == 0 ? null : result;
            }

            private void AddValueExpression()
            {
                // If we're in a setter/adder/remover then add "value".
                if (_parentStatement.GetAncestorOrThis<AccessorDeclarationSyntax>() is (kind:
                        SyntaxKind.SetAccessorDeclaration or
                        SyntaxKind.InitAccessorDeclaration or
                        SyntaxKind.AddAccessorDeclaration or
                        SyntaxKind.RemoveAccessorDeclaration))
                {
                    _expressions.Add("value");
                }
            }

            private void AddThisExpression()
            {
                // If it's an instance member, then also add "this".
                var memberDeclaration = _parentStatement.GetAncestorOrThis<MemberDeclarationSyntax>();
                if (!memberDeclaration.IsKind(SyntaxKind.GlobalStatement) && !memberDeclaration.GetModifiers().Any(SyntaxKind.StaticKeyword))
                {
                    _expressions.Add("this");
                }
            }

            private void AddCatchParameters()
            {
                var block = GetImmediatelyContainingBlock();

                // if we're the start of a "catch(Goo e)" clause, then add "e".
                if (block != null && block?.Parent is CatchClauseSyntax catchClause &&
                    catchClause.Declaration != null && catchClause.Declaration.Identifier.Kind() != SyntaxKind.None)
                {
                    _expressions.Add(catchClause.Declaration.Identifier.ValueText);
                }
            }

            private BlockSyntax GetImmediatelyContainingBlock()
            {
                return IsFirstBlockStatement()
                    ? (BlockSyntax)_parentStatement.Parent
                    : _parentStatement is BlockSyntax block && block.OpenBraceToken == _token
                        ? (BlockSyntax)_parentStatement
                        : null;
            }

            private bool IsFirstBlockStatement()
                => _parentStatement.Parent is BlockSyntax parentBlockOpt && parentBlockOpt.Statements.FirstOrDefault() == _parentStatement;

            private void AddCurrentDeclaration()
            {
                if (_parentStatement is LocalDeclarationStatementSyntax)
                {
                    AddRelevantExpressions(_parentStatement, _expressions, includeDeclarations: true);
                }
            }

            private void AddMethodParameters()
            {
                // and we're the start of a method, then also add the parameters of that method to
                // the proximity expressions.
                var block = GetImmediatelyContainingBlock();

                if (block != null && block.Parent is MemberDeclarationSyntax memberDeclaration)
                {
                    var parameterList = memberDeclaration.GetParameterList();
                    AddParameters(parameterList);
                }
                else if (block is null
                    && _parentStatement.Parent is GlobalStatementSyntax { Parent: CompilationUnitSyntax compilationUnit } globalStatement
                    && compilationUnit.Members.FirstOrDefault() == globalStatement)
                {
                    _expressions.Add("args");
                }
            }

            private void AddIndexerParameters()
            {
                var block = GetImmediatelyContainingBlock();

                // and we're the start of a method, then also add the parameters of that method to
                // the proximity expressions.
                if (block != null &&
                    block.Parent is AccessorDeclarationSyntax &&
                    block.Parent.Parent is AccessorListSyntax &&
                    block.Parent.Parent.Parent is IndexerDeclarationSyntax indexerDeclaration)
                {
                    var parameterList = indexerDeclaration.ParameterList;
                    AddParameters(parameterList);
                }
            }

            private void AddParameters(BaseParameterListSyntax parameterList)
            {
                if (parameterList != null)
                {
                    _expressions.AddRange(
                        from p in parameterList.Parameters
                        select p.Identifier.ValueText);
                }
            }

            private void AddFollowingRelevantExpressions(CancellationToken cancellationToken)
            {
                var line = _syntaxTree.GetText(cancellationToken).Lines.IndexOf(_position);

                // If there's are more statements following us on the same line, then add them as
                // well. 
                for (var nextStatement = _parentStatement.GetNextStatement();
                     nextStatement != null && _syntaxTree.GetText(cancellationToken).Lines.IndexOf(nextStatement.SpanStart) == line;
                     nextStatement = nextStatement.GetNextStatement())
                {
                    AddRelevantExpressions(nextStatement, _expressions, includeDeclarations: false);
                }
            }

            private void AddPrecedingRelevantExpressions()
            {
                // If we're not the first statement in this block, 
                // and there's an expression or declaration statement directly above us,
                // then add the expressions from that as well.

                StatementSyntax previousStatement;

                if (_parentStatement is BlockSyntax block &&
                    block.CloseBraceToken == _token)
                {
                    // If we're at the last brace of a block, use the last
                    // statement in the block.
                    previousStatement = block.Statements.LastOrDefault();
                }
                else
                {
                    previousStatement = _parentStatement.GetPreviousStatement();
                }

                if (previousStatement != null)
                {
                    switch (previousStatement.Kind())
                    {
                        case SyntaxKind.ExpressionStatement:
                        case SyntaxKind.LocalDeclarationStatement:
                            AddRelevantExpressions(previousStatement, _expressions, includeDeclarations: true);
                            break;
                        case SyntaxKind.DoStatement:
                            AddExpressionTerms((previousStatement as DoStatementSyntax).Condition, _expressions);
                            AddLastStatementOfConstruct(previousStatement);
                            break;
                        case SyntaxKind.ForStatement:
                        case SyntaxKind.ForEachStatement:
                        case SyntaxKind.ForEachVariableStatement:
                        case SyntaxKind.IfStatement:
                        case SyntaxKind.CheckedStatement:
                        case SyntaxKind.UncheckedStatement:
                        case SyntaxKind.WhileStatement:
                        case SyntaxKind.LockStatement:
                        case SyntaxKind.SwitchStatement:
                        case SyntaxKind.TryStatement:
                        case SyntaxKind.UsingStatement:
                            AddRelevantExpressions(previousStatement, _expressions, includeDeclarations: false);
                            AddLastStatementOfConstruct(previousStatement);
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    // This is the first statement of the block. Go to the nearest enclosing statement and add its expressions
                    var statementAncestor = _parentStatement.Ancestors().OfType<StatementSyntax>().FirstOrDefault(node => !node.IsKind(SyntaxKind.Block));
                    if (statementAncestor != null)
                    {
                        AddRelevantExpressions(statementAncestor, _expressions, includeDeclarations: true);
                    }
                }
            }

            private void AddLastStatementOfConstruct(StatementSyntax statement)
            {
                if (statement == null)
                {
                    return;
                }

                switch (statement.Kind())
                {
                    case SyntaxKind.Block:
                        AddLastStatementOfConstruct((statement as BlockSyntax).Statements.LastOrDefault());
                        break;
                    case SyntaxKind.BreakStatement:
                    case SyntaxKind.ContinueStatement:
                        AddLastStatementOfConstruct(statement.GetPreviousStatement());
                        break;
                    case SyntaxKind.CheckedStatement:
                    case SyntaxKind.UncheckedStatement:
                        AddLastStatementOfConstruct((statement as CheckedStatementSyntax).Block);
                        break;
                    case SyntaxKind.DoStatement:
                        AddLastStatementOfConstruct((statement as DoStatementSyntax).Statement);
                        break;
                    case SyntaxKind.ForStatement:
                        AddLastStatementOfConstruct((statement as ForStatementSyntax).Statement);
                        break;
                    case SyntaxKind.ForEachStatement:
                    case SyntaxKind.ForEachVariableStatement:
                        AddLastStatementOfConstruct((statement as CommonForEachStatementSyntax).Statement);
                        break;
                    case SyntaxKind.IfStatement:
                        var ifStatement = statement as IfStatementSyntax;
                        AddLastStatementOfConstruct(ifStatement.Statement);
                        if (ifStatement.Else != null)
                        {
                            AddLastStatementOfConstruct(ifStatement.Else.Statement);
                        }

                        break;
                    case SyntaxKind.LockStatement:
                        AddLastStatementOfConstruct((statement as LockStatementSyntax).Statement);
                        break;
                    case SyntaxKind.SwitchStatement:
                        var switchStatement = statement as SwitchStatementSyntax;
                        foreach (var section in switchStatement.Sections)
                        {
                            AddLastStatementOfConstruct(section.Statements.LastOrDefault());
                        }

                        break;
                    case SyntaxKind.TryStatement:
                        var tryStatement = statement as TryStatementSyntax;
                        if (tryStatement.Finally != null)
                        {
                            AddLastStatementOfConstruct(tryStatement.Finally.Block);
                        }
                        else
                        {
                            AddLastStatementOfConstruct(tryStatement.Block);
                            foreach (var catchClause in tryStatement.Catches)
                            {
                                AddLastStatementOfConstruct(catchClause.Block);
                            }
                        }

                        break;
                    case SyntaxKind.UsingStatement:
                        AddLastStatementOfConstruct((statement as UsingStatementSyntax).Statement);
                        break;
                    case SyntaxKind.WhileStatement:
                        AddLastStatementOfConstruct((statement as WhileStatementSyntax).Statement);
                        break;
                    default:
                        AddRelevantExpressions(statement, _expressions, includeDeclarations: false);
                        break;
                }
            }
        }
    }
}
