' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.AddExplicitCast

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.AddExplicitCast
    Public Class AddExplicitCastTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (Nothing, New VisualBasicAddExplicitCastCodeFixProvider)
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)>
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

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)>
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

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)>
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

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)>
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

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)>
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

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)>
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

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)>
        Public Async Function TestObjectCreation() As Task
            Await TestMissingInRegularAndScriptAsync(
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
End Module")
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)>
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

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)>
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

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)>
        Public Async Function TestWidening() As Task
            Await TestMissingInRegularAndScriptAsync(
"Option Strict On
Module Program
    Sub Main(args As String())
        Dim x As Double = 10[||]
    End Sub
End Module")
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)>
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

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)>
        Public Async Function SimpleVariableDeclaration() As Task

            Await TestInRegularAndScriptAsync(
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub M()
        Dim b As Base
        Dim d As Derived = [|b|]
    End Sub
End Class",
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub M()
        Dim b As Base
        Dim d As Derived = CType(b, Derived)
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)>
        Public Async Function SimpleVariableDeclarationWithFunctionInnvocation() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Function returnBase() As Base
        Dim b As Base
        Return b
    End Function

    Private Sub M()
        Dim d As Derived = [|returnBase()|]
    End Sub
End Class",
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Function returnBase() As Base
        Dim b As Base
        Return b
    End Function

    Private Sub M()
        Dim d As Derived = CType(returnBase(), Derived)
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)>
        Public Async Function ReturnStatementWithObject() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Function returnBase() As Derived
        Dim b As Base
        Return [|b|]
    End Function
End Class",
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Function returnBase() As Derived
        Dim b As Base
        Return CType(b, Derived)
    End Function
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)>
        Public Async Function ReturnStatementWithIEnumerator() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Function returnBase() As IEnumerable(Of Derived)
        Dim b As Base
        Return [|b|]
    End Function
End Class",
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Function returnBase() As IEnumerable(Of Derived)
        Dim b As Base
        Return CType(b, IEnumerable(Of Derived))
    End Function
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)>
        Public Async Function YieldReturnStatementWithObject() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Iterator Function returnBase() As IEnumerable(Of Derived)
        Dim b As Base = New Base
        Yield [||]b
    End Function
End Class",
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Iterator Function returnBase() As IEnumerable(Of Derived)
        Dim b As Base = New Base
        Yield CType(b, Derived)
    End Function
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)>
        Public Async Function ReturnStatementWithFunctionInnvocation() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Function returnBase() As Base
        Dim b As Base
        Return b
    End Function

    Private Function returnDerived() As Derived
        Return [|returnBase()|]
    End Function
End Class",
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Function returnBase() As Base
        Dim b As Base
        Return b
    End Function

    Private Function returnDerived() As Derived
        Return CType(returnBase(), Derived)
    End Function
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)>
        Public Async Function SimpleFunctionArgumentsWithObject1() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Function returnBase() As Base
        Dim b As Base
        Return b
    End Function

    Private Sub passDerived(ByVal d As Derived)
    End Sub

    Private Sub M()
        Dim b As Base
        passDerived([||]b)
    End Sub
End Class",
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Function returnBase() As Base
        Dim b As Base
        Return b
    End Function

    Private Sub passDerived(ByVal d As Derived)
    End Sub

    Private Sub M()
        Dim b As Base
        passDerived(CType(b, Derived))
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)>
        Public Async Function SimpleFunctionArgumentsWithObject2() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Function returnBase() As Base
        Dim b As Base
        Return b
    End Function

    Private Sub passDerived(i As Integer, ByVal d As Derived)
    End Sub

    Private Sub M()
        Dim b As Base
        passDerived(1, [||]b)
    End Sub
End Class",
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Function returnBase() As Base
        Dim b As Base
        Return b
    End Function

    Private Sub passDerived(i As Integer, ByVal d As Derived)
    End Sub

    Private Sub M()
        Dim b As Base
        passDerived(1, CType(b, Derived))
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)>
        Public Async Function SimpleFunctionArgumentsWithFunctionInvocation() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Function returnBase() As Base
        Dim b As Base
        Return b
    End Function

    Private Sub passDerived(ByVal d As Derived)
    End Sub

    Private Sub M()
        passDerived([|returnBase()|])
    End Sub
End Class",
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Function returnBase() As Base
        Dim b As Base
        Return b
    End Function

    Private Sub passDerived(ByVal d As Derived)
    End Sub

    Private Sub M()
        passDerived(CType(returnBase(), Derived))
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)>
        Public Async Function SimpleConstructorArgumentsWithObject() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Class Test
        Public Sub New(ByVal d As Derived)
        End Sub
    End Class

    Private Sub M()
        Dim b As Base
        Dim t As Test = New Test([||]b)
    End Sub
