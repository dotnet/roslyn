' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.BraceMatching

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.BraceMatching
    Friend Class AbstractVisualBasicBraceMatcher
        Inherits AbstractBraceMatcher

        Protected Sub New(openBrace As SyntaxKind,
                          closeBrace As SyntaxKind)
            MyBase.New(New BraceCharacterAndKind(SyntaxFacts.GetText(openBrace)(0), openBrace),
                       New BraceCharacterAndKind(SyntaxFacts.GetText(closeBrace)(0), closeBrace))
        End Sub
    End Class
End Namespace
