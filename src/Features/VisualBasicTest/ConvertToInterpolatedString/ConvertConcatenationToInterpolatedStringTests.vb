' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports VerifyVB = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.VisualBasicCodeRefactoringVerifier(Of
    Microsoft.CodeAnalysis.VisualBasic.ConvertToInterpolatedString.VisualBasicConvertConcatenationToInterpolatedStringRefactoringProvider)

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ConvertToInterpolatedString
    <UseExportProvider>
    <Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
    Public Class ConvertConcatenationToInterpolatedStringTests
        <Fact>
        Public Async Function TestMissingOnSimpleString() As Task
            Dim code = "
Public Class C
    Sub M()
        dim v = [||]""string""
    End Sub
End Class"

            Await VerifyVB.VerifyRefactoringAsync(code, code)
        End Function

        <Fact>
        Public Async Function TestWithStringOnLeft() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"
Public Class C
    Sub M()
        dim v = [||]""string"" & 1
    End Sub
End Class",
"
Public Class C
    Sub M()
        dim v = $""string{1}""
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestRightSideOfString() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"
Public Class C
    Sub M()
        dim v = ""string""[||] & 1
    End Sub
End Class",
"
Public Class C
    Sub M()
        dim v = $""string{1}""
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestWithStringOnRight() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"
Public Class C
    Sub M()
        dim v = 1 & [||]""string""
    End Sub
End Class",
"
Public Class C
    Sub M()
        dim v = $""{1}string""
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestWithComplexExpressionOnLeft() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"
Public Class C
    Sub M()
        dim v = 1 + 2 & [||]""string""
    End Sub
End Class",
"
Public Class C
    Sub M()
        dim v = $""{1 + 2}string""
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestWithTrivia1() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"
Public Class C
    Sub M()
        dim v = 1 + 2 & [||]""string"" ' trailing trivia
    End Sub
End Class",
"
Public Class C
    Sub M()
        dim v = $""{1 + 2}string"" ' trailing trivia
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestWithComplexExpressions() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"
Public Class C
    Sub M()
        dim v = 1 + 2 & [||]""string"" & 3 & 4
    End Sub
End Class",
"
Public Class C
    Sub M()
        dim v = $""{1 + 2}string{3}{4}""
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestWithEscapes1() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"
Public Class C
    Sub M()
        dim v = ""\r"" & 2 & [||]""string"" & 3 & ""\n""
    End Sub
End Class",
"
Public Class C
    Sub M()
        dim v = $""\r{2}string{3}\n""
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestWithEscapes2() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"
Public Class C
    Sub M()
        dim v = ""\\r"" & 2 & [||]""string"" & 3 & ""\\n""
    End Sub
End Class",
"
Public Class C
    Sub M()
        dim v = $""\\r{2}string{3}\\n""
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestWithOverloadedOperator() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"
public class D
    public shared operator &(d As D, s As String) as boolean
        Return False
    end operator
    public shared operator &(s As String, d As D) as boolean
        Return False
    end operator
end class

Public Class C
    Sub M()
        dim d as D = nothing
        dim v = 1 & [||]""string"" & d
    End Sub
End Class",
"
public class D
    public shared operator &(d As D, s As String) as boolean
        Return False
    end operator
    public shared operator &(s As String, d As D) as boolean
        Return False
    end operator
end class

Public Class C
    Sub M()
        dim d as D = nothing
        dim v = $""{1}string"" & d
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestWithOverloadedOperator2() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"
public class D
    public shared operator &(d As D, s As String) as boolean
        Return False
    end operator
    public shared operator &(s As String, d As D) as boolean
        Return False
    end operator
end class

Public Class C
    Sub M()
        dim d as D = nothing
        dim v = d & [||]""string"" & 1
    End Sub
End Class",
"
public class D
    public shared operator &(d As D, s As String) as boolean
        Return False
    end operator
    public shared operator &(s As String, d As D) as boolean
        Return False
    end operator
end class

Public Class C
    Sub M()
        dim d as D = nothing
        dim v = $""{d & ""string""}{1}""
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16820")>
        Public Async Function TestWithMultipleStringConcatinations() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"
