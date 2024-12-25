// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.AddRequiredParentheses;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Precedence;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.AddRequiredParentheses;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal class CSharpAddRequiredPatternParenthesesDiagnosticAnalyzer :
    AbstractAddRequiredParenthesesDiagnosticAnalyzer<
        PatternSyntax, BinaryPatternSyntax, SyntaxKind>
{
    public CSharpAddRequiredPatternParenthesesDiagnosticAnalyzer()
        : base(CSharpPatternPrecedenceService.Instance)
    {
    }

    private static readonly ImmutableArray<SyntaxKind> s_kinds = [SyntaxKind.AndPattern, SyntaxKind.OrPattern];

    protected override ImmutableArray<SyntaxKind> GetSyntaxNodeKinds()
        => s_kinds;

    protected override int GetPrecedence(BinaryPatternSyntax pattern)
        => (int)pattern.GetOperatorPrecedence();

    protected override bool IsBinaryLike(PatternSyntax node)
        => node is BinaryPatternSyntax;

    protected override (PatternSyntax, SyntaxToken, PatternSyntax) GetPartsOfBinaryLike(BinaryPatternSyntax binaryPattern)
    {
        Debug.Assert(IsBinaryLike(binaryPattern));
        return (binaryPattern.Left, binaryPattern.OperatorToken, binaryPattern.Right);
    }

    protected override PatternSyntax? TryGetAppropriateParent(BinaryPatternSyntax binaryLike)
        => binaryLike.Parent as PatternSyntax;
}
