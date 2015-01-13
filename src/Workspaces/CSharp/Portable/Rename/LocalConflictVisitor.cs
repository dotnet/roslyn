// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename.ConflictEngine;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Rename
{
    internal sealed class LocalConflictVisitor : CSharpSyntaxVisitor
    {
        private readonly ConflictingIdentifierTracker tracker;

        public LocalConflictVisitor(SyntaxToken tokenBeingRenamed)
        {
            tracker = new ConflictingIdentifierTracker(tokenBeingRenamed, StringComparer.Ordinal);
        }

        public override void DefaultVisit(SyntaxNode node)
        {
            foreach (var child in node.ChildNodes())
            {
                Visit(child);
            }
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var parameterTokens = node.ParameterList.Parameters.Select(p => p.Identifier);
            tracker.AddIdentifiers(parameterTokens);
            Visit(node.Body);
            tracker.RemoveIdentifiers(parameterTokens);
        }

        public override void VisitBlock(BlockSyntax node)
        {
            var tokens = new List<SyntaxToken>();

            // We want to collect any variable declarations that are in the block
            // before visiting nested statements
            foreach (var statement in node.Statements)
            {
                if (statement.Kind() == SyntaxKind.LocalDeclarationStatement)
                {
                    var declarationStatement = (LocalDeclarationStatementSyntax)statement;

                    foreach (var declarator in declarationStatement.Declaration.Variables)
                    {
                        tokens.Add(declarator.Identifier);
                    }
                }
            }

            tracker.AddIdentifiers(tokens);
            DefaultVisit(node);
            tracker.RemoveIdentifiers(tokens);
        }

        public override void VisitForEachStatement(ForEachStatementSyntax node)
        {
            tracker.AddIdentifier(node.Identifier);
            Visit(node.Statement);
            tracker.RemoveIdentifier(node.Identifier);
        }

        public override void VisitForStatement(ForStatementSyntax node)
        {
            var tokens = new List<SyntaxToken>();

            if (node.Declaration != null)
            {
                tokens.AddRange(node.Declaration.Variables.Select(v => v.Identifier));
            }

            tracker.AddIdentifiers(tokens);
            Visit(node.Statement);
            tracker.RemoveIdentifiers(tokens);
        }

        public override void VisitUsingStatement(UsingStatementSyntax node)
        {
            var tokens = new List<SyntaxToken>();

            if (node.Declaration != null)
            {
                tokens.AddRange(node.Declaration.Variables.Select(v => v.Identifier));
            }

            tracker.AddIdentifiers(tokens);
            Visit(node.Statement);
            tracker.RemoveIdentifiers(tokens);
        }

        public override void VisitCatchClause(CatchClauseSyntax node)
        {
            var tokens = new List<SyntaxToken>();

            if (node.Declaration != null)
            {
                tokens.Add(node.Declaration.Identifier);
            }

            tracker.AddIdentifiers(tokens);
            Visit(node.Block);
            tracker.RemoveIdentifiers(tokens);
        }

        public override void VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
        {
            tracker.AddIdentifier(node.Parameter.Identifier);
            Visit(node.Body);
            tracker.RemoveIdentifier(node.Parameter.Identifier);
        }

        public override void VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
        {
            var tokens = node.ParameterList.Parameters.Select(p => p.Identifier);
            tracker.AddIdentifiers(tokens);
            Visit(node.Body);
            tracker.RemoveIdentifiers(tokens);
        }

        public override void VisitQueryExpression(QueryExpressionSyntax node)
        {
            VisitQueryInternal(node.FromClause, node.Body);
        }

        private void VisitQueryInternal(FromClauseSyntax fromClause, QueryBodySyntax body)
        {
            // This is somewhat ornery: we need to collect all the locals being introduced
            // since they're all in scope throught all parts of the query.
            var tokens = new List<SyntaxToken>();

            if (fromClause != null)
            {
                tokens.Add(fromClause.Identifier);
            }

            foreach (var clause in body.Clauses)
            {
                switch (clause.Kind())
                {
                    case SyntaxKind.FromClause:

                        tokens.Add(((FromClauseSyntax)clause).Identifier);
                        break;

                    case SyntaxKind.LetClause:

                        tokens.Add(((LetClauseSyntax)clause).Identifier);
                        break;
                }
            }

            tracker.AddIdentifiers(tokens);

            // We have to be careful that the query continuation of this query isn't visited
            // as everything there is actually an independent scope.
            if (fromClause != null)
            {
                Visit(fromClause);
            }

            foreach (var child in body.ChildNodes().Where(c => c.Kind() != SyntaxKind.QueryContinuation))
            {
                Visit(child);
            }

            tracker.RemoveIdentifiers(tokens);

            // And now we must visit the continuation
            Visit(body.Continuation);
        }

        public override void VisitQueryContinuation(QueryContinuationSyntax node)
        {
            tracker.AddIdentifier(node.Identifier);
            VisitQueryInternal(null, node.Body);
            tracker.RemoveIdentifier(node.Identifier);
        }

        public IEnumerable<SyntaxToken> ConflictingTokens
        {
            get
            {
                return tracker.ConflictingTokens;
            }
        }
    }
}