Public Class C
    Sub M()
        dim v = ""A"" & 1 & [||]""B"" & ""C""
    End Sub
End Class",
"
Public Class C
    Sub M()
        dim v = $""A{1}BC""
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16820")>
        Public Async Function TestWithMultipleStringConcatinations2() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"
Public Class C
    Sub M()
        dim v = ""A"" & [||]""B"" & ""C"" & 1
    End Sub
End Class",
"
Public Class C
    Sub M()
        dim v = $""ABC{1}""
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16820")>
        Public Async Function TestWithMultipleStringConcatinations3() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"
Public Class C
    Sub M()
        dim v = ""A"" & 1 & [||]""B"" & ""C"" & 2 & ""D"" & ""E"" & ""F"" & 3  
    End Sub
End Class",
"
Public Class C
    Sub M()
        dim v = $""A{1}BC{2}DEF{3}""
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23536")>
        Public Async Function TestWithStringLiteralWithBraces() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"
Public Class C
    Sub M()
        dim v = 1 & [||]""{string}""
    End Sub
End Class",
"
Public Class C
    Sub M()
        dim v = $""{1}{{string}}""
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23536")>
        Public Async Function TestWithStringLiteralWithDoubleBraces() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"
Public Class C
    Sub M()
        dim v = 1 & [||]""{{string}}""
    End Sub
End Class",
"
Public Class C
    Sub M()
        dim v = $""{1}{{{{string}}}}""
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23536")>
        Public Async Function TestWithMultipleStringLiteralsWithBraces() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"
Public Class C
    Sub M()
        dim v = ""{"" & 1 & [||]""}""
    End Sub
End Class",
"
Public Class C
    Sub M()
        dim v = $""{{{1}}}""
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16981")>
        <WorkItem("https://github.com/dotnet/roslyn/issues/35525")>
        Public Async Function TestWithSelectionOnEntireToBeInterpolatedString() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"
Public Class C
    Sub M()
        dim v = [|""string"" & 1|]
    End Sub
End Class",
"
Public Class C
    Sub M()
        dim v = $""string{1}""
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16981")>
        Public Async Function TestMissingWithSelectionOnPartOfToBeInterpolatedString() As Task
            Dim code = "
Public Class C
    Sub M()
        dim v = [|""string"" & 1|] & ""string""
    End Sub
End Class"

            Await VerifyVB.VerifyRefactoringAsync(code, code)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16981")>
        <WorkItem("https://github.com/dotnet/roslyn/issues/35525")>
        Public Async Function TestWithSelectionExceedingToBeInterpolatedString() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"
Public Class C
    Sub M()
        [|dim v = ""string"" & 1|]
    End Sub
End Class",
"
Public Class C
    Sub M()
        dim v = $""string{1}""
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16981")>
        Public Async Function TestWithCaretBeforeNonStringToken() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"
Public Class C
    Sub M()
        dim v = [||]3 & ""string"" & 1 & ""string""
    End Sub
End Class",
"
Public Class C
    Sub M()
        dim v = $""{3}string{1}string""
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16981")>
        Public Async Function TestWithCaretAfterNonStringToken() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"
Public Class C
    Sub M()
        dim v = 3[||] & ""string"" & 1 & ""string""
    End Sub
End Class",
"
Public Class C
    Sub M()
        dim v = $""{3}string{1}string""
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16981")>
        Public Async Function TestWithCaretBeforeAmpersandToken() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"
Public Class C
    Sub M()
        dim v = 3 [||]& ""string"" & 1 & ""string""
    End Sub
End Class",
"
Public Class C
    Sub M()
        dim v = $""{3}string{1}string""
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16981")>
        Public Async Function TestWithCaretAfterAmpersandToken() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"
Public Class C
    Sub M()
        dim v = 3 &[||] ""string"" & 1 & ""string""
    End Sub
End Class",
"
Public Class C
    Sub M()
        dim v = $""{3}string{1}string""
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16981")>
        Public Async Function TestWithCaretBeforeLastAmpersandToken() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"
