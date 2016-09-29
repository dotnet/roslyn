' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Text
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Test.Utilities
Imports Xunit
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.SyntaxTreeExtensions
Imports System.Threading

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class VisualBasicExtensionsTests
        <WorkItem(6536, "https://github.com/dotnet/roslyn/issues/6536")>
        <Fact>
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