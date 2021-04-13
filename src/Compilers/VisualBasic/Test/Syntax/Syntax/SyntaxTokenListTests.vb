' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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
