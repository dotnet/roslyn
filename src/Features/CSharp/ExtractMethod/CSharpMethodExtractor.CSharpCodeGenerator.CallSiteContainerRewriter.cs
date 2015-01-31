// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ExtractMethod
{
    internal partial class CSharpMethodExtractor
    {
        private abstract partial class CSharpCodeGenerator
        {
            private class CallSiteContainerRewriter : CSharpSyntaxRewriter
            {
                private readonly SyntaxNode outmostCallSiteContainer;
                private readonly IEnumerable<SyntaxNode> statementsOrFieldToInsert;
                private readonly HashSet<SyntaxAnnotation> variableToRemoveMap;
                private readonly SyntaxNode firstStatementOrFieldToReplace;
                private readonly SyntaxNode lastStatementOrFieldToReplace;

                public CallSiteContainerRewriter(
                    SyntaxNode outmostCallSiteContainer,
                    HashSet<SyntaxAnnotation> variableToRemoveMap,
                    SyntaxNode firstStatementOrFieldToReplace,
                    SyntaxNode lastStatementOrFieldToReplace,
                    IEnumerable<SyntaxNode> statementsOrFieldToInsert)
                {
                    Contract.ThrowIfNull(outmostCallSiteContainer);
                    Contract.ThrowIfNull(variableToRemoveMap);
                    Contract.ThrowIfNull(firstStatementOrFieldToReplace);
                    Contract.ThrowIfNull(lastStatementOrFieldToReplace);
                    Contract.ThrowIfNull(statementsOrFieldToInsert);
                    Contract.ThrowIfTrue(statementsOrFieldToInsert.IsEmpty());

                    this.outmostCallSiteContainer = outmostCallSiteContainer;

                    this.variableToRemoveMap = variableToRemoveMap;
                    this.statementsOrFieldToInsert = statementsOrFieldToInsert;

                    this.firstStatementOrFieldToReplace = firstStatementOrFieldToReplace;
                    this.lastStatementOrFieldToReplace = lastStatementOrFieldToReplace;

                    Contract.ThrowIfFalse(this.firstStatementOrFieldToReplace.Parent == this.lastStatementOrFieldToReplace.Parent);
                }

                public SyntaxNode Generate()
                {
                    return Visit(this.outmostCallSiteContainer);
                }

                private SyntaxNode ContainerOfStatementsOrFieldToReplace
                {
                    get { return this.firstStatementOrFieldToReplace.Parent; }
                }

                public override SyntaxNode VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
                {
                    node = (LocalDeclarationStatementSyntax)base.VisitLocalDeclarationStatement(node);

                    var list = new List<VariableDeclaratorSyntax>();
                    var triviaList = new List<SyntaxTrivia>();

                    // go through each var decls in decl statement
                    foreach (var variable in node.Declaration.Variables)
                    {
                        if (this.variableToRemoveMap.HasSyntaxAnnotation(variable))
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
                        return SyntaxFactory.EmptyStatement(SyntaxFactory.Token(SyntaxFactory.TriviaList(triviaList), SyntaxKind.SemicolonToken, SyntaxTriviaList.Create(SyntaxFactory.ElasticMarker)));
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
                                    SyntaxFactory.SeparatedList(list)),
                                    node.SemicolonToken.WithPrependedLeadingTrivia(triviaList));
                }

                // for every kind of extract methods
                public override SyntaxNode VisitBlock(BlockSyntax node)
                {
                    if (node != this.ContainerOfStatementsOrFieldToReplace)
                    {
                        // make sure we visit nodes under the block
                        return base.VisitBlock(node);
                    }

                    return node.WithStatements(VisitList(ReplaceStatements(node.Statements)).ToSyntaxList());
                }

                public override SyntaxNode VisitSwitchSection(SwitchSectionSyntax node)
                {
                    if (node != this.ContainerOfStatementsOrFieldToReplace)
                    {
                        // make sure we visit nodes under the switch section
                        return base.VisitSwitchSection(node);
                    }

                    return node.WithStatements(VisitList(ReplaceStatements(node.Statements)).ToSyntaxList());
                }

                // only for single statement or expression
                public override SyntaxNode VisitLabeledStatement(LabeledStatementSyntax node)
                {
                    if (node != this.ContainerOfStatementsOrFieldToReplace)
                    {
                        return base.VisitLabeledStatement(node);
                    }

                    return node.WithStatement(ReplaceStatementIfNeeded(node.Statement));
                }

                public override SyntaxNode VisitElseClause(ElseClauseSyntax node)
                {
                    if (node != this.ContainerOfStatementsOrFieldToReplace)
                    {
                        return base.VisitElseClause(node);
                    }

                    return node.WithStatement(ReplaceStatementIfNeeded(node.Statement));
                }

                public override SyntaxNode VisitIfStatement(IfStatementSyntax node)
                {
                    if (node != this.ContainerOfStatementsOrFieldToReplace)
                    {
                        return base.VisitIfStatement(node);
                    }

                    return node.WithCondition(VisitNode(node.Condition))
                               .WithStatement(ReplaceStatementIfNeeded(node.Statement))
                               .WithElse(VisitNode(node.Else));
                }

                public override SyntaxNode VisitLockStatement(LockStatementSyntax node)
                {
                    if (node != this.ContainerOfStatementsOrFieldToReplace)
                    {
                        return base.VisitLockStatement(node);
                    }

                    return node.WithExpression(VisitNode(node.Expression))
                               .WithStatement(ReplaceStatementIfNeeded(node.Statement));
                }

                public override SyntaxNode VisitFixedStatement(FixedStatementSyntax node)
                {
                    if (node != this.ContainerOfStatementsOrFieldToReplace)
                    {
                        return base.VisitFixedStatement(node);
                    }

                    return node.WithDeclaration(VisitNode(node.Declaration))
                               .WithStatement(ReplaceStatementIfNeeded(node.Statement));
                }

                public override SyntaxNode VisitUsingStatement(UsingStatementSyntax node)
                {
                    if (node != this.ContainerOfStatementsOrFieldToReplace)
                    {
                        return base.VisitUsingStatement(node);
                    }

                    return node.WithDeclaration(VisitNode(node.Declaration))
                               .WithExpression(VisitNode(node.Expression))
                               .WithStatement(ReplaceStatementIfNeeded(node.Statement));
                }

                public override SyntaxNode VisitForEachStatement(ForEachStatementSyntax node)
                {
                    if (node != this.ContainerOfStatementsOrFieldToReplace)
                    {
                        return base.VisitForEachStatement(node);
                    }

                    return node.WithExpression(VisitNode(node.Expression))
                               .WithStatement(ReplaceStatementIfNeeded(node.Statement));
                }

                public override SyntaxNode VisitForStatement(ForStatementSyntax node)
                {
                    if (node != this.ContainerOfStatementsOrFieldToReplace)
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
                    if (node != this.ContainerOfStatementsOrFieldToReplace)
                    {
                        return base.VisitDoStatement(node);
                    }

                    return node.WithStatement(ReplaceStatementIfNeeded(node.Statement))
                               .WithCondition(VisitNode(node.Condition));
                }

                public override SyntaxNode VisitWhileStatement(WhileStatementSyntax node)
                {
                    if (node != this.ContainerOfStatementsOrFieldToReplace)
                    {
                        return base.VisitWhileStatement(node);
                    }

                    return node.WithCondition(VisitNode(node.Condition))
                               .WithStatement(ReplaceStatementIfNeeded(node.Statement));
                }

                private TNode VisitNode<TNode>(TNode node) where TNode : SyntaxNode
                {
                    return (TNode)Visit(node);
                }

                private StatementSyntax ReplaceStatementIfNeeded(StatementSyntax statement)
                {
                    Contract.ThrowIfNull(statement);

                    // if all three same
                    if ((statement != firstStatementOrFieldToReplace) || (firstStatementOrFieldToReplace != lastStatementOrFieldToReplace))
                    {
                        return statement;
                    }

                    // replace one statement with another
                    if (this.statementsOrFieldToInsert.Count() == 1)
                    {
                        return this.statementsOrFieldToInsert.Cast<StatementSyntax>().Single();
                    }

                    // replace one statement with multiple statements (see bug # 6310)
                    return SyntaxFactory.Block(SyntaxFactory.List(this.statementsOrFieldToInsert.Cast<StatementSyntax>()));
                }

                private SyntaxList<StatementSyntax> ReplaceStatements(SyntaxList<StatementSyntax> statements)
                {
                    // okay, this visit contains the statement
                    var newStatements = new List<StatementSyntax>(statements);

                    var firstStatementIndex = newStatements.FindIndex(s => s == this.firstStatementOrFieldToReplace);
                    Contract.ThrowIfFalse(firstStatementIndex >= 0);

                    var lastStatementIndex = newStatements.FindIndex(s => s == this.lastStatementOrFieldToReplace);
                    Contract.ThrowIfFalse(lastStatementIndex >= 0);

                    Contract.ThrowIfFalse(firstStatementIndex <= lastStatementIndex);

                    // remove statement that must be removed
                    newStatements.RemoveRange(firstStatementIndex, lastStatementIndex - firstStatementIndex + 1);

                    // add new statements to replace
                    newStatements.InsertRange(firstStatementIndex, this.statementsOrFieldToInsert.Cast<StatementSyntax>());

                    return newStatements.ToSyntaxList();
                }

                private SyntaxList<MemberDeclarationSyntax> ReplaceMembers(SyntaxList<MemberDeclarationSyntax> members, bool global)
                {
                    // okay, this visit contains the statement
                    var newMembers = new List<MemberDeclarationSyntax>(members);

                    var firstMemberIndex = newMembers.FindIndex(s => s == (global ? this.firstStatementOrFieldToReplace.Parent : this.firstStatementOrFieldToReplace));
                    Contract.ThrowIfFalse(firstMemberIndex >= 0);

                    var lastMemberIndex = newMembers.FindIndex(s => s == (global ? this.lastStatementOrFieldToReplace.Parent : this.lastStatementOrFieldToReplace));
                    Contract.ThrowIfFalse(lastMemberIndex >= 0);

                    Contract.ThrowIfFalse(firstMemberIndex <= lastMemberIndex);

                    // remove statement that must be removed
                    newMembers.RemoveRange(firstMemberIndex, lastMemberIndex - firstMemberIndex + 1);

                    // add new statements to replace
                    newMembers.InsertRange(firstMemberIndex,
                        this.statementsOrFieldToInsert.Select(s => global ? SyntaxFactory.GlobalStatement((StatementSyntax)s) : (MemberDeclarationSyntax)s));

                    return newMembers.ToSyntaxList();
                }

                public override SyntaxNode VisitGlobalStatement(GlobalStatementSyntax node)
                {
                    if (node != this.ContainerOfStatementsOrFieldToReplace)
                    {
                        return base.VisitGlobalStatement(node);
                    }

                    return node.WithStatement(ReplaceStatementIfNeeded(node.Statement));
                }

                public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
                {
                    if (node != this.ContainerOfStatementsOrFieldToReplace)
                    {
                        return base.VisitConstructorDeclaration(node);
                    }

                    Contract.ThrowIfFalse(this.firstStatementOrFieldToReplace == this.lastStatementOrFieldToReplace);
                    return node.WithInitializer((ConstructorInitializerSyntax)this.statementsOrFieldToInsert.Single());
                }

                public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
                {
                    if (node != this.ContainerOfStatementsOrFieldToReplace)
                    {
                        return base.VisitClassDeclaration(node);
                    }

                    var newMembers = VisitList(ReplaceMembers(node.Members, global: false));
                    return node.WithMembers(newMembers);
                }

                public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax node)
                {
                    if (node != this.ContainerOfStatementsOrFieldToReplace)
                    {
                        return base.VisitStructDeclaration(node);
                    }

                    var newMembers = VisitList(ReplaceMembers(node.Members, global: false));
                    return node.WithMembers(newMembers);
                }

                public override SyntaxNode VisitCompilationUnit(CompilationUnitSyntax node)
                {
                    if (node != this.ContainerOfStatementsOrFieldToReplace.Parent)
                    {
                        // make sure we visit nodes under the block
                        return base.VisitCompilationUnit(node);
                    }

                    var newMembers = VisitList(ReplaceMembers(node.Members, global: true));
                    return node.WithMembers(newMembers);
                }
            }
        }
    }
}
