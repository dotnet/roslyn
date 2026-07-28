// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using CSharpToVisualBasicConverter.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using CS = Microsoft.CodeAnalysis.CSharp;
using VB = Microsoft.CodeAnalysis.VisualBasic;

namespace CSharpToVisualBasicConverter
{
    public partial class Converter
    {
        private partial class StatementVisitor : CS.CSharpSyntaxVisitor<SyntaxList<VB.Syntax.StatementSyntax>>
        {
            private readonly NodeVisitor nodeVisitor;
            private readonly SourceText text;

            public StatementVisitor(NodeVisitor nodeVisitor, SourceText text)
            {
                this.nodeVisitor = nodeVisitor;
                this.text = text;
            }

            public IEnumerable<VB.Syntax.StatementSyntax> VisitStatementEnumerable(CS.Syntax.StatementSyntax node)
            {
                return Visit(node);
            }

            public SyntaxList<VB.Syntax.StatementSyntax> VisitStatement(CS.Syntax.StatementSyntax node)
            {
                return Visit(node);
            }

            private static VB.Syntax.StatementSyntax ConvertToStatement(SyntaxNode node)
            {
                if (node == null)
                {
                    return null;
                }
                else if (node is VB.Syntax.StatementSyntax)
                {
                    return (VB.Syntax.StatementSyntax)node;
                }
                else if (node is VB.Syntax.InvocationExpressionSyntax)
                {
                    return VB.SyntaxFactory.ExpressionStatement((VB.Syntax.InvocationExpressionSyntax)node);
                }
                else
                {
                    // can happen in error scenarios
                    return CreateBadStatement(((SyntaxNode)node).ToFullString(), typeof(VB.Syntax.StatementSyntax));
                }
            }

            public override SyntaxList<VB.Syntax.StatementSyntax> VisitBlock(CS.Syntax.BlockSyntax node)
            {
                List<VB.Syntax.StatementSyntax> statements = node.Statements.SelectMany(VisitStatementEnumerable).ToList();

                if (node.IsParentKind(CS.SyntaxKind.ConstructorDeclaration))
                {
                    ConstructorDeclarationSyntax constructor = (CS.Syntax.ConstructorDeclarationSyntax)node.Parent;
                    if (constructor.Initializer != null)
                    {
                        VB.Syntax.StatementSyntax initializer = nodeVisitor.Visit<VB.Syntax.StatementSyntax>(constructor.Initializer);
                        statements.Insert(0, initializer);
                    }
                }

                return List<VB.Syntax.StatementSyntax>(statements);
            }

            public override SyntaxList<VB.Syntax.StatementSyntax> VisitLocalDeclarationStatement(CS.Syntax.LocalDeclarationStatementSyntax node)
            {
                SyntaxTriviaList leadingTrivia = TriviaList(node.GetFirstToken(includeSkipped: true).LeadingTrivia.SelectMany(nodeVisitor.VisitTrivia));

                SyntaxToken token = node.Modifiers.Any(t => t.IsKind(CS.SyntaxKind.ConstKeyword))
                    ? VB.SyntaxFactory.Token(leadingTrivia, VB.SyntaxKind.ConstKeyword)
                    : VB.SyntaxFactory.Token(leadingTrivia, VB.SyntaxKind.DimKeyword);

                return List<VB.Syntax.StatementSyntax>(
                    VB.SyntaxFactory.FieldDeclaration(
                        new SyntaxList<VB.Syntax.AttributeListSyntax>(),
                        SyntaxTokenList.Create(token),
                        SeparatedCommaList(node.Declaration.Variables.Select(nodeVisitor.Visit<VB.Syntax.VariableDeclaratorSyntax>))));
            }

            public override SyntaxList<VB.Syntax.StatementSyntax> VisitReturnStatement(CS.Syntax.ReturnStatementSyntax node)
            {
                return List<VB.Syntax.StatementSyntax>(
                    VB.SyntaxFactory.ReturnStatement(nodeVisitor.VisitExpression(node.Expression)));
            }

            public override SyntaxList<VB.Syntax.StatementSyntax> VisitExpressionStatement(CS.Syntax.ExpressionStatementSyntax node)
            {
                return List(
                    nodeVisitor.VisitStatement(node.Expression));
            }

