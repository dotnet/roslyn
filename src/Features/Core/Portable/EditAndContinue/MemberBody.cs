// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue;

/// <summary>
/// Represents a declaration of a member with executable code directly inside.
/// </summary>
/// <remarks>
/// Executable code here is code that a user might set breakpoints in. For example, a normal type with no primary constructor would not have any directly-nested
/// executable code, but a record with a primary constructor might.
/// </remarks>
internal abstract class MemberBody : DeclarationBody
{
    /// <summary>
    /// A minimal span that contains all possible breakpoint spans of <see cref="MemberBody"/>.
    /// </summary>
    public abstract TextSpan Envelope { get; }

    /// <summary>
    /// True if <paramref name="span"/> belongs to the <see cref="MemberBody"/>.
    /// </summary>
    public bool ContainsActiveStatementSpan(TextSpan span)
        => Envelope.Contains(span) && !IsExcludedActiveStatementSpanWithinEnvelope(span);

    /// <summary>
    /// True for <paramref name="span"/> within <see cref="Envelope"/> does not belong to the body.
    /// </summary>
    public virtual bool IsExcludedActiveStatementSpanWithinEnvelope(TextSpan span)
        => false;

    /// <summary>
    /// All tokens of the body that may be part of an active statement.
    /// </summary>
    public abstract IEnumerable<SyntaxToken>? GetActiveTokens(Func<SyntaxNode, IEnumerable<SyntaxToken>> getDescendantTokens);

    public IEnumerable<SyntaxToken>? GetActiveTokens()
        => GetActiveTokens(static node => node.DescendantTokens());

    /// <summary>
    /// All tokens of the body representing user code. This may be empty sequence if the entire body is synthesized.
    /// </summary>
    public abstract IEnumerable<SyntaxToken> GetUserCodeTokens(Func<SyntaxNode, IEnumerable<SyntaxToken>> getDescendantTokens);

    /// <summary>
    /// Finds an active statement at given span within this body and the corresponding partner statement in 
    /// <paramref name="partnerDeclarationBody"/>, if specified. Only called with <paramref name="partnerDeclarationBody"/> when
    /// the body does not have any non-trivial changes and thus the correpsonding active statement is always found in the partner body.
    /// </summary>
    public abstract SyntaxNode FindStatementAndPartner(TextSpan span, MemberBody? partnerDeclarationBody, out SyntaxNode? partnerStatement, out int statementPart);

    public SyntaxNode FindStatement(TextSpan span, out int statementPart)
        => FindStatementAndPartner(span, partnerDeclarationBody: null, out _, out statementPart);

    public IEnumerable<int> GetOverlappingActiveStatementIndices(ImmutableArray<UnmappedActiveStatement> statements)
    {
        var envelope = Envelope;

        var range = ActiveStatementsMap.GetSpansStartingInSpan(
            envelope.Start,
            envelope.End,
            statements,
            startPositionComparer: (x, y) => x.UnmappedSpan.Start.CompareTo(y));

        for (var i = range.Start.Value; i < range.End.Value; i++)
        {
            var span = statements[i].UnmappedSpan;
            if (envelope.Contains(span) && !IsExcludedActiveStatementSpanWithinEnvelope(span))
            {
                yield return i;
            }
        }
    }
}
