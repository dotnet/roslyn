// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal abstract class MemberBody : DeclarationBody
{
    /// <summary>
    /// A span that contains all possible breakpoint spans of <see cref="MemberBody"/>
    /// and no breakpoint spans that do not belong to the <see cref="MemberBody"/>.
    /// </summary>
    public abstract ActiveStatementEnvelope Envelope { get; }

    /// <summary>
    /// <see cref="SyntaxNode"/> that includes all active tokens (<see cref="GetActiveTokens"/>) and its span covers the entire <see cref="Envelope"/>.
    /// </summary>
    public abstract SyntaxNode EncompassingAncestor { get; }

    /// <summary>
    /// All tokens of the body that may be part of an active statement.
    /// </summary>
    public abstract IEnumerable<SyntaxToken>? GetActiveTokens();

    /// <summary>
    /// Finds am active statement at given span within this body and the corresponding partner statement in 
    /// <paramref name="partnerDeclarationBody"/>, if specified. Only called with <paramref name="partnerDeclarationBody"/> when
    /// the body does not have any non-trivial changes and thus the correpsonding active statement is always found in the partner body.
    /// </summary>
    public abstract SyntaxNode FindStatementAndPartner(TextSpan span, MemberBody? partnerDeclarationBody, out SyntaxNode? partnerStatement, out int statementPart);

    public SyntaxNode FindStatement(TextSpan span, out int statementPart)
        => FindStatementAndPartner(span, partnerDeclarationBody: null, out _, out statementPart);

    /// <summary>
    /// Analyzes data flow in the member body represented by the specified node and returns all captured variables and parameters (including "this").
    /// If the body is a field/property initializer analyzes the initializer expression only.
    /// </summary>
    public abstract ImmutableArray<ISymbol> GetCapturedVariables(SemanticModel model);
}
