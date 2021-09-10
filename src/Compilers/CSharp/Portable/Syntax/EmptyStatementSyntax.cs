// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class EmptyStatementSyntax
    {
        public EmptyStatementSyntax Update(SyntaxToken semicolonToken)
            => Update(AttributeLists, semicolonToken);
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static EmptyStatementSyntax EmptyStatement(SyntaxToken semicolonToken)
            => EmptyStatement(attributeLists: default, semicolonToken);
    }
}
