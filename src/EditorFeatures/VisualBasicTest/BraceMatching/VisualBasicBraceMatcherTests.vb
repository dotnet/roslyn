' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.BraceMatching
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.BraceMatching
    Public Class VisualBasicBraceMatcherTests
        Inherits AbstractBraceMatcherTests

        Protected Overrides Function CreateWorkspaceFromCode(code As String) As TestWorkspace
            Return VisualBasicWorkspaceFactory.CreateWorkspaceFromLines(code)
        End Function

        Private Sub TestInClass(code As String, expectedCode As String)
            Test(
                "Class C" & vbCrLf & code & vbCrLf & "End Class",
                "Class C" & vbCrLf & expectedCode & vbCrLf & "End Class")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub TestEmptyFile()
            Dim code = "$$"
            Dim expected = ""

            Test(code, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub TestAtFirstPositionInFile()
            Dim code = "$$Class C" & vbCrLf & vbCrLf & "End Class"
            Dim expected = "Class C" & vbCrLf & vbCrLf & "End Class"

            Test(code, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub TestAtLastPositionInFile()
            Dim code = "Class C" & vbCrLf & vbCrLf & "End Class$$"
            Dim expected = "Class C" & vbCrLf & vbCrLf & "End Class"

            Test(code, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub TestCurlyBrace1()
            Dim code = "Dim l As New List(Of Integer) From $${}"
            Dim expected = "Dim l As New List(Of Integer) From {[|}|]"

            TestInClass(code, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub TestCurlyBrace2()
            Dim code = "Dim l As New List(Of Integer) From {$$}"
            Dim expected = "Dim l As New List(Of Integer) From {[|}|]"

            TestInClass(code, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub TestCurlyBrace3()
            Dim code = "Dim l As New List(Of Integer) From {$$ }"
            Dim expected = "Dim l As New List(Of Integer) From { [|}|]"

            TestInClass(code, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub TestCurlyBrace4()
            Dim code = "Dim l As New List(Of Integer) From { $$}"
            Dim expected = "Dim l As New List(Of Integer) From [|{|] }"

            TestInClass(code, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub TestCurlyBrace5()
            Dim code = "Dim l As New List(Of Integer) From { }$$"
            Dim expected = "Dim l As New List(Of Integer) From [|{|] }"

            TestInClass(code, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub TestCurlyBrace6()
            Dim code = "Dim l As New List(Of Integer) From {}$$"
            Dim expected = "Dim l As New List(Of Integer) From [|{|]}"

            TestInClass(code, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub TestNestedParen1()
            Dim code = "Dim l As New List$$(Of Func(Of Integer))"
            Dim expected = "Dim l As New List(Of Func(Of Integer)[|)|]"

            TestInClass(code, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub TestNestedParen2()
            Dim code = "Dim l As New List($$Of Func(Of Integer))"
            Dim expected = "Dim l As New List(Of Func(Of Integer)[|)|]"

            TestInClass(code, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub TestNestedParen3()
            Dim code = "Dim l As New List(Of Func$$(Of Integer))"
            Dim expected = "Dim l As New List(Of Func(Of Integer[|)|])"

            TestInClass(code, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub TestNestedParen4()
            Dim code = "Dim l As New List(Of Func($$Of Integer))"
            Dim expected = "Dim l As New List(Of Func(Of Integer[|)|])"

            TestInClass(code, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub TestNestedParen5()
            Dim code = "Dim l As New List(Of Func(Of Integer$$))"
            Dim expected = "Dim l As New List(Of Func[|(|]Of Integer))"

            TestInClass(code, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub TestNestedParen6()
            Dim code = "Dim l As New List(Of Func(Of Integer)$$)"
            Dim expected = "Dim l As New List(Of Func[|(|]Of Integer))"

            TestInClass(code, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub TestNestedParen7()
            Dim code = "Dim l As New List(Of Func(Of Integer)$$ )"
            Dim expected = "Dim l As New List(Of Func[|(|]Of Integer) )"

            TestInClass(code, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub TestNestedParen8()
            Dim code = "Dim l As New List(Of Func(Of Integer) $$)"
            Dim expected = "Dim l As New List[|(|]Of Func(Of Integer) )"

            TestInClass(code, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub TestNestedParen9()
            Dim code = "Dim l As New List(Of Func(Of Integer) )$$"
            Dim expected = "Dim l As New List[|(|]Of Func(Of Integer) )"

            TestInClass(code, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub TestNestedParen10()
            Dim code = "Dim l As New List(Of Func(Of Integer))$$"
            Dim expected = "Dim l As New List[|(|]Of Func(Of Integer))"

            TestInClass(code, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub TestAngleBracket1()
            Dim code = "$$<Foo()> Dim i As Integer"
            Dim expected = "<Foo()[|>|] Dim i As Integer"

            TestInClass(code, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub TestAngleBracket2()
            Dim code = "<$$Foo()> Dim i As Integer"
            Dim expected = "<Foo()[|>|] Dim i As Integer"

            TestInClass(code, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub TestAngleBracket3()
            Dim code = "<Foo$$()> Dim i As Integer"
            Dim expected = "<Foo([|)|]> Dim i As Integer"

            TestInClass(code, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub TestAngleBracket4()
            Dim code = "<Foo($$)> Dim i As Integer"
            Dim expected = "<Foo([|)|]> Dim i As Integer"

            TestInClass(code, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub TestAngleBracket5()
            Dim code = "<Foo($$ )> Dim i As Integer"
            Dim expected = "<Foo( [|)|]> Dim i As Integer"

            TestInClass(code, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub TestAngleBracket6()
            Dim code = "<Foo( $$)> Dim i As Integer"
            Dim expected = "<Foo[|(|] )> Dim i As Integer"

            TestInClass(code, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub TestAngleBracket7()
            Dim code = "<Foo( )$$> Dim i As Integer"
            Dim expected = "<Foo[|(|] )> Dim i As Integer"

            TestInClass(code, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub TestAngleBracket8()
            Dim code = "<Foo()$$> Dim i As Integer"
            Dim expected = "<Foo[|(|])> Dim i As Integer"

            TestInClass(code, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub TestAngleBracket9()
            Dim code = "<Foo()$$ > Dim i As Integer"
            Dim expected = "<Foo[|(|]) > Dim i As Integer"

            TestInClass(code, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub TestAngleBracket10()
            Dim code = "<Foo() $$> Dim i As Integer"
            Dim expected = "[|<|]Foo() > Dim i As Integer"

            TestInClass(code, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub TestAngleBracket11()
            Dim code = "<Foo() >$$ Dim i As Integer"
            Dim expected = "[|<|]Foo() > Dim i As Integer"

            TestInClass(code, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub TestAngleBracket12()
            Dim code = "<Foo()>$$ Dim i As Integer"
            Dim expected = "[|<|]Foo()> Dim i As Integer"

            TestInClass(code, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub TestString1()
            Dim code = "Dim s As String = $$""Foo"""
            Dim expected = "Dim s As String = ""Foo[|""|]"

            TestInClass(code, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub TestString2()
            Dim code = "Dim s As String = ""$$Foo"""
            Dim expected = "Dim s As String = ""Foo[|""|]"

            TestInClass(code, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub TestString3()
            Dim code = "Dim s As String = ""Foo$$"""
            Dim expected = "Dim s As String = [|""|]Foo"""

            TestInClass(code, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub TestString4()
            Dim code = "Dim s As String = ""Foo""$$"
            Dim expected = "Dim s As String = [|""|]Foo"""

            TestInClass(code, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub TestString5()
            Dim code = "Dim s As String = ""Foo$$"
            Dim expected = "Dim s As String = ""Foo"

            TestInClass(code, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub TestInterpolatedString1()
            Dim code = "Dim s = $$[||]$""Foo"""
            Dim expected = "Dim s = $""Foo[|""|]"

            TestInClass(code, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub TestInterpolatedString2()
            Dim code = "Dim s = $""$$Foo"""
            Dim expected = "Dim s = $""Foo[|""|]"

            TestInClass(code, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub TestInterpolatedString3()
            Dim code = "Dim s = $""Foo$$"""
            Dim expected = "Dim s = [|$""|]Foo"""

            TestInClass(code, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub TestInterpolatedString4()
            Dim code = "Dim s = $""Foo""$$"
            Dim expected = "Dim s = [|$""|]Foo"""

            TestInClass(code, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub TestInterpolatedString5()
            Dim code = "Dim s = $"" $${x} """
            Dim expected = "Dim s = $"" {x[|}|] """

            TestInClass(code, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub TestInterpolatedString6()
            Dim code = "Dim s = $"" {$$x} """
            Dim expected = "Dim s = $"" {x[|}|] """

            TestInClass(code, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub TestInterpolatedString7()
            Dim code = "Dim s = $"" {x$$} """
            Dim expected = "Dim s = $"" [|{|]x} """

            TestInClass(code, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub TestInterpolatedString8()
            Dim code = "Dim s = $"" {x}$$ """
            Dim expected = "Dim s = $"" [|{|]x} """

            TestInClass(code, expected)
        End Sub

    End Class
End Namespace
