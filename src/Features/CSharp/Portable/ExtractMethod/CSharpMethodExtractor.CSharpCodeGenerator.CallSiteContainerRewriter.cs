// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.ExtractMethod;

internal sealed partial class CSharpExtractMethodService
{
    internal sealed partial class CSharpMethodExtractor
    {
        private abstract partial class CSharpCodeGenerator
        {
            private sealed class CallSiteContainerRewriter : CSharpSyntaxRewriter
            {
                private readonly SyntaxNode _outmostCallSiteContainer;
                private readonly HashSet<SyntaxAnnotation> _variableToRemoveMap;
                private readonly SyntaxNode _firstStatementOrFieldToReplace;
                private readonly SyntaxNode _lastStatementOrFieldToReplace;
                private readonly ImmutableArray<SyntaxNode> _statementsOrMemberOrAccessorToInsert;

                public CallSiteContainerRewriter(
                    SyntaxNode outmostCallSiteContainer,
                    HashSet<SyntaxAnnotation> variableToRemoveMap,
                    SyntaxNode firstStatementOrFieldToReplace,
                    SyntaxNode lastStatementOrFieldToReplace,
                    ImmutableArray<SyntaxNode> statementsOrFieldToInsert)
                {
                    Contract.ThrowIfNull(outmostCallSiteContainer);
                    Contract.ThrowIfNull(variableToRemoveMap);
                    Contract.ThrowIfNull(firstStatementOrFieldToReplace);
                    Contract.ThrowIfNull(lastStatementOrFieldToReplace);
                    Contract.ThrowIfTrue(statementsOrFieldToInsert.IsDefaultOrEmpty);

                    _outmostCallSiteContainer = outmostCallSiteContainer;

                    _variableToRemoveMap = variableToRemoveMap;
                    _firstStatementOrFieldToReplace = firstStatementOrFieldToReplace;
                    _lastStatementOrFieldToReplace = lastStatementOrFieldToReplace;
                    _statementsOrMemberOrAccessorToInsert = statementsOrFieldToInsert;
                }

                public SyntaxNode Generate()
                    => Visit(_outmostCallSiteContainer);

                private SyntaxNode ContainerOfStatementsOrFieldToReplace => _firstStatementOrFieldToReplace.Parent;

                public override SyntaxNode VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
                {
                    node = (LocalDeclarationStatementSyntax)base.VisitLocalDeclarationStatement(node);
                    var list = new List<VariableDeclaratorSyntax>();
                    var triviaList = new List<SyntaxTrivia>();
                    // go through each var decls in decl statement
                    foreach (var variable in node.Declaration.Variables)
                    {
                        if (_variableToRemoveMap.HasSyntaxAnnotation(variable))
                        {
                            // if it had initialization, it shouldn't reach here.
                            Contract.ThrowIfFalse(variable.Initializer == null);

                            // we don't remove trivia around tokens we remove
                            triviaList.AddRange(variable.GetLeadingTrivia());
                            triviaList.AddRange(variable.GetTrailingTrivia());
                            continue;
                        }

                        if (triviaList.Count > 0)
                        {
                            list.Add(variable.WithPrependedLeadingTrivia(triviaList));
                            triviaList.Clear();
                            continue;
                        }

                        list.Add(variable);
                    }

                    if (list.Count == 0)
                    {
                        // nothing has survived. remove this from the list
                        if (triviaList.Count == 0)
                        {
                            return null;
                        }

                        // well, there are trivia associated with the node.
                        // we can't just delete the node since then, we will lose
                        // the trivia. unfortunately, it is not easy to attach the trivia
                        // to next token. for now, create an empty statement and associate the
                        // trivia to the statement

                        // TODO : think about a way to move the trivia to next token.
                        return SyntaxFactory.EmptyStatement(SyntaxFactory.Token([.. triviaList], SyntaxKind.SemicolonToken, [SyntaxFactory.ElasticMarker]));
                    }

                    if (list.Count == node.Declaration.Variables.Count)
                    {
                        // nothing has changed, return as it is
                        return node;
                    }

                    // TODO : fix how it manipulate trivia later

                    // if there is left over syntax trivia, it will be attached to leading trivia
                    // of semicolon
                    return
                        SyntaxFactory.LocalDeclarationStatement(
                            node.Modifiers,
                            SyntaxFactory.VariableDeclaration(
                                node.Declaration.Type,
                                [.. list]),
                            node.SemicolonToken.WithPrependedLeadingTrivia(triviaList));
                }

