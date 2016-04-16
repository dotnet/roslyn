// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename.ConflictEngine;

namespace Microsoft.CodeAnalysis.CSharp.Rename
{
    internal sealed class LabelConflictVisitor : CSharpSyntaxVisitor
    {
        private readonly ConflictingIdentifierTracker _tracker;

        public LabelConflictVisitor(SyntaxToken tokenBeingRenamed)
        {
            _tracker = new ConflictingIdentifierTracker(tokenBeingRenamed, StringComparer.Ordinal);
        }

        public override void DefaultVisit(SyntaxNode node)
        {
            foreach (var child in node.ChildNodes())
            {
                Visit(child);
            }
        }

        public override void VisitBlock(BlockSyntax node)
        {
            var tokens = new List<SyntaxToken>();

            // We want to collect any labels and add them all at once for this scope
            foreach (var statement in node.Statements)
            {
                if (statement.Kind() == SyntaxKind.LabeledStatement)
                {
                    var declarationStatement = (LabeledStatementSyntax)statement;
                    tokens.Add(declarationStatement.Identifier);
                }
            }

            _tracker.AddIdentifiers(tokens);
            DefaultVisit(node);
            _tracker.RemoveIdentifiers(tokens);
        }

        public IEnumerable<SyntaxToken> ConflictingTokens
        {
            get
            {
                return _tracker.ConflictingTokens;
            }
        }
    }
}
