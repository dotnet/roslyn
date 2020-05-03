' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class SyntaxTriviaListTests
        <Fact>
        Public Sub Extensions()
            Dim list = SyntaxFactory.ParseLeadingTrivia(": ")

            Assert.Equal(0, list.IndexOf(SyntaxKind.ColonTrivia))
            Assert.True(list.Any(SyntaxKind.ColonTrivia))

            Assert.Equal(1, list.IndexOf(SyntaxKind.WhitespaceTrivia))
            Assert.True(list.Any(SyntaxKind.WhitespaceTrivia))

            Assert.Equal(-1, list.IndexOf(SyntaxKind.CommentTrivia))
            Assert.False(list.Any(SyntaxKind.CommentTrivia))
        End Sub
    End Class
End Namespace