            public override SyntaxList<VB.Syntax.StatementSyntax> VisitIfStatement(CS.Syntax.IfStatementSyntax node)
            {
                VB.Syntax.IfStatementSyntax ifStatement = VB.SyntaxFactory.IfStatement(
                    VB.SyntaxFactory.Token(VB.SyntaxKind.IfKeyword),
                    nodeVisitor.VisitExpression(node.Condition),
                    VB.SyntaxFactory.Token(VB.SyntaxKind.ThenKeyword));

                List<VB.Syntax.ElseIfBlockSyntax> elseIfBlocks = new List<VB.Syntax.ElseIfBlockSyntax>();
                ElseClauseSyntax currentElseClause = node.Else;
                while (currentElseClause != null)
                {
                    if (!currentElseClause.Statement.IsKind(CS.SyntaxKind.IfStatement))
                    {
                        break;
                    }

                    IfStatementSyntax nestedIf = (CS.Syntax.IfStatementSyntax)currentElseClause.Statement;
                    currentElseClause = nestedIf.Else;

                    VB.Syntax.ElseIfStatementSyntax elseIfStatement = VB.SyntaxFactory.ElseIfStatement(
                        VB.SyntaxFactory.Token(VB.SyntaxKind.ElseIfKeyword),
                        nodeVisitor.VisitExpression(nestedIf.Condition),
                        VB.SyntaxFactory.Token(VB.SyntaxKind.ThenKeyword));
                    VB.Syntax.ElseIfBlockSyntax elseIfBlock = VB.SyntaxFactory.ElseIfBlock(
                        elseIfStatement,
                        Visit(nestedIf.Statement));
                    elseIfBlocks.Add(elseIfBlock);
                }

                return List<VB.Syntax.StatementSyntax>(
                    VB.SyntaxFactory.MultiLineIfBlock(
                        ifStatement,
                        Visit(node.Statement),
                        List<VB.Syntax.ElseIfBlockSyntax>(elseIfBlocks),
                        currentElseClause == null ? null : nodeVisitor.Visit<VB.Syntax.ElseBlockSyntax>(currentElseClause)));
            }

            public override SyntaxList<VB.Syntax.StatementSyntax> VisitSwitchStatement(CS.Syntax.SwitchStatementSyntax node)
            {
                VB.Syntax.SelectStatementSyntax begin = VB.SyntaxFactory.SelectStatement(
                    expression: nodeVisitor.VisitExpression(node.Expression));

                return List<VB.Syntax.StatementSyntax>(
                    VB.SyntaxFactory.SelectBlock(
                        begin,
                        List(node.Sections.Select(nodeVisitor.Visit<VB.Syntax.CaseBlockSyntax>))));
            }

            public override SyntaxList<VB.Syntax.StatementSyntax> VisitThrowStatement(CS.Syntax.ThrowStatementSyntax node)
            {
                return List<VB.Syntax.StatementSyntax>(
                    VB.SyntaxFactory.ThrowStatement(nodeVisitor.VisitExpression(node.Expression)));
            }

            public override SyntaxList<VB.Syntax.StatementSyntax> VisitBreakStatement(CS.Syntax.BreakStatementSyntax node)
            {
                return List(VisitBreakStatementWorker(node));
            }

            private VB.Syntax.StatementSyntax VisitBreakStatementWorker(CS.Syntax.BreakStatementSyntax node)
            {
                foreach (SyntaxNode parent in node.GetAncestorsOrThis<SyntaxNode>())
                {
                    if (parent.IsBreakableConstruct())
                    {
                        switch (parent.Kind())
                        {
                            case CS.SyntaxKind.DoStatement:
                                return VB.SyntaxFactory.ExitDoStatement();
                            case CS.SyntaxKind.WhileStatement:
                                return VB.SyntaxFactory.ExitWhileStatement();
                            case CS.SyntaxKind.SwitchStatement:
                                // If the 'break' is the last statement of a switch block, then we
                                // don't need to translate it into VB (as it is implied).
                                SwitchSectionSyntax outerSection = node.FirstAncestorOrSelf<CS.Syntax.SwitchSectionSyntax>();
                                if (outerSection != null && outerSection.Statements.Count > 0)
                                {
                                    if (node == outerSection.Statements.Last())
                                    {
                                        return VB.SyntaxFactory.EmptyStatement();
                                    }
                                }

                                return VB.SyntaxFactory.ExitSelectStatement();
                            case CS.SyntaxKind.ForStatement:
                            case CS.SyntaxKind.ForEachStatement:
                                return VB.SyntaxFactory.ExitForStatement();
                        }
                    }
                }

                return CreateBadStatement(node, nodeVisitor);
            }

