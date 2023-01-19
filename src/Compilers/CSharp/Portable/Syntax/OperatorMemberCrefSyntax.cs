// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class OperatorMemberCrefSyntax
    {
        public OperatorMemberCrefSyntax Update(SyntaxToken operatorKeyword, SyntaxToken operatorToken, CrefParameterListSyntax? parameters)
        {
            return Update(
                operatorKeyword: operatorKeyword,
                checkedKeyword: this.CheckedKeyword,
                operatorToken: operatorToken,
                parameters: parameters);
        }
    }
}