                // for every kind of extract methods
                public override SyntaxNode VisitBlock(BlockSyntax node)
                {
                    if (node != ContainerOfStatementsOrFieldToReplace)
                    {
                        // make sure we visit nodes under the block
                        return base.VisitBlock(node);
                    }

                    return node.WithStatements([.. VisitList(ReplaceStatements(node.Statements))]);
                }

                public override SyntaxNode VisitSwitchSection(SwitchSectionSyntax node)
                {
                    if (node != ContainerOfStatementsOrFieldToReplace)
                    {
                        // make sure we visit nodes under the switch section
                        return base.VisitSwitchSection(node);
                    }

                    return node.WithStatements([.. VisitList(ReplaceStatements(node.Statements))]);
                }

                // only for single statement or expression
                public override SyntaxNode VisitLabeledStatement(LabeledStatementSyntax node)
                {
                    if (node != ContainerOfStatementsOrFieldToReplace)
                    {
                        return base.VisitLabeledStatement(node);
                    }

                    return node.WithStatement(ReplaceStatementIfNeeded(node.Statement));
                }

                public override SyntaxNode VisitElseClause(ElseClauseSyntax node)
                {
                    if (node != ContainerOfStatementsOrFieldToReplace)
                    {
                        return base.VisitElseClause(node);
                    }

                    return node.WithStatement(ReplaceStatementIfNeeded(node.Statement));
                }

                public override SyntaxNode VisitIfStatement(IfStatementSyntax node)
                {
                    if (node != ContainerOfStatementsOrFieldToReplace)
                    {
                        return base.VisitIfStatement(node);
                    }

                    return node.WithCondition(VisitNode(node.Condition))
                               .WithStatement(ReplaceStatementIfNeeded(node.Statement))
                               .WithElse(VisitNode(node.Else));
                }

                public override SyntaxNode VisitLockStatement(LockStatementSyntax node)
                {
                    if (node != ContainerOfStatementsOrFieldToReplace)
                    {
                        return base.VisitLockStatement(node);
                    }

                    return node.WithExpression(VisitNode(node.Expression))
                               .WithStatement(ReplaceStatementIfNeeded(node.Statement));
                }

                public override SyntaxNode VisitFixedStatement(FixedStatementSyntax node)
                {
                    if (node != ContainerOfStatementsOrFieldToReplace)
                    {
                        return base.VisitFixedStatement(node);
                    }

                    return node.WithDeclaration(VisitNode(node.Declaration))
                               .WithStatement(ReplaceStatementIfNeeded(node.Statement));
                }

                public override SyntaxNode VisitUsingStatement(UsingStatementSyntax node)
                {
                    if (node != ContainerOfStatementsOrFieldToReplace)
                    {
                        return base.VisitUsingStatement(node);
                    }

                    return node.WithDeclaration(VisitNode(node.Declaration))
                               .WithExpression(VisitNode(node.Expression))
                               .WithStatement(ReplaceStatementIfNeeded(node.Statement));
                }

                public override SyntaxNode VisitForEachStatement(ForEachStatementSyntax node)
                {
                    if (node != ContainerOfStatementsOrFieldToReplace)
                    {
                        return base.VisitForEachStatement(node);
                    }

                    return node.WithExpression(VisitNode(node.Expression))
                               .WithStatement(ReplaceStatementIfNeeded(node.Statement));
                }

                public override SyntaxNode VisitForEachVariableStatement(ForEachVariableStatementSyntax node)
                {
                    if (node != ContainerOfStatementsOrFieldToReplace)
                    {
                        return base.VisitForEachVariableStatement(node);
                    }

                    return node.WithExpression(VisitNode(node.Expression))
                               .WithStatement(ReplaceStatementIfNeeded(node.Statement));
                }

