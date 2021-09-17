// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    partial class NameColonSyntax
    {
        public override ExpressionSyntax Expression => Name;

        internal override BaseExpressionColonSyntax WithExpressionCore(ExpressionSyntax expression)
        {
            if (expression is IdentifierNameSyntax identifierName)
                return WithName(identifierName);
            return SyntaxFactory.ExpressionColon(expression, this.ColonToken);
        }
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static NameColonSyntax NameColon(IdentifierNameSyntax name)
            => NameColon(name, Token(SyntaxKind.ColonToken));

        public static NameColonSyntax NameColon(string name)
            => NameColon(IdentifierName(name));
    }
}
