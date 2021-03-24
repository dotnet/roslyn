// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static PropertyPatternClauseSyntax PropertyPatternClause(SyntaxToken openBraceToken, SeparatedSyntaxList<SubpatternSyntax> subpatterns, SyntaxToken closeBraceToken)
        {
            return PropertyPatternClause(SyntaxKind.PropertyPatternClause, openBraceToken, subpatterns, closeBraceToken);
        }

        public static PropertyPatternClauseSyntax PropertyPatternClause(SeparatedSyntaxList<SubpatternSyntax> subpatterns)
        {
            return PropertyPatternClause(SyntaxKind.PropertyPatternClause, subpatterns);
        }
    }
}