                public override SyntaxNode VisitForStatement(ForStatementSyntax node)
                {
                    if (node != ContainerOfStatementsOrFieldToReplace)
                    {
                        return base.VisitForStatement(node);
                    }

                    return node.WithDeclaration(VisitNode(node.Declaration))
                               .WithInitializers(VisitList(node.Initializers))
                               .WithCondition(VisitNode(node.Condition))
                               .WithIncrementors(VisitList(node.Incrementors))
                               .WithStatement(ReplaceStatementIfNeeded(node.Statement));
                }

                public override SyntaxNode VisitDoStatement(DoStatementSyntax node)
                {
                    if (node != ContainerOfStatementsOrFieldToReplace)
                    {
                        return base.VisitDoStatement(node);
                    }

                    return node.WithStatement(ReplaceStatementIfNeeded(node.Statement))
                               .WithCondition(VisitNode(node.Condition));
                }

                public override SyntaxNode VisitWhileStatement(WhileStatementSyntax node)
                {
                    if (node != ContainerOfStatementsOrFieldToReplace)
                    {
                        return base.VisitWhileStatement(node);
                    }

                    return node.WithCondition(VisitNode(node.Condition))
                               .WithStatement(ReplaceStatementIfNeeded(node.Statement));
                }

                private TNode VisitNode<TNode>(TNode node) where TNode : SyntaxNode
                    => (TNode)Visit(node);

                private StatementSyntax ReplaceStatementIfNeeded(StatementSyntax statement)
                {
                    Contract.ThrowIfNull(statement);

                    // if all three same
                    if ((statement != _firstStatementOrFieldToReplace) || (_firstStatementOrFieldToReplace != _lastStatementOrFieldToReplace))
                        return statement;

                    // replace one statement with another
                    if (_statementsOrMemberOrAccessorToInsert.Length == 1)
                        return _statementsOrMemberOrAccessorToInsert.Cast<StatementSyntax>().Single();

                    // replace one statement with multiple statements (see bug # 6310)
                    var statements = _statementsOrMemberOrAccessorToInsert.CastArray<StatementSyntax>();
                    return SyntaxFactory.Block(statements);
                }

                private SyntaxList<TSyntax> ReplaceList<TSyntax>(SyntaxList<TSyntax> list)
                    where TSyntax : SyntaxNode
                {
                    // okay, this visit contains the statement
                    var newList = new List<TSyntax>(list);

                    var firstIndex = newList.FindIndex(s => s == _firstStatementOrFieldToReplace);
                    Contract.ThrowIfFalse(firstIndex >= 0);

                    var lastIndex = newList.FindIndex(s => s == _lastStatementOrFieldToReplace);
                    Contract.ThrowIfFalse(lastIndex >= 0);

                    Contract.ThrowIfFalse(firstIndex <= lastIndex);

                    // remove statement that must be removed
                    newList.RemoveRange(firstIndex, lastIndex - firstIndex + 1);

                    // add new statements to replace
                    newList.InsertRange(firstIndex, _statementsOrMemberOrAccessorToInsert.Cast<TSyntax>());

                    return [.. newList];
                }

                private SyntaxList<StatementSyntax> ReplaceStatements(SyntaxList<StatementSyntax> statements)
                    => ReplaceList(statements);

                private SyntaxList<AccessorDeclarationSyntax> ReplaceAccessors(SyntaxList<AccessorDeclarationSyntax> accessors)
                    => ReplaceList(accessors);

