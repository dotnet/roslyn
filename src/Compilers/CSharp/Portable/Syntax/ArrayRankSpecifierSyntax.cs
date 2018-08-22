// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class ArrayRankSpecifierSyntax
    {
        public int Rank
        {
            get
            {
                return this.Sizes.Count;
            }
        }

        public ArrayRankSpecifierSyntax Update(SyntaxToken openBracketToken, SeparatedSyntaxList<ExpressionSyntax> sizes, SyntaxToken closeBracketToken)
        {
            return this.Update(openBracketToken, sizes, closeBracketToken, default(SyntaxToken));
        }
    }
}