            public override SyntaxList<VB.Syntax.StatementSyntax> VisitContinueStatement(CS.Syntax.ContinueStatementSyntax node)
            {
                return List(VisitContinueStatementWorker(node));
            }

            private VB.Syntax.StatementSyntax VisitContinueStatementWorker(CS.Syntax.ContinueStatementSyntax node)
            {
                foreach (SyntaxNode parent in node.GetAncestorsOrThis<SyntaxNode>())
                {
                    if (parent.IsContinuableConstruct())
                    {
                        switch (parent.Kind())
                        {
                            case CS.SyntaxKind.DoStatement:
                                return VB.SyntaxFactory.ContinueDoStatement();
                            case CS.SyntaxKind.WhileStatement:
                                return VB.SyntaxFactory.ContinueWhileStatement();
                            case CS.SyntaxKind.ForStatement:
                            case CS.SyntaxKind.ForEachStatement:
                                return VB.SyntaxFactory.ContinueForStatement();
                        }
                    }
                }

                return CreateBadStatement(node, nodeVisitor);
            }

            public override SyntaxList<VB.Syntax.StatementSyntax> VisitWhileStatement(CS.Syntax.WhileStatementSyntax node)
            {
                VB.Syntax.WhileStatementSyntax begin = VB.SyntaxFactory.WhileStatement(
                    nodeVisitor.VisitExpression(node.Condition));

                return List<VB.Syntax.StatementSyntax>(
                    VB.SyntaxFactory.WhileBlock(
                        begin,
                        Visit(node.Statement)));
            }

            public override SyntaxList<VB.Syntax.StatementSyntax> VisitForEachStatement(CS.Syntax.ForEachStatementSyntax node)
            {
                VB.Syntax.ForEachStatementSyntax begin = VB.SyntaxFactory.ForEachStatement(
                    VB.SyntaxFactory.IdentifierName(nodeVisitor.ConvertIdentifier(node.Identifier)),
                    nodeVisitor.VisitExpression(node.Expression));

                return List<VB.Syntax.StatementSyntax>(
                    VB.SyntaxFactory.ForEachBlock(
                        begin,
                        Visit(node.Statement),
                        VB.SyntaxFactory.NextStatement()));
            }

            public override SyntaxList<VB.Syntax.StatementSyntax> VisitYieldStatement(CS.Syntax.YieldStatementSyntax node)
            {
                // map this to a return statement for now.
                return List<VB.Syntax.StatementSyntax>(
                    VB.SyntaxFactory.ReturnStatement(nodeVisitor.VisitExpression(node.Expression)));
            }

            public override SyntaxList<VB.Syntax.StatementSyntax> VisitDoStatement(CS.Syntax.DoStatementSyntax node)
            {
                VB.Syntax.DoStatementSyntax begin = VB.SyntaxFactory.SimpleDoStatement();

                VB.Syntax.LoopStatementSyntax loop = VB.SyntaxFactory.LoopWhileStatement(
                    VB.SyntaxFactory.WhileClause(nodeVisitor.VisitExpression(node.Condition)));

                return List<VB.Syntax.StatementSyntax>(
                    VB.SyntaxFactory.DoLoopWhileBlock(
                        begin,
                        Visit(node.Statement),
                        loop));
            }

