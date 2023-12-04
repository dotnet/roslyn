' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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