End Class",
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Class Test
        Public Sub New(ByVal d As Derived)
        End Sub
    End Class

    Private Sub M()
        Dim b As Base
        Dim t As Test = New Test(CType(b, Derived))
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)>
        Public Async Function VariableDeclarationWithPublicFieldMember() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Class Test
        Public b As Base

        Public Sub New(ByVal b As Base)
            Me.b = b
        End Sub
    End Class

    Private Sub M()
        Dim b As Base
        Dim t As Test = New Test(b)
        Dim d As Derived = t.b[||]
    End Sub
End Class",
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Class Test
        Public b As Base

        Public Sub New(ByVal b As Base)
            Me.b = b
        End Sub
    End Class

    Private Sub M()
        Dim b As Base
        Dim t As Test = New Test(b)
        Dim d As Derived = CType(t.b, Derived)
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)>
        Public Async Function VariableDeclarationWithPrivateFieldMember() As Task
            Await TestMissingInRegularAndScriptAsync(
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Class Test
        Private b As Base

        Public Sub New(ByVal b As Base)
            Me.b = b
        End Sub
    End Class

    Private Sub M()
        Dim b As Base
        Dim t As Test = New Test(b)
        Dim d As Derived = t.b[||]
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)>
        Public Async Function PublicMemberFunctionArgument1() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub M()
        Dim b As Base
        Dim list As List(Of Derived) = New List(Of Derived)()
        list.Add([||]b)
    End Sub
End Class",
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub M()
        Dim b As Base
        Dim list As List(Of Derived) = New List(Of Derived)()
        list.Add(b)
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)>
        Public Async Function PublicMemberFunctionArgument2() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Class Test
        Public Sub testing(ByVal d As Derived)
        End Sub
    End Class

    Private Sub M()
        Dim b As Base
        Dim t As Test
        t.testing([||]b)
    End Sub
End Class",
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Class Test
        Public Sub testing(ByVal d As Derived)
        End Sub
    End Class

    Private Sub M()
        Dim b As Base
        Dim t As Test
        t.testing(CType(b, Derived))
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)>
        Public Async Function PrivateMemberFunctionArgument() As Task
            Await TestMissingInRegularAndScriptAsync(
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Class Test
        Private Sub testing(ByVal d As Derived)
        End Sub
    End Class

    Private Sub M()
        Dim b As Base
        Dim t As Test
        t.testing([||]b)
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)>
        Public Async Function MemberFunctions() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Class Test
        Public Sub testing(ByVal d As Derived)
        End Sub

        Private Sub testing(ByVal b As Base)
        End Sub
    End Class

    Private Sub M()
        Dim b As Base
        Dim t As Test
        t.testing([||]b)
    End Sub
End Class",
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Class Test
        Public Sub testing(ByVal d As Derived)
        End Sub

        Private Sub testing(ByVal b As Base)
        End Sub
    End Class

    Private Sub M()
        Dim b As Base
        Dim t As Test
        t.testing(CType(b, Derived))
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)>
        Public Async Function BaseConstructorArgument() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Class Test
        Public Sub New(ByVal d As Derived)
        End Sub
    End Class

    Class Derived_Test
        Inherits Test

        Public Sub New(ByVal b As Base)
            MyBase.New([||]b)
        End Sub
    End Class
End Class",
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Class Test
        Public Sub New(ByVal d As Derived)
        End Sub
    End Class

    Class Derived_Test
        Inherits Test

        Public Sub New(ByVal b As Base)
            MyBase.New(CType(b, Derived))
        End Sub
    End Class
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)>
        Public Async Function ThisConstructorArgument() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Class Test
        Public Sub New(ByVal d As Derived)
        End Sub

        Public Sub New(ByVal b As Base, ByVal i As Integer)
            Me.New([||]b)
        End Sub
    End Class
End Class",
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Class Test
        Public Sub New(ByVal d As Derived)
        End Sub

        Public Sub New(ByVal b As Base, ByVal i As Integer)
            Me.New(CType(b, Derived))
        End Sub
    End Class
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)>
        Public Async Function ObjectInitializer() As Task
            Await TestMissingInRegularAndScriptAsync(
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub M()
        Dim d As Derived = [|New Base()|]
        Dim d2 As Derived = New Test()
    End Sub
End Class
")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)>
        Public Async Function ObjectInitializer2() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Class Test
        Public Shared Operator CType(t As Test) As Derived
            Return New Derived
        End Operator
    End Class

    Private Sub M()
        Dim d2 As Derived = New Test()[||]
    End Sub
End Class",
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Class Test
        Public Shared Operator CType(t As Test) As Derived
            Return New Derived
        End Operator
    End Class

    Private Sub M()
        Dim d2 As Derived = CType(New Test(), Derived)
    End Sub
End Class")
        End Function
    End Class
End Namespace
