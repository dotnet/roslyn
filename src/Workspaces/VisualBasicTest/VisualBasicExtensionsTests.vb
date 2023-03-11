' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Text
Imports Roslyn.Test.Utilities
Imports Xunit
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.SyntaxTreeExtensions
Imports System.Threading

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class VisualBasicExtensionsTests
        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/6536")>
        Public Sub TestFindTrivia_NoStackOverflowOnLargeExpression()
            Dim code As New StringBuilder()
            code.Append(<![CDATA[

Module Module1
     Sub Test()
         Dim c =  ]]>.Value)
            For i = 0 To 3000
                code.Append("""asdf"" + ")
            Next

            code.AppendLine(<![CDATA["last"
    End Sub
End Module]]>.Value)

            Dim tree = VisualBasicSyntaxTree.ParseText(code.ToString())
            Dim trivia = tree.FindTriviaToLeft(4000, CancellationToken.None)
            ' no stack overflow
        End Sub

    End Class

End Namespace