Public Class C
    Sub M()
        dim v = 3 & ""string"" & 1 [||]& ""string""
    End Sub
End Class",
"
Public Class C
    Sub M()
        dim v = $""{3}string{1}string""
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16981")>
        Public Async Function TestWithCaretAfterLastAmpersandToken() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"
Public Class C
    Sub M()
        dim v = 3 & ""string"" & 1 &[||] ""string""
    End Sub
End Class",
"
Public Class C
    Sub M()
        dim v = $""{3}string{1}string""
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37324")>
        Public Async Function TestConcatenationWithChar() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"
Public Class C
    Private Sub M()
        Dim hello = ""hello""
        Dim world = ""world""
        Dim str = hello [||]& "" ""c & world
    End Sub
End Class",
"
Public Class C
    Private Sub M()
        Dim hello = ""hello""
        Dim world = ""world""
        Dim str = $""{hello} {world}""
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37324")>
        Public Async Function TestConcatenationWithCharAfterStringLiteral() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"
Public Class C
    Private Sub M()
        Dim world = ""world""
        Dim str = ""hello"" [||]& "" ""c & world
    End Sub
End Class",
"
Public Class C
    Private Sub M()
        Dim world = ""world""
        Dim str = $""hello {world}""
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37324")>
        Public Async Function TestConcatenationWithCharBeforeStringLiteral() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"
Public Class C
    Private Sub M()
        Dim hello = ""hello""
        Dim str = hello [||]& "" ""c & ""world""
    End Sub
End Class",
"
Public Class C
    Private Sub M()
        Dim hello = ""hello""
        Dim str = $""{hello} world""
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40413")>
        Public Async Function TestConcatenationWithConstMember() As Task
            Dim code = "
Public Class C
    Private Const Hello As String = ""Hello""
    Private Const World As String = ""World""
    Private Const Message As String = Hello + "" "" + World[||]
End Class"

            Await VerifyVB.VerifyRefactoringAsync(code, code)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40413")>
        Public Async Function TestConcatenationWithConstDeclaration() As Task
            Dim code = "
Public Class C
    Private Sub M()
        Const Hello As String = ""Hello""
        Const World As String = ""World""
        Const Message As String = Hello + "" "" + World[||]
    End Sub
End Class"

            Await VerifyVB.VerifyRefactoringAsync(code, code)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40413")>
        Public Async Function TestConcatenationWithInlineString() As Task
            Await VerifyVB.VerifyRefactoringAsync("
Imports System
Public Class C
    Private Sub M()
        Const Hello As String = ""Hello""
        Const World As String = ""World""
        Console.WriteLine(Hello + "" "" + World[||])
    End Sub
End Class", "
Imports System
Public Class C
    Private Sub M()
        Const Hello As String = ""Hello""
        Const World As String = ""World""
        Console.WriteLine($""{Hello} {World}"")
    End Sub
End Class")
        End Function

        <Theory, WorkItem("https://github.com/dotnet/roslyn/issues/49229")>
        <InlineData("[|""a"" + $""{1:000}""|]", "$""a{1:000}""")>
        <InlineData("[|""a"" + $""b{1:000}""|]", "$""ab{1:000}""")>
        <InlineData("[|$""a{1:000}"" + ""b""|]", "$""a{1:000}b""")>
        <InlineData("[|""a"" + $""b{1:000}c"" + ""d""|]", "$""ab{1:000}cd""")>
        <InlineData("[|""a"" + $""{1:000}b"" + ""c""|]", "$""a{1:000}bc""")>
        <InlineData("[|""a"" + $""{1:000}"" + $""{2:000}"" + ""b""|]", "$""a{1:000}{2:000}b""")>
        Public Async Function TestInliningOfInterpolatedString(ByVal before As String, ByVal after As String) As Task
            Dim initialMarkup = $"
Public Class C
    Private Sub M()
        Dim s = {before}
    End Sub
End Class"
            Dim expected = $"
Public Class C
    Private Sub M()
        Dim s = {after}
    End Sub
End Class"
            Await VerifyVB.VerifyRefactoringAsync(initialMarkup, expected)
        End Function
    End Class
End Namespace
