' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.InsertMissingCast

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.InsertMissingCast
    Public Class InsertMissingCastTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (Nothing, New InsertMissingCastCodeFixProvider)
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsInsertMissingCast)>
        Public Async Function TestPredefinedAssignment() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Module M1
    Sub Main()
        Dim b As Byte = 1
        Dim c As Byte = 2
        b = [|b & c|]
    End Sub
End Module",
"Option Strict On
Module M1
    Sub Main()
        Dim b As Byte = 1
        Dim c As Byte = 2
        b = CByte(b & c)
    End Sub
End Module")
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsInsertMissingCast)>
        Public Async Function TestAssignment() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Class Base
End Class
Class Derived
    Inherits Base
End Class
Module M1
    Sub Main()
        Dim d As Derived = Nothing
        Dim b As Base = Nothing
        d = [|b|]
    End Sub
End Module",
"Option Strict On
Class Base
End Class
Class Derived
    Inherits Base
End Class
Module M1
    Sub Main()
        Dim d As Derived = Nothing
        Dim b As Base = Nothing
        d = CType(b, Derived)
    End Sub
End Module")
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsInsertMissingCast)>
        Public Async Function TestMethodCall() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Class Base
End Class
Class Derived
    Inherits Base
End Class
Module M1
    Sub goo(d As Derived)
    End Sub
    Sub Main()
        Dim b As Base = Nothing
        goo([|b|])
    End Sub
End Module",
"Option Strict On
Class Base
End Class
Class Derived
    Inherits Base
End Class
Module M1
    Sub goo(d As Derived)
    End Sub
    Sub Main()
        Dim b As Base = Nothing
        goo(CType(b, Derived))
    End Sub
End Module")
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsInsertMissingCast)>
        Public Async Function TestMethodCallPredefined() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Module M1
    Sub goo(d As Integer)
    End Sub
    Sub Main()
        goo([|""10""|])
    End Sub
End Module",
"Option Strict On
Module M1
    Sub goo(d As Integer)
    End Sub
    Sub Main()
        goo(CInt(""10""))
    End Sub
End Module")
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsInsertMissingCast)>
        Public Async Function TestConditional() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Module M1
    Sub Main()
        Dim i As Integer = 10
        Dim b = If([|i|], True, False)
    End Sub
End Module",
"Option Strict On
Module M1
    Sub Main()
        Dim i As Integer = 10
        Dim b = If(CBool(i), True, False)
    End Sub
End Module")
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsInsertMissingCast)>
        Public Async Function TestReturn() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Module M1
    Function goo() As Integer
        Return [|""10""|] 
 End Function
End Module",
"Option Strict On
Module M1
    Function goo() As Integer
        Return CInt(""10"")
    End Function
End Module")
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsInsertMissingCast)>
        Public Async Function TestLambda() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Class Base
End Class
Class Derived
    Inherits Base
End Class
Module M1
    Sub Main()
        Dim l = Function() As Derived
                    Return [|New Base()|]
                End Function
    End Sub
End Module",
"Option Strict On
Class Base
End Class
Class Derived
    Inherits Base
End Class
Module M1
    Sub Main()
        Dim l = Function() As Derived
                    Return CType(New Base(), Derived)
                End Function
    End Sub
End Module")
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsInsertMissingCast)>
        Public Async Function TestAttribute() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Module Program
    <System.Obsolete(""as"", [|10|])>
    Sub Main(args As String())
    End Sub
End Module",
"Option Strict On
Module Program
    <System.Obsolete(""as"", CBool(10))>
    Sub Main(args As String())
    End Sub
End Module")
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsInsertMissingCast)>
        Public Async Function TestMultiline() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Module Program
    Sub Main(args As String())
        Dim x As Integer = [|10.0 +
        11.0 +
        10|] ' asas 
    End Sub
End Module",
"Option Strict On
Module Program
    Sub Main(args As String())
        Dim x As Integer = CInt(10.0 +
        11.0 +
        10) ' asas 
    End Sub
End Module")
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsInsertMissingCast)>
        Public Async Function TestWidening() As Task
            Await TestMissingInRegularAndScriptAsync(
"Option Strict On
Module Program
    Sub Main(args As String())
        Dim x As Double = 10[||]
    End Sub
End Module")
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsInsertMissingCast)>
        Public Async Function TestInvalidCast() As Task
            Await TestMissingInRegularAndScriptAsync(
"Option Strict On
Class A
End Class
Class B
End Class
Module Program[||]
    Sub Main(args As String())
        Dim x As A = New B()
    End Sub
End Module")
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsInsertMissingCast)>
        Public Async Function TestOptionStrictOn() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Module Module1
    Sub Main()
        Dim red = ColorF.FromArgb(255, 255, 0, 0)
        Dim c As Color = [|red|]
    End Sub
End Module
Public Structure ColorF
    Public A, R, G, B As Single
    Public Shared Function FromArgb(a As Double, r As Double, g As Double, b As Double) As ColorF
        Return New ColorF With {.A = CSng(a), .R = CSng(r), .G = CSng(g), .B = CSng(b)}
    End Function
    Public Shared Widening Operator CType(x As Color) As ColorF
        Return ColorF.FromArgb(x.A / 255, x.R / 255, x.G / 255, x.B / 255)
    End Operator
    Public Shared Narrowing Operator CType(x As ColorF) As Color
        Return Color.FromArgb(CByte(x.A * 255), CByte(x.R * 255), CByte(x.G * 255), CByte(x.B * 255))
    End Operator
End Structure
Public Structure Color
    Public A, R, G, B As Byte
    Public Shared Function FromArgb(a As Byte, r As Byte, g As Byte, b As Byte) As Color
        Return New Color With {.A = a, .R = r, .G = g, .B = b}
    End Function
End Structure",
"Option Strict On
Module Module1
    Sub Main()
        Dim red = ColorF.FromArgb(255, 255, 0, 0)
        Dim c As Color = CType(red, Color)
    End Sub
End Module
Public Structure ColorF
    Public A, R, G, B As Single
    Public Shared Function FromArgb(a As Double, r As Double, g As Double, b As Double) As ColorF
        Return New ColorF With {.A = CSng(a), .R = CSng(r), .G = CSng(g), .B = CSng(b)}
    End Function
    Public Shared Widening Operator CType(x As Color) As ColorF
        Return ColorF.FromArgb(x.A / 255, x.R / 255, x.G / 255, x.B / 255)
    End Operator
    Public Shared Narrowing Operator CType(x As ColorF) As Color
        Return Color.FromArgb(CByte(x.A * 255), CByte(x.R * 255), CByte(x.G * 255), CByte(x.B * 255))
    End Operator
End Structure
Public Structure Color
    Public A, R, G, B As Byte
    Public Shared Function FromArgb(a As Byte, r As Byte, g As Byte, b As Byte) As Color
        Return New Color With {.A = a, .R = r, .G = g, .B = b}
    End Function
End Structure")
        End Function
    End Class
End Namespace
