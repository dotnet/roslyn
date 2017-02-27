' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Global.Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Namespace Syntax.Submissions
        Public MustInherit Class Syntax_Submissions_TestBase
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
            Public Overridable Sub Complete(Code As String)
                AssertValidCompleteSubmission(Code)
            End Sub
            Public Overridable Sub Incomplete(Code As String)
                AssertIncompleteSubmission(Code)
            End Sub
            Public Overridable Sub Invalid(Code As String)
                AssertInvalidCompleteSubmission(Code)
            End Sub
        End Class

#Region "Theory: Basic Subissons"
        Public Class Basic_Submissions : Inherits Syntax_Submissions_TestBase
            <Theory,
     InlineData(""),
     InlineData("'comment"),
     InlineData("Dim x = 12"),
     InlineData("Dim x = 1: Dim y = 2"),
     InlineData("Dim x =
    12"),
     InlineData("Dim x = 12 _
    + 2")>
            Public Overrides Sub Complete(Code As String)
                MyBase.Complete(Code)
            End Sub

            <Theory,
InlineData("Dim x y = 12"),
InlineData("Dim x = 1 Dim y = 2"),
InlineData("Dim x = _
    & ""hello""")>
            Public Overrides Sub Invalid(CodeAnalysis As String)
                MyBase.Invalid(CodeAnalysis)
            End Sub

            <Theory,
        InlineData("Dim x ="),
        InlineData("Dim x = ""str"" & "),
        InlineData("Dim x = 12 _")> Public Overrides Sub Incomplete(Code As String)
                MyBase.Incomplete(Code)
            End Sub

        End Class
#End Region

#Region "Theory: Attributes"
        Public Class Attributes : Inherits Syntax_Submissions_TestBase
            <Theory,
    InlineData("<AttributeUsage(
    AttributeTargets.All)>
Class C
End Class")>
            Public Overrides Sub Complete(Code As String)
                MyBase.Complete(Code)
            End Sub

            <Theory,
    InlineData("<AttributeUsage(AttributeTargets.All)>"),
    InlineData("<AttributeUsage(AttributeTargets.All)>
<AttributeUsage(AttributeTargets.All)>"),
    InlineData("<AttributeUsage(
    AttributeTargets.All)>")>
            Public Overrides Sub Incomplete(Code As String)
                MyBase.Incomplete(Code)
            End Sub
        End Class
#End Region

#Region "Theory: XML Literals"
        Public Class XML_Literals : Inherits Syntax_Submissions_TestBase
            <Theory, InlineData("Dim xml = <xml></xml>")>
            Public Overrides Sub Complete(Code As String)
                MyBase.Complete(Code)
            End Sub
            <Theory,
    InlineData("Dim xml = <xml>
more text")>
            Public Overrides Sub Incomplete(Code As String)
                MyBase.Incomplete(Code)
            End Sub
            Private Sub Theory_IncompleteSubmission(Valid As Boolean, Code As String)
                AssertIncompleteSubmission(Code)
            End Sub
        End Class
#End Region

#Region "Theory: Array Literals"
        Public Class Array_Literals : Inherits Syntax_Submissions_TestBase
            <Theory,
InlineData("Dim arr = New Integer() { 1, 2, 3, 4, 5 }"),
InlineData("Dim arr = New Integer() {
    1, 2, 3, 4, 5 }")>
            Public Overrides Sub Complete(Code As String)
                MyBase.Complete(Code)
            End Sub
            <Theory, InlineData("Dim arr = New Integer() { ")>
            Public Overrides Sub Incomplete(Code As String)
                MyBase.Incomplete(Code)
            End Sub
        End Class
#End Region

#Region "Theory: Method Calls"
        Public Class Method_Calls : Inherits Syntax_Submissions_TestBase
            <Theory, InlineData("Console.WriteLine(10)")>
            Public Overrides Sub Complete(Code As String)
                MyBase.Complete(Code)
            End Sub

            <Theory, InlineData("Console.WriteLine(")>
            Public Overrides Sub Incomplete(Code As String)
                MyBase.Incomplete(Code)
            End Sub

            <Theory, InlineData("Console.WriteLine(10) Console.WriteLine(10)"), InlineData("Console.WriteLine(1+)")>
            Public Overrides Sub Invalid(Code As String)
                MyBase.Invalid(Code)
            End Sub

        End Class
#End Region

#Region "Theory: Method Definitions"
        Public Class Method_Definitions : Inherits Syntax_Submissions_TestBase
            <Theory, InlineData("Sub Sub Main()
End Sub")>
            Public Overrides Sub Invalid(Code As String)
                MyBase.Invalid(Code)
            End Sub
            <Theory, InlineData("Sub Main()")>
            Public Overrides Sub Incomplete(Code As String)
                MyBase.Incomplete(Code)
            End Sub
            <Theory, InlineData("Sub Main()
                    End Sub")>
            Public Overrides Sub Complete(Code As String)
                MyBase.Complete(Code)
            End Sub

        End Class
#End Region

#Region "Theory: Class Definitions"
        Public Class Class_Definitions : Inherits Syntax_Submissions_TestBase
            <Theory,
    InlineData("Class C
        End Class"),
    InlineData("Class C
                        Sub Main()
                        End Sub
                    End Class"),
    InlineData("Class C
End Class"),
    InlineData("Class C
    Sub Main()
    End Sub
End Class")>
            Public Overrides Sub Complete(Code As String)
                MyBase.Complete(Code)
            End Sub
            <Theory, InlineData("Class C")>
            Public Overrides Sub Incomplete(Code As String)
                MyBase.Incomplete(Code)
            End Sub
            <Theory, InlineData("Class C
    Sub Main()
End Class")>
            Public Overrides Sub Invalid(Code As String)
                MyBase.Invalid(Code)
            End Sub

        End Class
#End Region

#Region "Theory: Directives"
        Public Class Directives : Inherits Syntax_Submissions_TestBase
            <Theory,
    InlineData("#If somestatement
                    #End If"),
    InlineData("#Region ""r""
                    #End Region")>
            Public Overrides Sub Complete(Code As String)
                MyBase.Complete(Code)
            End Sub
            <Theory,
                InlineData("#if somestatement"),
                InlineData("#region ""r"""),
                InlineData("Sub Main()
#If somestatement
End Sub"),
                InlineData("Sub Main()
#Region ""r""
End Sub")>
            Public Overrides Sub Incomplete(Code As String)
                MyBase.Incomplete(Code)
            End Sub

        End Class
#End Region

#Region "Theory: Try Statements"
        Public Class Try_Statements : Inherits Syntax_Submissions_TestBase
            <Theory,
                InlineData("Try"),
                InlineData("Try
    Console.WriteLine(10)"),
                InlineData("Try
Catch exception"),
                InlineData("Try
Catch exception")>
            Public Overrides Sub Incomplete(Code As String)
                MyBase.Incomplete(Code)
            End Sub
            <Theory, InlineData("Try
                    Console.WriteLine(10)
                End Try")>
            Public Overrides Sub Complete(Code As String)
                MyBase.Complete(Code)
            End Sub

        End Class
#End Region

#Region "Theory: Loop Statements"
        Public Class Loop_Statements : Inherits Syntax_Submissions_TestBase
            <Theory,
    InlineData("Do
                    Loop"),
    InlineData("For x = 1 to 10
                        For y = 1 to 10
                    Next y, x")>
            Public Overrides Sub Complete(Code As String)
                MyBase.Complete(Code)
            End Sub
            <Theory,
InlineData("Do"),
InlineData("Do While condition"),
InlineData("For x = 1 to 10
    For y = 1 to 10
Next")>
            Public Overrides Sub Incomplete(Code As String)
                MyBase.Incomplete(Code)
            End Sub

        End Class
#End Region

#Region "Theory: If Statements"
        Public Class If_Statements : Inherits Syntax_Submissions_TestBase
            <Theory,
InlineData("If holidays
                        TakeABreak()
                    End If"),
InlineData("If holidays Then TakeABreak()")>
            Public Overrides Sub Complete(Code As String)
                MyBase.Complete(Code)
            End Sub
            <Theory, InlineData("If something Then"), InlineData("If something")>
            Public Overrides Sub Incomplete(Code As String)
                MyBase.Incomplete(Code)
            End Sub

            <Theory, InlineData("If holidays TakeABreak()")>
            Public Overrides Sub Invalid(Code As String)
                MyBase.Invalid(Code)
            End Sub

        End Class
#End Region

#Region "Theory: LINQ Queries"
        Public Class LINQ_Queries : Inherits Syntax_Submissions_TestBase
            <Theory, InlineData("Dim x = FROM x In {1, 2, 3}
         WHERE x > 1

"), InlineData("Dim x = FROM x In {1, 2, 3}
         WHERE x > 1
         SELECT x + 1

")>
            Public Overrides Sub Complete(Code As String)
                MyBase.Complete(Code)
            End Sub

            <Theory,
InlineData("Dim x = FROM x In {1, 2, 3}"),
InlineData("Dim x = FROM x In {1, 2, 3}
         WHERE x > 1"),
InlineData("Dim x = FROM x In {1, 2, 3}
         WHERE x > 1
         SELECT x + 1")>
            Public Overrides Sub Incomplete(Code As String)
                MyBase.Incomplete(Code)
            End Sub

        End Class
#End Region

#Region "Theory: String Interpolation"
        Public Class String_Interpolation : Inherits Syntax_Submissions_TestBase
            <Theory, InlineData("Dim s = $""{name}""")>
            Public Overrides Sub Complete(Code As String)
                MyBase.Complete(Code)
            End Sub
        End Class
#End Region

        Public Class SyntaxSubmissionsTests
#If False Then
#Region "Theory: "
                Public Class __ : Inherits Syntax_Submissions_TestBase
                    <Theory,
        InlineData(),
        InlineData(),
        InlineData(),
        InlineData(),
        InlineData(),
        InlineData(),
        InlineData()>
                    Public Overrides Sub Complete(Code As String)
                        MyBase.Complete(Code)
                    End Sub
                End Class
#End Region
#End If
            <Fact>
            Public Sub TestCompleteSubmission()
                Assert.Throws(Of ArgumentNullException)(Function() SyntaxFactory.IsCompleteSubmission(Nothing))
                Assert.Throws(Of ArgumentException)(Function() SyntaxFactory.IsCompleteSubmission(SyntaxFactory.ParseSyntaxTree("Dim x = 12", options:=TestOptions.Regular)))
            End Sub
        End Class

    End Namespace
End Namespace
