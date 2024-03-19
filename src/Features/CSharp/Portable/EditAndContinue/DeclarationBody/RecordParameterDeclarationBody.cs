// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue;

/// <summary>
/// record C([Attr] [|in int a|] = 1);
/// </summary>
internal sealed class RecordParameterDeclarationBody(ParameterSyntax parameter) : PropertyOrIndexerAccessorDeclarationBody
{
    public override SyntaxNode? ExplicitBody
        => null;

    public override SyntaxNode? HeaderActiveStatement
        => parameter;

    public override TextSpan HeaderActiveStatementSpan
        => BreakpointSpans.CreateSpanForRecordParameter(parameter);

    public override SyntaxNode? MatchRoot
        => null;

    public override IEnumerable<SyntaxToken>? GetActiveTokens()
        => BreakpointSpans.GetActiveTokensForRecordParameter(parameter);
}
