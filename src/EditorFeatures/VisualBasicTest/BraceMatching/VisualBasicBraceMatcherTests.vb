' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.UnitTests.BraceMatching
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.BraceMatching
    Public Class VisualBasicBraceMatcherTests
        Inherits AbstractBraceMatcherTests

        Protected Overrides Function CreateWorkspaceFromCodeAsync(code As String) As Task(Of TestWorkspace)
            Return TestWorkspace.CreateVisualBasicAsync(code)
        End Function

        Private Async Function TestInClassAsync(code As String, expectedCode As String) As Task
            Await TestAsync(
                "Class C" & vbCrLf & code & vbCrLf & "End Class",
                "Class C" & vbCrLf & expectedCode & vbCrLf & "End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestEmptyFile() As Task
            Dim code = "$$"
            Dim expected = ""

            Await TestAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestAtFirstPositionInFile() As Task
            Dim code = "$$Class C" & vbCrLf & vbCrLf & "End Class"
            Dim expected = "Class C" & vbCrLf & vbCrLf & "End Class"

            Await TestAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestAtLastPositionInFile() As Task
            Dim code = "Class C" & vbCrLf & vbCrLf & "End Class$$"
            Dim expected = "Class C" & vbCrLf & vbCrLf & "End Class"

            Await TestAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestCurlyBrace1() As Task
            Dim code = "Dim l As New List(Of Integer) From $${}"
            Dim expected = "Dim l As New List(Of Integer) From {[|}|]"

            Await TestInClassAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestCurlyBrace2() As Task
            Dim code = "Dim l As New List(Of Integer) From {$$}"
            Dim expected = "Dim l As New List(Of Integer) From {[|}|]"

            Await TestInClassAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestCurlyBrace3() As Task
            Dim code = "Dim l As New List(Of Integer) From {$$ }"
            Dim expected = "Dim l As New List(Of Integer) From { [|}|]"

            Await TestInClassAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestCurlyBrace4() As Task
            Dim code = "Dim l As New List(Of Integer) From { $$}"
            Dim expected = "Dim l As New List(Of Integer) From [|{|] }"

            Await TestInClassAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestCurlyBrace5() As Task
            Dim code = "Dim l As New List(Of Integer) From { }$$"
            Dim expected = "Dim l As New List(Of Integer) From [|{|] }"

            Await TestInClassAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestCurlyBrace6() As Task
            Dim code = "Dim l As New List(Of Integer) From {}$$"
            Dim expected = "Dim l As New List(Of Integer) From [|{|]}"

            Await TestInClassAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestNestedParen1() As Task
            Dim code = "Dim l As New List$$(Of Func(Of Integer))"
            Dim expected = "Dim l As New List(Of Func(Of Integer)[|)|]"

            Await TestInClassAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestNestedParen2() As Task
            Dim code = "Dim l As New List($$Of Func(Of Integer))"
            Dim expected = "Dim l As New List(Of Func(Of Integer)[|)|]"

            Await TestInClassAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestNestedParen3() As Task
            Dim code = "Dim l As New List(Of Func$$(Of Integer))"
            Dim expected = "Dim l As New List(Of Func(Of Integer[|)|])"

            Await TestInClassAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestNestedParen4() As Task
            Dim code = "Dim l As New List(Of Func($$Of Integer))"
            Dim expected = "Dim l As New List(Of Func(Of Integer[|)|])"

            Await TestInClassAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestNestedParen5() As Task
            Dim code = "Dim l As New List(Of Func(Of Integer$$))"
            Dim expected = "Dim l As New List(Of Func[|(|]Of Integer))"

            Await TestInClassAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestNestedParen6() As Task
            Dim code = "Dim l As New List(Of Func(Of Integer)$$)"
            Dim expected = "Dim l As New List(Of Func[|(|]Of Integer))"

            Await TestInClassAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestNestedParen7() As Task
            Dim code = "Dim l As New List(Of Func(Of Integer)$$ )"
            Dim expected = "Dim l As New List(Of Func[|(|]Of Integer) )"

            Await TestInClassAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestNestedParen8() As Task
            Dim code = "Dim l As New List(Of Func(Of Integer) $$)"
            Dim expected = "Dim l As New List[|(|]Of Func(Of Integer) )"

            Await TestInClassAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestNestedParen9() As Task
            Dim code = "Dim l As New List(Of Func(Of Integer) )$$"
            Dim expected = "Dim l As New List[|(|]Of Func(Of Integer) )"

            Await TestInClassAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestNestedParen10() As Task
            Dim code = "Dim l As New List(Of Func(Of Integer))$$"
            Dim expected = "Dim l As New List[|(|]Of Func(Of Integer))"

            Await TestInClassAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestAngleBracket1() As Task
            Dim code = "$$<Foo()> Dim i As Integer"
            Dim expected = "<Foo()[|>|] Dim i As Integer"

            Await TestInClassAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestAngleBracket2() As Task
            Dim code = "<$$Foo()> Dim i As Integer"
            Dim expected = "<Foo()[|>|] Dim i As Integer"

            Await TestInClassAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestAngleBracket3() As Task
            Dim code = "<Foo$$()> Dim i As Integer"
            Dim expected = "<Foo([|)|]> Dim i As Integer"

            Await TestInClassAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestAngleBracket4() As Task
            Dim code = "<Foo($$)> Dim i As Integer"
            Dim expected = "<Foo([|)|]> Dim i As Integer"

            Await TestInClassAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestAngleBracket5() As Task
            Dim code = "<Foo($$ )> Dim i As Integer"
            Dim expected = "<Foo( [|)|]> Dim i As Integer"

            Await TestInClassAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestAngleBracket6() As Task
            Dim code = "<Foo( $$)> Dim i As Integer"
            Dim expected = "<Foo[|(|] )> Dim i As Integer"

            Await TestInClassAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestAngleBracket7() As Task
            Dim code = "<Foo( )$$> Dim i As Integer"
            Dim expected = "<Foo[|(|] )> Dim i As Integer"

            Await TestInClassAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestAngleBracket8() As Task
            Dim code = "<Foo()$$> Dim i As Integer"
            Dim expected = "<Foo[|(|])> Dim i As Integer"

            Await TestInClassAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestAngleBracket9() As Task
            Dim code = "<Foo()$$ > Dim i As Integer"
            Dim expected = "<Foo[|(|]) > Dim i As Integer"

            Await TestInClassAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestAngleBracket10() As Task
            Dim code = "<Foo() $$> Dim i As Integer"
            Dim expected = "[|<|]Foo() > Dim i As Integer"

            Await TestInClassAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestAngleBracket11() As Task
            Dim code = "<Foo() >$$ Dim i As Integer"
            Dim expected = "[|<|]Foo() > Dim i As Integer"

            Await TestInClassAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestAngleBracket12() As Task
            Dim code = "<Foo()>$$ Dim i As Integer"
            Dim expected = "[|<|]Foo()> Dim i As Integer"

            Await TestInClassAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestString1() As Task
            Dim code = "Dim s As String = $$""Foo"""
            Dim expected = "Dim s As String = ""Foo[|""|]"

            Await TestInClassAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestString2() As Task
            Dim code = "Dim s As String = ""$$Foo"""
            Dim expected = "Dim s As String = ""Foo[|""|]"

            Await TestInClassAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestString3() As Task
            Dim code = "Dim s As String = ""Foo$$"""
            Dim expected = "Dim s As String = [|""|]Foo"""

            Await TestInClassAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestString4() As Task
            Dim code = "Dim s As String = ""Foo""$$"
            Dim expected = "Dim s As String = [|""|]Foo"""

            Await TestInClassAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestString5() As Task
            Dim code = "Dim s As String = ""Foo$$"
            Dim expected = "Dim s As String = ""Foo"

            Await TestInClassAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestInterpolatedString1() As Task
            Dim code = "Dim s = $$[||]$""Foo"""
            Dim expected = "Dim s = $""Foo[|""|]"

            Await TestInClassAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestInterpolatedString2() As Task
            Dim code = "Dim s = $""$$Foo"""
            Dim expected = "Dim s = $""Foo[|""|]"

            Await TestInClassAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestInterpolatedString3() As Task
            Dim code = "Dim s = $""Foo$$"""
            Dim expected = "Dim s = [|$""|]Foo"""

            Await TestInClassAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestInterpolatedString4() As Task
            Dim code = "Dim s = $""Foo""$$"
            Dim expected = "Dim s = [|$""|]Foo"""

            Await TestInClassAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestInterpolatedString5() As Task
            Dim code = "Dim s = $"" $${x} """
            Dim expected = "Dim s = $"" {x[|}|] """

            Await TestInClassAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestInterpolatedString6() As Task
            Dim code = "Dim s = $"" {$$x} """
            Dim expected = "Dim s = $"" {x[|}|] """

            Await TestInClassAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestInterpolatedString7() As Task
            Dim code = "Dim s = $"" {x$$} """
            Dim expected = "Dim s = $"" [|{|]x} """

            Await TestInClassAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestInterpolatedString8() As Task
            Dim code = "Dim s = $"" {x}$$ """
            Dim expected = "Dim s = $"" [|{|]x} """

            Await TestInClassAsync(code, expected)
        End Function

        <WorkItem(7120, "https://github.com/dotnet/roslyn/issues/7120")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestConditionalDirectiveWithSingleMatchingDirective() As Task
            Dim code =
<Text>Class C
    Sub Test()
#If$$ CHK Then

#End If
    End Sub
End Class
</Text>.Value.Replace(vbLf, vbCrLf)
            Dim expected =
<Text>Class C
    Sub Test()
#If CHK Then

[|#End If|]
    End Sub
End Class
</Text>.Value.Replace(vbLf, vbCrLf)

            Await TestAsync(code, expected)
        End Function

        <WorkItem(7120, "https://github.com/dotnet/roslyn/issues/7120")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestConditionalDirectiveWithTwoMatchingDirectives() As Task
            Dim code =
<Text>Class C
    Sub Test()
#If$$ CHK Then
#Else
#End If
    End Sub
End Class
</Text>.Value.Replace(vbLf, vbCrLf)
            Dim expected =
<Text>Class C
    Sub Test()
#If CHK Then
[|#Else|]
#End If
    End Sub
End Class
</Text>.Value.Replace(vbLf, vbCrLf)

            Await TestAsync(code, expected)
        End Function

        <WorkItem(7120, "https://github.com/dotnet/roslyn/issues/7120")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestConditionalDirectiveWithAllMatchingDirectives() As Task
            Dim code =
<Text>Class C
    Sub Test()
#If CHK Then
#ElseIf RET Then
#Else
#End If$$
    End Sub
End Class
</Text>.Value.Replace(vbLf, vbCrLf)
            Dim expected =
<Text>Class C
    Sub Test()
[|#If|] CHK Then
#ElseIf RET Then
#Else
#End If
    End Sub
End Class
</Text>.Value.Replace(vbLf, vbCrLf)

            Await TestAsync(code, expected)
        End Function

        <WorkItem(7120, "https://github.com/dotnet/roslyn/issues/7120")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestRegionDirective() As Task
            Dim code =
<Text>Class C
$$#Region "Public Methods"
    Sub Test()
    End Sub
#End Region
End Class
</Text>.Value.Replace(vbLf, vbCrLf)
            Dim expected =
<Text>Class C
#Region "Public Methods"
    Sub Test()
    End Sub
[|#End Region|]
End Class
</Text>.Value.Replace(vbLf, vbCrLf)

            Await TestAsync(code, expected)
        End Function

        <WorkItem(7120, "https://github.com/dotnet/roslyn/issues/7120")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestInterleavedDirectivesInner() As Task
            Dim code =
<Text>#Const CHK = True
Module Program
    Sub Main(args As String())
#If CHK Then
#Region$$ "Public Methods"
        Console.Write(5)
#ElseIf RET Then
        Console.Write(5)
#Else
#End If
    End Sub
#End Region
End Module
</Text>.Value.Replace(vbLf, vbCrLf)
            Dim expected =
<Text>#Const CHK = True
Module Program
    Sub Main(args As String())
#If CHK Then
#Region "Public Methods"
        Console.Write(5)
#ElseIf RET Then
        Console.Write(5)
#Else
#End If
    End Sub
[|#End Region|]
End Module
</Text>.Value.Replace(vbLf, vbCrLf)

            Await TestAsync(code, expected)
        End Function

        <WorkItem(7120, "https://github.com/dotnet/roslyn/issues/7120")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestInterleavedDirectivesOuter() As Task
            Dim code =
<Text>#Const CHK = True
Module Program
    Sub Main(args As String())
#If$$ CHK Then
#Region "Public Methods"
        Console.Write(5)
#ElseIf RET Then
        Console.Write(5)
#Else
#End If
    End Sub
#End Region
End Module
</Text>.Value.Replace(vbLf, vbCrLf)
            Dim expected =
<Text>#Const CHK = True
Module Program
    Sub Main(args As String())
#If CHK Then
#Region "Public Methods"
        Console.Write(5)
[|#ElseIf|] RET Then
        Console.Write(5)
#Else
#End If
    End Sub
#End Region
End Module
</Text>.Value.Replace(vbLf, vbCrLf)

            Await TestAsync(code, expected)
        End Function

        <WorkItem(7120, "https://github.com/dotnet/roslyn/issues/7120")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestUnmatchedDirective1() As Task
            Dim code =
<Text>Class C
$$#Region "Public Methods"
    Sub Test()
    End Sub
End Class
</Text>.Value.Replace(vbLf, vbCrLf)
            Dim expected =
<Text>Class C
#Region "Public Methods"
    Sub Test()

    End Sub
End Class
</Text>.Value.Replace(vbLf, vbCrLf)

            Await TestAsync(code, expected)
        End Function

        <WorkItem(7120, "https://github.com/dotnet/roslyn/issues/7120")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestUnmatchedDirective2() As Task
            Dim code =
<Text>
#Enable Warning$$
Class C
    Sub Test()
    End Sub
End Class
</Text>.Value.Replace(vbLf, vbCrLf)
            Dim expected =
<Text>
#Enable Warning
Class C
    Sub Test()

    End Sub
End Class
</Text>.Value.Replace(vbLf, vbCrLf)

            Await TestAsync(code, expected)
        End Function

        <WorkItem(7534, "https://github.com/dotnet/roslyn/issues/7534")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestUnmatchedIncompleteConditionalDirective() As Task
            Dim code =
<Text>
Class C
    Sub Test()
#If$$
    End Sub
End Class
</Text>.Value.Replace(vbLf, vbCrLf)
            Dim expected =
<Text>
Class C
    Sub Test()
[|#If|]
    End Sub
End Class
</Text>.Value.Replace(vbLf, vbCrLf)

            Await TestAsync(code, expected)
        End Function

        <WorkItem(7534, "https://github.com/dotnet/roslyn/issues/7534")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestUnmatchedCompleteConditionalDirective() As Task
            Dim code =
<Text>
Class C
    Sub Test()
#If$$ CHK Then
    End Sub
End Class
</Text>.Value.Replace(vbLf, vbCrLf)
            Dim expected =
<Text>
Class C
    Sub Test()
[|#If|] CHK Then
    End Sub
End Class
</Text>.Value.Replace(vbLf, vbCrLf)

            Await TestAsync(code, expected)
        End Function

        <WorkItem(7534, "https://github.com/dotnet/roslyn/issues/7534")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Async Function TestUnmatchedConditionalDirective() As Task
            Dim code =
<Text>
Class C
    Sub Test()
#Else$$
    End Sub
End Class
</Text>.Value.Replace(vbLf, vbCrLf)
            Dim expected =
<Text>
Class C
    Sub Test()
[|#Else|]
    End Sub
End Class
</Text>.Value.Replace(vbLf, vbCrLf)

            Await TestAsync(code, expected)
        End Function

    End Class
End Namespace