            public override SyntaxList<VB.Syntax.StatementSyntax> VisitUsingStatement(CS.Syntax.UsingStatementSyntax node)
            {
                VB.Syntax.UsingStatementSyntax usingStatement;
                if (node.Expression != null)
                {
                    usingStatement = VB.SyntaxFactory.UsingStatement().WithExpression(
                        nodeVisitor.VisitExpression(node.Expression));
                }
                else
                {
                    usingStatement = VB.SyntaxFactory.UsingStatement().WithVariables(
                        SeparatedCommaList(node.Declaration.Variables.Select(nodeVisitor.Visit<VB.Syntax.VariableDeclaratorSyntax>)));
                }

                return List<VB.Syntax.StatementSyntax>(
                    VB.SyntaxFactory.UsingBlock(
                        usingStatement,
                        Visit(node.Statement)));
            }

            public override SyntaxList<VB.Syntax.StatementSyntax> VisitLabeledStatement(CS.Syntax.LabeledStatementSyntax node)
            {
                return List<VB.Syntax.StatementSyntax>(
                    VB.SyntaxFactory.LabelStatement(
                        nodeVisitor.ConvertIdentifier(node.Identifier)));
            }

            public override SyntaxList<VB.Syntax.StatementSyntax> VisitGotoStatement(CS.Syntax.GotoStatementSyntax node)
            {
                return List(VisitGotoStatementWorker(node));
            }

            private VB.Syntax.StatementSyntax VisitGotoStatementWorker(CS.Syntax.GotoStatementSyntax node)
            {
                switch (node.Kind())
                {
                    case CS.SyntaxKind.GotoStatement:
                        return VB.SyntaxFactory.GoToStatement(
                            VB.SyntaxFactory.IdentifierLabel(nodeVisitor.ConvertIdentifier((CS.Syntax.IdentifierNameSyntax)node.Expression)));
                    case CS.SyntaxKind.GotoDefaultStatement:
                        return VB.SyntaxFactory.GoToStatement(
                            VB.SyntaxFactory.IdentifierLabel(VB.SyntaxFactory.Identifier("Else")));
                    case CS.SyntaxKind.GotoCaseStatement:
                        string text = node.Expression.ToString();
                        return VB.SyntaxFactory.GoToStatement(
                            VB.SyntaxFactory.IdentifierLabel(VB.SyntaxFactory.Identifier(text)));
                }

                throw new NotImplementedException();
            }

            public override SyntaxList<VB.Syntax.StatementSyntax> VisitEmptyStatement(CS.Syntax.EmptyStatementSyntax node)
            {
                return List<VB.Syntax.StatementSyntax>(VB.SyntaxFactory.EmptyStatement());
            }

            public override SyntaxList<VB.Syntax.StatementSyntax> VisitLockStatement(CS.Syntax.LockStatementSyntax node)
            {
                return List<VB.Syntax.StatementSyntax>(
                    VB.SyntaxFactory.SyncLockBlock(
                        VB.SyntaxFactory.SyncLockStatement(
                            nodeVisitor.VisitExpression(node.Expression)),
                            Visit(node.Statement)));
            }

            public override SyntaxList<VB.Syntax.StatementSyntax> VisitTryStatement(CS.Syntax.TryStatementSyntax node)
            {
                return List<VB.Syntax.StatementSyntax>(
                           VB.SyntaxFactory.TryBlock(
                                                Visit(node.Block),
                                                List(node.Catches.Select(nodeVisitor.Visit<VB.Syntax.CatchBlockSyntax>)),
                                                nodeVisitor.Visit<VB.Syntax.FinallyBlockSyntax>(node.Finally)));
            }

            public override SyntaxList<VB.Syntax.StatementSyntax> VisitFixedStatement(CS.Syntax.FixedStatementSyntax node)
            {
                // todo
                return Visit(node.Statement);
            }

            public override SyntaxList<VB.Syntax.StatementSyntax> VisitUnsafeStatement(CS.Syntax.UnsafeStatementSyntax node)
            {
                return Visit(node.Block);
            }

            public override SyntaxList<VB.Syntax.StatementSyntax> VisitCheckedStatement(CS.Syntax.CheckedStatementSyntax node)
            {
                return Visit(node.Block);
            }

            public override SyntaxList<VB.Syntax.StatementSyntax> DefaultVisit(SyntaxNode node)
            {
                throw new NotImplementedException();
            }
        }
    }
}
