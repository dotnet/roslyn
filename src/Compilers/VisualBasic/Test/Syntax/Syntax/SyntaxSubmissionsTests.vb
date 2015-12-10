' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class SyntaxSubmissionsTests
        Public Shared Sub AssertCompleteSubmission(code As String, hasErrors As Boolean)
            Dim tree = SyntaxFactory.ParseSyntaxTree(code, options:=TestOptions.Script)

            Assert.True(SyntaxFactory.IsCompleteSubmission(tree))

            Dim compilation = DirectCast(tree.GetRoot(), CompilationUnitSyntax)
            Assert.Equal(hasErrors, compilation.HasErrors)
        End Sub

        Public Shared Sub AssertValidCompleteSubmission(code As String)
            AssertCompleteSubmission(code, hasErrors:=False)
        End Sub

        Public Shared Sub AssertInvalidCompleteSubmission(code As String)
            ' Invalid submissions (with compile errors) are treated as complete submissions.
            AssertCompleteSubmission(code, hasErrors:=True)
        End Sub

        Public Shared Sub AssertIncompleteSubmission(code As String)
            Assert.False(SyntaxFactory.IsCompleteSubmission(SyntaxFactory.ParseSyntaxTree(code, options:=TestOptions.Script)))
        End Sub

        <Fact>
        Public Sub TestCompleteSubmission()
            Assert.Throws(Of ArgumentNullException)(Function() SyntaxFactory.IsCompleteSubmission(
                Nothing))
            Assert.Throws(Of ArgumentException)(Function() SyntaxFactory.IsCompleteSubmission(
                SyntaxFactory.ParseSyntaxTree("Dim x = 12", options:=TestOptions.Regular)))

            ' Basic submissions
            AssertValidCompleteSubmission("")
            AssertValidCompleteSubmission("'comment")
            AssertValidCompleteSubmission("Dim x = 12")
            AssertInvalidCompleteSubmission("Dim x y = 12")
            AssertInvalidCompleteSubmission("Dim x = 1 Dim y = 2")
            AssertValidCompleteSubmission("Dim x = 1: Dim y = 2")
            AssertIncompleteSubmission("Dim x =")
            AssertIncompleteSubmission("Dim x = ""str"" & ")
            AssertIncompleteSubmission("Dim x = 12 _")
            AssertValidCompleteSubmission(
"Dim x =
    12")
            AssertValidCompleteSubmission(
"Dim x = 12 _
    + 2")
            AssertInvalidCompleteSubmission(
"Dim x = _
    & ""hello""")

            ' Annotations
            AssertIncompleteSubmission("<AttributeUsage(AttributeTargets.All)>")
            AssertIncompleteSubmission(
"<AttributeUsage(AttributeTargets.All)>
<AttributeUsage(AttributeTargets.All)>")
            AssertIncompleteSubmission(
"<AttributeUsage(
    AttributeTargets.All)>")
            AssertValidCompleteSubmission(
"<AttributeUsage(
    AttributeTargets.All)>
Class C
End Class")

            ' Xml literals
            AssertValidCompleteSubmission("Dim xml = <xml></xml>")
            AssertIncompleteSubmission(
"Dim xml = <xml>
more text")

            'Array literals
            AssertValidCompleteSubmission("Dim arr = New Integer() { 1, 2, 3, 4, 5 }")
            AssertValidCompleteSubmission(
"Dim arr = New Integer() {
    1, 2, 3, 4, 5 }")
            AssertIncompleteSubmission("Dim arr = New Integer() { ")

            ' Method calls
            AssertValidCompleteSubmission("Console.WriteLine(10)")
            AssertInvalidCompleteSubmission("Console.WriteLine(10) Console.WriteLine(10)")
            AssertInvalidCompleteSubmission("Console.WriteLine(1+)")
            AssertIncompleteSubmission("Console.WriteLine(")

            ' Method definitions
            AssertIncompleteSubmission("Sub Main()")
            AssertValidCompleteSubmission(
"Sub Main()
End Sub")
            AssertInvalidCompleteSubmission(
"Sub Sub Main()
End Sub")

            ' Class definitions
            AssertIncompleteSubmission("Class C")
            AssertValidCompleteSubmission(
"Class C
End Class")
            AssertInvalidCompleteSubmission(
"Class C
    Sub Main()
End Class")
            AssertValidCompleteSubmission(
"Class C
    Sub Main()
    End Sub
End Class")

            ' Directives
            AssertValidCompleteSubmission(
"#If somestatement
#End If")
            AssertValidCompleteSubmission(
"#Region ""r""
#End Region")
            AssertIncompleteSubmission("#if somestatement")
            AssertIncompleteSubmission("#region ""r""")
            AssertIncompleteSubmission(
"Sub Main()
#If somestatement
End Sub")
            AssertIncompleteSubmission(
"Sub Main()
#Region ""r""
End Sub")

            ' Try statement
            AssertIncompleteSubmission("Try")
            AssertIncompleteSubmission(
"Try
    Console.WriteLine(10)")
            AssertValidCompleteSubmission(
"Try
    Console.WriteLine(10)
End Try")
            AssertIncompleteSubmission(
"Try
Catch exception")
            AssertIncompleteSubmission(
"Try
Catch exception")

            ' Loop statements
            AssertIncompleteSubmission("Do")
            AssertIncompleteSubmission("Do While condition")
            AssertValidCompleteSubmission(
"Do
Loop")
            AssertValidCompleteSubmission(
"For x = 1 to 10
    For y = 1 to 10
Next y, x")
            AssertIncompleteSubmission(
"For x = 1 to 10
    For y = 1 to 10
Next")

            ' If statement

            AssertIncompleteSubmission("If something Then")
            AssertInvalidCompleteSubmission("If holidays TakeABreak()")
            AssertValidCompleteSubmission("If holidays Then TakeABreak()")
            AssertIncompleteSubmission("If something")
            AssertValidCompleteSubmission(
"If holidays
    TakeABreak()
End If")

            ' LINQ queries
            AssertIncompleteSubmission("Dim x = FROM x In {1, 2, 3}")
            AssertIncompleteSubmission(
"Dim x = FROM x In {1, 2, 3}
         WHERE x > 1")
            AssertIncompleteSubmission(
"Dim x = FROM x In {1, 2, 3}
         WHERE x > 1
         SELECT x + 1")
            AssertValidCompleteSubmission(
"Dim x = FROM x In {1, 2, 3}
         WHERE x > 1

")
            AssertValidCompleteSubmission(
"Dim x = FROM x In {1, 2, 3}
         WHERE x > 1
         SELECT x + 1

")

            ' String interpolation
            AssertValidCompleteSubmission("Dim s = $""{name}""")
        End Sub
    End Class

End Namespace
