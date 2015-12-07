' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class SyntaxSubmissionsTests
        Public Sub AssertCompleteSubmission(code As String)
            Assert.True(SyntaxFactory.IsCompleteSubmission(SyntaxFactory.ParseSyntaxTree(code, options:=TestOptions.Script)))
        End Sub

        Public Sub AssertInvalidCompleteSubmission(code As String)
            ' Invalid submissions (with compile errors) are treated as complete submissions.
            AssertCompleteSubmission(code)
        End Sub

        Public Sub AssertIncompleteSubmission(code As String)
            Assert.False(SyntaxFactory.IsCompleteSubmission(SyntaxFactory.ParseSyntaxTree(code, options:=TestOptions.Script)))
        End Sub

        <Fact>
        Public Sub TestCompleteSubmission()
            ' Basic submissions
            AssertCompleteSubmission("")
            AssertCompleteSubmission("'comment")
            AssertCompleteSubmission("Dim x = 12")
            AssertInvalidCompleteSubmission("Dim x y = 12")
            AssertInvalidCompleteSubmission("Dim x =")
            AssertIncompleteSubmission("Dim x = _")
            AssertCompleteSubmission(
"Dim x = _
 & ""hello""")

            ' Method calls
            AssertCompleteSubmission("Console.WriteLine(10)")
            AssertInvalidCompleteSubmission("Console.WriteLine(10) Console.WriteLine(10)")
            AssertInvalidCompleteSubmission("Console.WriteLine(1+)")
            AssertIncompleteSubmission("Console.WriteLine(")

            ' Method definitions
            AssertIncompleteSubmission("Sub Main()")
            AssertCompleteSubmission(
"Sub Main()
End Sub")
            AssertInvalidCompleteSubmission(
"Sub Sub Main()
End Sub")

            ' Class definitions
            AssertIncompleteSubmission("Class C")
            AssertCompleteSubmission(
"Class C
End Class")
            AssertInvalidCompleteSubmission(
"Class C
    Sub Main()
End Class")
            AssertCompleteSubmission(
"Class C
    Sub Main()
    End Sub
End Class")

            ' Directives
            AssertCompleteSubmission(
"#if somestatement
#endif")
            AssertCompleteSubmission(
"#region ""r""
#end region")
            AssertIncompleteSubmission("#if somestatement")
            AssertIncompleteSubmission("#region ""r""")
            AssertIncompleteSubmission(
"Sub Main()
#if somestatement
End Sub")
            AssertIncompleteSubmission(
"Sub Main()
#region ""r""
End Sub")

            ' Try statement
            AssertIncompleteSubmission("Try")
            AssertIncompleteSubmission(
"Try
    Console.WriteLine(10)")
            AssertCompleteSubmission(
"Try
    Console.WriteLine(10)
End Try")
            AssertIncompleteSubmission(
"Try
Catch exception")
            AssertIncompleteSubmission(
"Try
Catch exception")

            ' Do statement
            AssertIncompleteSubmission("Do")
            AssertIncompleteSubmission("Do While condition")
            AssertCompleteSubmission(
"Do
Loop")

            ' If statement

            AssertIncompleteSubmission("If something Then")
            AssertInvalidCompleteSubmission("If holidays TakeABreak()")
            AssertCompleteSubmission("If holidays Then TakeABreak()")
            AssertIncompleteSubmission("If something")
            AssertCompleteSubmission(
"If holidays
    TakeABreak()
End If")
        End Sub
    End Class

End Namespace
