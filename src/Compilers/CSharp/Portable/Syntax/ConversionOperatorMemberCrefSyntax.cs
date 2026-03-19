// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class ConversionOperatorMemberCrefSyntax
    {
        public ConversionOperatorMemberCrefSyntax Update(SyntaxToken implicitOrExplicitKeyword, SyntaxToken operatorKeyword, TypeSyntax type, CrefParameterListSyntax? parameters)
        {
            return Update(
                implicitOrExplicitKeyword: implicitOrExplicitKeyword,
                operatorKeyword: operatorKeyword,
                checkedKeyword: this.CheckedKeyword,
                type: type,
                parameters: parameters);
        }
    }
}
