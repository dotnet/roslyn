// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename.ConflictEngine;

namespace Microsoft.CodeAnalysis.CSharp.Rename;

internal sealed class LabelConflictVisitor : CSharpSyntaxVisitor
{
    private readonly ConflictingIdentifierTracker _tracker;

    public LabelConflictVisitor(SyntaxToken tokenBeingRenamed)
        => _tracker = new ConflictingIdentifierTracker(tokenBeingRenamed, StringComparer.Ordinal);

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
            if (statement is LabeledStatementSyntax declarationStatement)
            {
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
