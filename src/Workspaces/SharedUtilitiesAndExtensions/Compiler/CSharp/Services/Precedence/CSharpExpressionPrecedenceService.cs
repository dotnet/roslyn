// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Precedence
{
    internal class CSharpExpressionPrecedenceService : AbstractCSharpPrecedenceService<ExpressionSyntax>
    {
        public static readonly CSharpExpressionPrecedenceService Instance = new CSharpExpressionPrecedenceService();

        private CSharpExpressionPrecedenceService()
        {
        }

        public override OperatorPrecedence GetOperatorPrecedence(ExpressionSyntax expression)
            => expression.GetOperatorPrecedence();
    }
}