                private SyntaxList<MemberDeclarationSyntax> ReplaceMembers(SyntaxList<MemberDeclarationSyntax> members, bool global)
                {
                    // okay, this visit contains the statement
                    var newMembers = new List<MemberDeclarationSyntax>(members);

                    var firstMemberIndex = newMembers.FindIndex(s => s == (global ? _firstStatementOrFieldToReplace.Parent : _firstStatementOrFieldToReplace));
                    Contract.ThrowIfFalse(firstMemberIndex >= 0);

                    var lastMemberIndex = newMembers.FindIndex(s => s == (global ? _lastStatementOrFieldToReplace.Parent : _lastStatementOrFieldToReplace));
                    Contract.ThrowIfFalse(lastMemberIndex >= 0);

                    Contract.ThrowIfFalse(firstMemberIndex <= lastMemberIndex);

                    // remove statement that must be removed
                    newMembers.RemoveRange(firstMemberIndex, lastMemberIndex - firstMemberIndex + 1);

                    // add new statements to replace
                    newMembers.InsertRange(firstMemberIndex,
                        _statementsOrMemberOrAccessorToInsert.Select(s => global ? SyntaxFactory.GlobalStatement((StatementSyntax)s) : (MemberDeclarationSyntax)s));

                    return [.. newMembers];
                }

                public override SyntaxNode VisitGlobalStatement(GlobalStatementSyntax node)
                {
                    if (node != ContainerOfStatementsOrFieldToReplace)
                    {
                        return base.VisitGlobalStatement(node);
                    }

                    return node.WithStatement(ReplaceStatementIfNeeded(node.Statement));
                }

                public override SyntaxNode VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
                {
                    if (node != ContainerOfStatementsOrFieldToReplace)
                    {
                        return base.VisitInterfaceDeclaration(node);
                    }

                    return GetUpdatedTypeDeclaration(node);
                }

                public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
                {
                    if (node != ContainerOfStatementsOrFieldToReplace)
                    {
                        return base.VisitConstructorDeclaration(node);
                    }

                    Contract.ThrowIfFalse(_firstStatementOrFieldToReplace == _lastStatementOrFieldToReplace);
                    return node.WithInitializer((ConstructorInitializerSyntax)_statementsOrMemberOrAccessorToInsert.Single());
                }

                public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
                {
                    if (node != ContainerOfStatementsOrFieldToReplace)
                    {
                        return base.VisitClassDeclaration(node);
                    }

                    return GetUpdatedTypeDeclaration(node);
                }

                public override SyntaxNode VisitRecordDeclaration(RecordDeclarationSyntax node)
                {
                    if (node != ContainerOfStatementsOrFieldToReplace)
                    {
                        return base.VisitRecordDeclaration(node);
                    }

                    return GetUpdatedTypeDeclaration(node);
                }

                public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax node)
                {
                    if (node != ContainerOfStatementsOrFieldToReplace)
                    {
                        return base.VisitStructDeclaration(node);
                    }

                    return GetUpdatedTypeDeclaration(node);
                }

                public override SyntaxNode VisitAccessorList(AccessorListSyntax node)
                {
                    if (node != ContainerOfStatementsOrFieldToReplace)
                    {
                        return base.VisitAccessorList(node);
                    }

                    var newAccessors = VisitList(ReplaceAccessors(node.Accessors));
                    return node.WithAccessors(newAccessors);
                }

                public override SyntaxNode VisitCompilationUnit(CompilationUnitSyntax node)
                {
                    if (node != ContainerOfStatementsOrFieldToReplace.Parent)
                    {
                        // make sure we visit nodes under the block
                        return base.VisitCompilationUnit(node);
                    }

                    var newMembers = VisitList(ReplaceMembers(node.Members, global: true));
                    return node.WithMembers(newMembers);
                }

                public override SyntaxNode VisitBaseList(BaseListSyntax node)
                {
                    if (node != ContainerOfStatementsOrFieldToReplace)
                        return base.VisitBaseList(node);

                    var primaryConstructorBase = (PrimaryConstructorBaseTypeSyntax)_statementsOrMemberOrAccessorToInsert.Single();
                    return node.WithTypes(node.Types.Replace(node.Types[0], primaryConstructorBase));
                }

                private SyntaxNode GetUpdatedTypeDeclaration(TypeDeclarationSyntax node)
                {
                    var newMembers = VisitList(ReplaceMembers(node.Members, global: false));
                    return node.WithMembers(newMembers);
                }
            }
        }
    }
}
