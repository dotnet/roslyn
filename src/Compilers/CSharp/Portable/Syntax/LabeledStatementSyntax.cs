// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class LabeledStatementSyntax
    {
        public LabeledStatementSyntax Update(SyntaxToken identifier, SyntaxToken colonToken, StatementSyntax statement)
            => Update(AttributeLists, identifier, colonToken, statement);
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static LabeledStatementSyntax LabeledStatement(SyntaxToken identifier, SyntaxToken colonToken, StatementSyntax statement)
            => LabeledStatement(attributeLists: default, identifier, colonToken, statement);
    }
}

