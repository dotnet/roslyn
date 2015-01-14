' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class SyntaxTokenListTests
        <Fact>
        Public Sub Extensions()
            Dim list = SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.AddHandlerKeyword),
                SyntaxFactory.Literal("x"),
                SyntaxFactory.Token(SyntaxKind.DotToken))

            Assert.Equal(0, list.IndexOf(SyntaxKind.AddHandlerKeyword))
            Assert.True(list.Any(SyntaxKind.AddHandlerKeyword))

            Assert.Equal(1, List.IndexOf(SyntaxKind.StringLiteralToken))
            Assert.True(List.Any(SyntaxKind.StringLiteralToken))

            Assert.Equal(2, List.IndexOf(SyntaxKind.DotToken))
            Assert.True(List.Any(SyntaxKind.DotToken))

            Assert.Equal(-1, list.IndexOf(SyntaxKind.NothingKeyword))
            Assert.False(list.Any(SyntaxKind.NothingKeyword))
        End Sub
    End Class
End Namespace
