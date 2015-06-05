' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.InsertMissingCast

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.InsertMissingCast
    Public Class InsertMissingCastTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
            Return New Tuple(Of DiagnosticAnalyzer, CodeFixProvider)(
                Nothing, New InsertMissingCastCodeFixProvider)
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsInsertMissingCast)>
        Public Sub TestPredefinedAssignment()
            Test(
NewLines("Option Strict On \n Module M1 \n Sub Main() \n Dim b As Byte = 1 \n Dim c As Byte = 2 \n b = [|b & c|] \n End Sub \n End Module"),
NewLines("Option Strict On \n Module M1 \n Sub Main() \n Dim b As Byte = 1 \n Dim c As Byte = 2 \n b = CByte(b & c) \n End Sub \n End Module"))
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsInsertMissingCast)>
        Public Sub TestAssignment()
            Test(
NewLines("Option Strict On \n Class Base \n End Class \n Class Derived \n Inherits Base \n End Class \n Module M1 \n Sub Main() \n Dim d As Derived = Nothing \n Dim b As Base = Nothing \n d = [|b|] \n End Sub \n End Module"),
NewLines("Option Strict On \n Class Base \n End Class \n Class Derived \n Inherits Base \n End Class \n Module M1 \n Sub Main() \n Dim d As Derived = Nothing \n Dim b As Base = Nothing \n d = CType(b, Derived) \n End Sub \n End Module"))
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsInsertMissingCast)>
        Public Sub TestMethodCall()
            Test(
NewLines("Option Strict On \n Class Base \n End Class \n Class Derived \n Inherits Base \n End Class \n Module M1 \n Sub foo(d As Derived) \n End Sub \n Sub Main() \n Dim b As Base = Nothing \n foo([|b|]) \n End Sub \n End Module"),
NewLines("Option Strict On \n Class Base \n End Class \n Class Derived \n Inherits Base \n End Class \n Module M1 \n Sub foo(d As Derived) \n End Sub \n Sub Main() \n Dim b As Base = Nothing \n foo(CType(b, Derived)) \n End Sub \n End Module"))
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsInsertMissingCast)>
        Public Sub TestMethodCallPredefined()
            Test(
NewLines("Option Strict On \n Module M1 \n Sub foo(d As Integer) \n End Sub \n Sub Main() \n foo([|""10""|]) \n End Sub \n End Module"),
NewLines("Option Strict On \n Module M1 \n Sub foo(d As Integer) \n End Sub \n Sub Main() \n foo(CInt(""10"")) \n End Sub \n End Module"))
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsInsertMissingCast)>
        Public Sub TestConditional()
            Test(
NewLines("Option Strict On \n Module M1 \n Sub Main() \n Dim i As Integer = 10 \n Dim b = If([|i|], True, False) \n End Sub \n End Module"),
NewLines("Option Strict On \n Module M1 \n Sub Main() \n Dim i As Integer = 10 \n Dim b = If(CBool(i), True, False) \n End Sub \n End Module"))
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsInsertMissingCast)>
        Public Sub TestReturn()
            Test(
NewLines("Option Strict On \n Module M1 \n Function foo() As Integer \n Return [|""10""|] \n End Function \n End Module"),
NewLines("Option Strict On \n Module M1 \n Function foo() As Integer \n Return CInt(""10"") \n End Function \n End Module"))
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsInsertMissingCast)>
        Public Sub TestLambda()
            Test(
NewLines("Option Strict On \n Class Base \n End Class \n Class Derived \n Inherits Base \n End Class \n Module M1 \n Sub Main() \n Dim l = Function() As Derived \n Return [|New Base()|] \n End Function \n End Sub \n End Module"),
NewLines("Option Strict On \n Class Base \n End Class \n Class Derived \n Inherits Base \n End Class \n Module M1 \n Sub Main() \n Dim l = Function() As Derived \n Return CType(New Base(), Derived) \n End Function \n End Sub \n End Module"))
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsInsertMissingCast)>
        Public Sub TestAttribute()
            Test(
NewLines("Option Strict On \n Module Program \n <System.Obsolete(""as"", [|10|])> \n Sub Main(args As String()) \n End Sub \n End Module"),
NewLines("Option Strict On \n Module Program \n <System.Obsolete(""as"", CBool(10))> \n Sub Main(args As String()) \n End Sub \n End Module"))
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsInsertMissingCast)>
        Public Sub TestMultiline()
            Test(
NewLines("Option Strict On \n Module Program \n Sub Main(args As String()) \n Dim x As Integer = [|10.0 + \n 11.0 + \n 10|] ' asas \n End Sub \n End Module"),
NewLines("Option Strict On \n Module Program \n Sub Main(args As String()) \n Dim x As Integer = CInt(10.0 + \n 11.0 + \n 10) ' asas \n End Sub \n End Module"))
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsInsertMissingCast)>
        Public Sub TestWidening()
            TestMissing(
NewLines("Option Strict On \n Module Program \n Sub Main(args As String()) \n Dim x As Double = 10[||] \n End Sub \n End Module"))
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsInsertMissingCast)>
        Public Sub TestInvalidCast()
            TestMissing(
NewLines("Option Strict On \n Class A \n End Class \n Class B \n End Class \n Module Program[||] \n Sub Main(args As String()) \n Dim x As A = New B() \n End Sub \n End Module"))
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsInsertMissingCast)>
        Public Sub TestOptionStrictOn()
            Test(
NewLines("Option Strict On \n Module Module1 \n Sub Main() \n Dim red = ColorF.FromArgb(255, 255, 0, 0) \n Dim c As Color = [|red|] \n End Sub \n End Module \n Public Structure ColorF \n Public A, R, G, B As Single \n Public Shared Function FromArgb(a As Double, r As Double, g As Double, b As Double) As ColorF \n Return New ColorF With {.A = CSng(a), .R = CSng(r), .G = CSng(g), .B = CSng(b)} \n End Function \n Public Shared Widening Operator CType(x As Color) As ColorF \n Return ColorF.FromArgb(x.A / 255, x.R / 255, x.G / 255, x.B / 255) \n End Operator \n Public Shared Narrowing Operator CType(x As ColorF) As Color \n Return Color.FromArgb(CByte(x.A * 255), CByte(x.R * 255), CByte(x.G * 255), CByte(x.B * 255)) \n End Operator \n End Structure \n Public Structure Color \n Public A, R, G, B As Byte \n Public Shared Function FromArgb(a As Byte, r As Byte, g As Byte, b As Byte) As Color \n Return New Color With {.A = a, .R = r, .G = g, .B = b} \n End Function \n End Structure"),
NewLines("Option Strict On \n Module Module1 \n Sub Main() \n Dim red = ColorF.FromArgb(255, 255, 0, 0) \n Dim c As Color = CType(red, Color) \n End Sub \n End Module \n Public Structure ColorF \n Public A, R, G, B As Single \n Public Shared Function FromArgb(a As Double, r As Double, g As Double, b As Double) As ColorF \n Return New ColorF With {.A = CSng(a), .R = CSng(r), .G = CSng(g), .B = CSng(b)} \n End Function \n Public Shared Widening Operator CType(x As Color) As ColorF \n Return ColorF.FromArgb(x.A / 255, x.R / 255, x.G / 255, x.B / 255) \n End Operator \n Public Shared Narrowing Operator CType(x As ColorF) As Color \n Return Color.FromArgb(CByte(x.A * 255), CByte(x.R * 255), CByte(x.G * 255), CByte(x.B * 255)) \n End Operator \n End Structure \n Public Structure Color \n Public A, R, G, B As Byte \n Public Shared Function FromArgb(a As Byte, r As Byte, g As Byte, b As Byte) As Color \n Return New Color With {.A = a, .R = r, .G = g, .B = b} \n End Function \n End Structure"))
        End Sub
    End Class
End Namespace
