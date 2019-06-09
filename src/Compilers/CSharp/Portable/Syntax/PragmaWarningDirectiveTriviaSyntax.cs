// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class PragmaWarningDirectiveTriviaSyntax
    {
        public PragmaWarningDirectiveTriviaSyntax Update(SyntaxToken hashToken, SyntaxToken pragmaKeyword, SyntaxToken warningKeyword, SyntaxToken disableOrRestoreKeyword, SeparatedSyntaxList<ExpressionSyntax> errorCodes, SyntaxToken endOfDirectiveToken, bool isActive)
        {
            return Update(hashToken, pragmaKeyword, warningKeyword, disableOrRestoreKeyword, nullableKeyword: default, errorCodes, endOfDirectiveToken, isActive);
        }

        public PragmaWarningDirectiveTriviaSyntax Update(SyntaxToken hashToken, SyntaxToken pragmaKeyword, SyntaxToken warningKeyword, SyntaxToken disableOrRestoreKeyword, SyntaxToken nullableKeyword, SyntaxToken endOfDirectiveToken, bool isActive)
        {
            return Update(hashToken, pragmaKeyword, warningKeyword, disableOrRestoreKeyword, nullableKeyword, errorCodes: default, endOfDirectiveToken, isActive);
        }
    }
}
