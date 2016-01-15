' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax
    Partial Public Class SkippedTokensTriviaSyntax
        Implements ISkippedTokensTriviaSyntax

        Private ReadOnly Property ISkippedTokensTriviaSyntax_Tokens As SyntaxTokenList Implements ISkippedTokensTriviaSyntax.Tokens
            Get
                Return Me.Tokens
            End Get
        End Property
    End Class
End Namespace
