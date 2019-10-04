// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class LabeledStatementSyntax
    {
        public LabeledStatementSyntax Update(SyntaxToken identifier, SyntaxToken colonToken, StatementSyntax statement)
            => Update(attributeLists: default, identifier, colonToken, statement);
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

