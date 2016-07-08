// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class SimpleLambdaExpressionSyntax
    {
        public SimpleLambdaExpressionSyntax Update(SyntaxToken asyncKeyword, ParameterSyntax parameter, SyntaxToken arrowToken, CSharpSyntaxNode body)
        {
            return Update(asyncKeyword, parameter, arrowToken, this.RefKeyword, body);
        }
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        /// <summary>Creates a new SimpleLambdaExpressionSyntax instance.</summary>
        public static SimpleLambdaExpressionSyntax SimpleLambdaExpression(SyntaxToken asyncKeyword, ParameterSyntax parameter, SyntaxToken arrowToken, CSharpSyntaxNode body)
        {
            return SimpleLambdaExpression(asyncKeyword, parameter, arrowToken, default(SyntaxToken), body);
        }
    }
}