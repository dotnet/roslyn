// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public sealed partial class ForEachStatementSyntax : StatementSyntax
    {
        public ForEachStatementSyntax Update(SyntaxToken forEachKeyword, SyntaxToken openParenToken, TypeSyntax type, SyntaxToken identifier, SyntaxToken inKeyword, ExpressionSyntax expression, SyntaxToken closeParenToken, StatementSyntax statement)
        {
            return Update(forEachKeyword, openParenToken, type, identifier, null, inKeyword, expression, closeParenToken, statement);
        }

        internal bool IsDeconstructionDeclaration => DeconstructionVariables != null;
    }
}
