' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.Implementation.BraceMatching

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
