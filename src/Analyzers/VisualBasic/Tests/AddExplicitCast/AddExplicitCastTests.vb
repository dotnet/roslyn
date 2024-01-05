' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.AddExplicitCast

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.AddExplicitCast
    <Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)>
    Partial Public Class AddExplicitCastTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (Nothing, New VisualBasicAddExplicitCastCodeFixProvider)
        End Function

        Protected Overrides Function MassageActions(actions As ImmutableArray(Of CodeAction)) As ImmutableArray(Of CodeAction)
            Return FlattenActions(actions)
        End Function

        <Fact>
        Public Async Function TestPredefinedAssignmentCBool() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Module M1
    Sub Main()
        Dim i As Integer = 0
        Dim b As Boolean = [|i|]
    End Sub
End Module",
"Option Strict On
Module M1
    Sub Main()
        Dim i As Integer = 0
        Dim b As Boolean = CBool(i)
    End Sub
End Module")
        End Function

        <Fact>
        Public Async Function TestPredefinedAssignmentCByte() As Task
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

        <Fact>
        Public Async Function TestPredefinedAssignmentCChar() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Module M1
    Sub Main()
        Dim s As String = 0.ToString()
        Dim ch As Char = [|s|]
    End Sub
End Module",
"Option Strict On
Module M1
    Sub Main()
        Dim s As String = 0.ToString()
        Dim ch As Char = CChar(s)
    End Sub
End Module")
        End Function

        <Fact>
        Public Async Function TestPredefinedAssignmentCDate() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Module M1
    Sub Main()
        Dim s As String = #2006-06-13#.ToString()
        Dim dt As Date = [|s|]
    End Sub
End Module",
"Option Strict On
Module M1
    Sub Main()
        Dim s As String = #2006-06-13#.ToString()
        Dim dt As Date = CDate(s)
    End Sub
End Module")
        End Function

        <Fact>
        Public Async Function TestPredefinedAssignmentCDbl() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Module M1
    Sub Main()
        Dim s As String = 1.0R.ToString()
        Dim db As Double = [|s|]
    End Sub
End Module",
"Option Strict On
Module M1
    Sub Main()
        Dim s As String = 1.0R.ToString()
        Dim db As Double = CDbl(s)
    End Sub
End Module")
        End Function

        <Fact>
        Public Async Function TestPredefinedAssignmentCDec() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Module M1
    Sub Main()
        Dim db As Double = 1.0R
        Dim dc As Decimal = [|db|]
    End Sub
End Module",
"Option Strict On
Module M1
    Sub Main()
        Dim db As Double = 1.0R
        Dim dc As Decimal = CDec(db)
    End Sub
End Module")
        End Function

        <Fact>
        Public Async Function TestPredefinedAssignmentCInt() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Module M1
    Sub Main()
        Dim db As Double = 1.0R
        Dim i As Integer = [|db|]
    End Sub
End Module",
"Option Strict On
Module M1
    Sub Main()
        Dim db As Double = 1.0R
        Dim i As Integer = CInt(db)
    End Sub
End Module")
        End Function

        <Fact>
        Public Async Function TestPredefinedAssignmentCLng() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Module M1
    Sub Main()
        Dim db As Double = 1.0R
        Dim lg As Long = [|db|]
    End Sub
End Module",
"Option Strict On
Module M1
    Sub Main()
        Dim db As Double = 1.0R
        Dim lg As Long = CLng(db)
    End Sub
End Module")
        End Function

        <Fact>
        Public Async Function TestPredefinedAssignmentCSByte() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Module M1
    Sub Main()
        Dim dc As Decimal = -14.02D
        Dim sb As SByte = [|dc|]
    End Sub
End Module",
"Option Strict On
Module M1
    Sub Main()
        Dim dc As Decimal = -14.02D
        Dim sb As SByte = CSByte(dc)
    End Sub
End Module")
        End Function

        <Fact>
        Public Async Function TestPredefinedAssignmentCShort() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Module M1
    Sub Main()
        Dim i As Integer = 2
        Dim sh As Short = [|i|]
    End Sub
End Module",
"Option Strict On
Module M1
    Sub Main()
        Dim i As Integer = 2
        Dim sh As Short = CShort(i)
    End Sub
End Module")
        End Function

        <Fact>
        Public Async Function TestPredefinedAssignmentCSng() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Module M1
    Sub Main()
        Dim db As Double = 1.0R
        Dim sn As Single = [|db|]
    End Sub
End Module",
"Option Strict On
Module M1
    Sub Main()
        Dim db As Double = 1.0R
        Dim sn As Single = CSng(db)
    End Sub
End Module")
        End Function

        <Fact>
        Public Async Function TestPredefinedAssignmentCStr() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Module M1
    Sub Main()
        Dim i As Integer = 1
        Dim s As String = [|i|]
    End Sub
End Module",
"Option Strict On
Module M1
    Sub Main()
        Dim i As Integer = 1
        Dim s As String = CStr(i)
    End Sub
End Module")
        End Function

        <Fact>
        Public Async Function TestPredefinedAssignmentObjectToStringCStr() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Module M1
    Sub Main()
        Dim o As Object = 1.ToString()
        Dim s As String = [|o|]
    End Sub
End Module",
"Option Strict On
Module M1
    Sub Main()
        Dim o As Object = 1.ToString()
        Dim s As String = CStr(o)
    End Sub
End Module")
        End Function

        <Fact>
        Public Async Function TestPredefinedAssignmentCUInt() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Module M1
    Sub Main()
        Dim i As Integer = -1
        Dim ui As UInteger = [|i|]
    End Sub
End Module",
"Option Strict On
Module M1
    Sub Main()
        Dim i As Integer = -1
        Dim ui As UInteger = CUInt(i)
    End Sub
End Module")
        End Function

        <Fact>
        Public Async Function TestPredefinedAssignmentCULng() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Module M1
    Sub Main()
        Dim l As Long = -1
        Dim ul As ULong =[|l|]
    End Sub
End Module",
"Option Strict On
Module M1
    Sub Main()
        Dim l As Long = -1
        Dim ul As ULong = CULng(l)
    End Sub
End Module")
        End Function

        <Fact>
        Public Async Function TestPredefinedAssignmentCUShort() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Module M1
    Sub Main()
        Dim i As Integer = -2
        Dim us As UShort = [|i|]
    End Sub
End Module",
"Option Strict On
Module M1
    Sub Main()
        Dim i As Integer = -2
        Dim us As UShort = CUShort(i)
    End Sub
End Module")
        End Function

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
        Public Async Function TestClassAttribute() As Task
            Await TestInRegularAndScriptAsync(
"<[|TestOverload|](1)>
Class Program
End Class
Public Class TestOverloadAttribute
    Inherits System.Attribute
    Public Sub New(param As Short)
    End Sub
    Public Sub New(param As TestEnum)
    End Sub
End Class
Public Enum TestEnum
    One = 1
End Enum",
"<TestOverload(CShort(1))>
Class Program
End Class
Public Class TestOverloadAttribute
    Inherits System.Attribute
    Public Sub New(param As Short)
    End Sub
    Public Sub New(param As TestEnum)
    End Sub
End Class
Public Enum TestEnum
    One = 1
End Enum", compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication, optionStrict:=OptionStrict.Off))
        End Function

        <Fact>
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

        <Fact>
        Public Async Function TestWidening() As Task
            Await TestMissingInRegularAndScriptAsync(
"Option Strict On
Module Program
    Sub Main(args As String())
        Dim x As Double = 10[||]
    End Sub
End Module")
        End Function

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
        Public Async Function ReturnStatementWithIEnumerator() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Imports System.Collections.Generic
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
Imports System.Collections.Generic
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

        <Fact>
        Public Async Function YieldReturnStatementWithObject() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Imports System.Collections.Generic
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
Imports System.Collections.Generic
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
        Public Async Function PublicMemberFunctionArgument1() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Imports System.Collections.Generic
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub M()
        Dim b As Base = New Derived
        Dim list As List(Of Derived) = New List(Of Derived)()
        list.Add([|b|])
    End Sub
End Class",
"Option Strict On
Imports System.Collections.Generic
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub M()
        Dim b As Base = New Derived
        Dim list As List(Of Derived) = New List(Of Derived)()
        list.Add(CType(b, Derived))
    End Sub
End Class")
        End Function

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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
        Public Shared Narrowing Operator CType(t As Test) As Derived
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
        Public Shared Narrowing Operator CType(t As Test) As Derived
            Return New Derived
        End Operator
    End Class

    Private Sub M()
        Dim d2 As Derived = CType(New Test(), Derived)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function ObjectInitializer3() As Task
            Await TestMissingInRegularAndScriptAsync(
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Function M() As Derived
        Return [||]New Base
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function ObjectInitializer4() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Class Test
        Public Shared Narrowing Operator CType(t As Test) As Derived
            Return New Derived
        End Operator
    End Class

    Private Function M() As Derived
        Return [||]New Test
    End Function
End Class",
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Class Test
        Public Shared Narrowing Operator CType(t As Test) As Derived
            Return New Derived
        End Operator
    End Class

    Private Function M() As Derived
        Return CType(New Test, Derived)
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function ObjectInitializer5() As Task
            Await TestMissingInRegularAndScriptAsync(
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub M(d As Derived)
    End Sub

    Private Sub Goo()
        M([||]new Base)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function ObjectInitializer6() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Class Test
        Public Shared Narrowing Operator CType(t As Test) As Derived
            Return New Derived
        End Operator
    End Class

    Private Sub M(d As Derived)
    End Sub

    Private Sub Goo()
        M([||]new Test)
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
        Public Shared Narrowing Operator CType(t As Test) As Derived
            Return New Derived
        End Operator
    End Class

    Private Sub M(d As Derived)
    End Sub

    Private Sub Goo()
        M(CType(new Test, Derived))
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function ObjectInitializer7() As Task
            Await TestMissingInRegularAndScriptAsync(
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub M(d As Julia)
    End Sub

    Private Sub Goo()
        M([||]new Base)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function ObjectInitializer8() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Class Test
        Private Shared Narrowing Operator CType(t As Test) As Derived
            Return New Derived
        End Operator
    End Class

    Private Sub M(d As Derived)
    End Sub

    Private Sub Goo()
        M([||]new Test)
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
        Private Shared Narrowing Operator CType(t As Test) As Derived
            Return New Derived
        End Operator
    End Class

    Private Sub M(d As Derived)
    End Sub

    Private Sub Goo()
        M(CType(new Test, Derived))
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function InheritInterfaces1() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Class Program
    Interface Base1
    End Interface

    Interface Base2
    End Interface

    Class Derived
        Implements Base1, Base2
    End Class

    Private Sub Goo(ByRef b As Base2)
        Dim d As Derived = [||]b
    End Sub
End Class",
"Option Strict On
Class Program
    Interface Base1
    End Interface

    Interface Base2
    End Interface

    Class Derived
        Implements Base1, Base2
    End Class

    Private Sub Goo(ByRef b As Base2)
        Dim d As Derived = CType(b, Derived)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function InheritInterfaces2() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Class Program
    Interface Base1
    End Interface

    Interface Base2
    End Interface

    Class Derived1
        Implements Base1, Base2
    End Class

    Class Derived2
        Inherits Derived1
    End Class

    Private Sub Goo(ByRef b As Base2)
        Dim d As Derived2 = [||]b
    End Sub
End Class",
"Option Strict On
Class Program
    Interface Base1
    End Interface

    Interface Base2
    End Interface

    Class Derived1
        Implements Base1, Base2
    End Class

    Class Derived2
        Inherits Derived1
    End Class

    Private Sub Goo(ByRef b As Base2)
        Dim d As Derived2 = CType(b, Derived2)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function InheritInterfaces3() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Class Program
    Interface Base1
    End Interface

    Interface Base2
        Implements Base1
    End Interface

    Private Function Goo(ByRef b As Base1) As Base2
        Return b[||]
    End Function
End Class",
"Option Strict On
Class Program
    Interface Base1
    End Interface

    Interface Base2
        Implements Base1
    End Interface

    Private Function Goo(ByRef b As Base1) As Base2
        Return CType(b, Base2)
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function InheritInterfaces4() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Class Program
    Interface Base1
    End Interface

    Interface Base2
        Implements Base1
    End Interface

    Private Sub Goo(ByRef b As Base1)
        Dim b2 As Base2 = b[||]
    End Sub
End Class",
"Option Strict On
Class Program
    Interface Base1
    End Interface

    Interface Base2
        Implements Base1
    End Interface

    Private Sub Goo(ByRef b As Base1)
        Dim b2 As Base2 = CType(b, Base2)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function InheritInterfaces5() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Class Program
    Interface Base1
    End Interface

    Interface Base2
        Inherits Base1
    End Interface

    Interface Base3
    End Interface

    Class Derived1
        Implements Base2, Base3
    End Class

    Class Derived2
        Inherits Derived1
    End Class

    Private Sub Goo(ByRef b As Derived2)
    End Sub

    Private Sub M(ByRef b As Base1)
        Goo([||]b)
    End Sub
End Class",
"Option Strict On
Class Program
    Interface Base1
    End Interface

    Interface Base2
        Inherits Base1
    End Interface

    Interface Base3
    End Interface

    Class Derived1
        Implements Base2, Base3
    End Class

    Class Derived2
        Inherits Derived1
    End Class

    Private Sub Goo(ByRef b As Derived2)
    End Sub

    Private Sub M(ByRef b As Base1)
        Goo(CType(b, Derived2))
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function GenericType() As Task
            Await TestMissingInRegularAndScriptAsync(
"Option Strict On
Imports System

Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub M()
        Dim func1 As Func(Of Base, Base) = Function(b) b
        Dim func2 As Func(Of Derived, Derived) = func1[||]
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function GenericType2() As Task
            Await TestMissingInRegularAndScriptAsync(
"Option Strict On
Class Program
    Interface IA
    End Interface

    Interface IB
        Inherits IA
    End Interface

    Interface A(Of T As IA)
    End Interface

    Class B(Of T As IB)
        Implements A(Of T)
    End Class

    Private Sub Goo()
        Dim b As B(Of IB) = New B(Of IB)
        Dim c1 As A(Of IA) = b[||]
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function GenericType3() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Class Program
    Interface IA
    End Interface

    Class CB
        Implements IA
    End Class

    Interface A(Of T As IA, U)
    End Interface

    Class B(Of T As CB, U)
        Implements A(Of T, U)
    End Class

    Private Sub Goo()
        Dim b As B(Of CB, Integer) = New B(Of CB, Integer)
        Dim c1 As A(Of IA, String) = b[||]
    End Sub
End Class",
"Option Strict On
Class Program
    Interface IA
    End Interface

    Class CB
        Implements IA
    End Class

    Interface A(Of T As IA, U)
    End Interface

    Class B(Of T As CB, U)
        Implements A(Of T, U)
    End Class

    Private Sub Goo()
        Dim b As B(Of CB, Integer) = New B(Of CB, Integer)
        Dim c1 As A(Of IA, String) = CType(b, A(Of IA, String))
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function LambdaFunction1() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Imports System

Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub M()
        Dim foo As Func(Of Base, Derived) = Function(d) d[||]
    End Sub
End Class",
"Option Strict On
Imports System

Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub M()
        Dim foo As Func(Of Base, Derived) = Function(d) CType(d, Derived)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function LambdaFunction2() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Imports System

Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub Goo()
        Dim func As Func(Of Derived, Derived) = Function(d) d
        Dim b As Base
        Dim b2 As Base = func(b[||])
    End Sub
End Class",
"Option Strict On
Imports System

Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub Goo()
        Dim func As Func(Of Derived, Derived) = Function(d) d
        Dim b As Base
        Dim b2 As Base = func(CType(b, Derived))
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function LambdaFunction3() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Imports System

Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub Goo()
        Dim func As Func(Of Base, Base) = Function(d) d
        Dim b As Base
        Dim b2 As Derived = [||]func(b)
    End Sub
End Class",
"Option Strict On
Imports System

Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub Goo()
        Dim func As Func(Of Base, Base) = Function(d) d
        Dim b As Base
        Dim b2 As Derived = CType(func(b), Derived)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function LambdaFunction4() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Imports System

Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Function Goo() As Derived
        Dim func As Func(Of Base, Base) = Function(d) d
        Dim b As Base
        Return [||]func(b)
    End Function
End Class",
"Option Strict On
Imports System

Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Function Goo() As Derived
        Dim func As Func(Of Base, Base) = Function(d) d
        Dim b As Base
        Return CType(func(b), Derived)
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function LambdaFunction5_ReturnStatement() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Imports System

Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Shared Function Goo() As Action(Of Base)
        Return [|Sub(ByVal b As Derived)
                   Console.WriteLine()
               End Sub|]
    End Function
End Class",
"Option Strict On
Imports System

Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Shared Function Goo() As Action(Of Base)
        Return CType(Sub(ByVal b As Derived)
                   Console.WriteLine()
               End Sub, Action(Of Base))
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function LambdaFunction6_Arguments() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Imports System

Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub M(ByVal d As Derived, ByVal action As Action(Of Derived))
    End Sub

    Private Sub Goo()
        Dim b As Base = New Derived()
        M([||]b, Sub(ByVal d As Derived)
             End Sub)
    End Sub
End Class",
"Option Strict On
Imports System

Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub M(ByVal d As Derived, ByVal action As Action(Of Derived))
    End Sub

    Private Sub Goo()
        Dim b As Base = New Derived()
        M(CType(b, Derived), Sub(ByVal d As Derived)
             End Sub)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function LambdaFunction7_Arguments() As Task
            Await TestMissingInRegularAndScriptAsync(
"Option Strict On
Imports System

Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub M(ByVal d As Derived, ByRef action As Action(Of Derived))
    End Sub

    Private Sub Goo()
        Dim b As Base = New Derived()
        M(b[||], Sub(ByRef d As Base)
             End Sub)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function LambdaFunction8_Arguments() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Imports System
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Shared Sub M(ByVal d As Derived, ParamArray action As Action(Of Derived)())
    End Sub

    Private Shared Sub Goo()
        Dim b1 As Base = New Derived()
        Dim action As Action(Of Derived) = Sub(b)
                                           End Sub
        M([||]b1, action, action)
    End Sub
End Class",
"Option Strict On
Imports System
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Shared Sub M(ByVal d As Derived, ParamArray action As Action(Of Derived)())
    End Sub

    Private Shared Sub Goo()
        Dim b1 As Base = New Derived()
        Dim action As Action(Of Derived) = Sub(b)
                                           End Sub
        M(CType(b1, Derived), action, action)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function RedundantCast1() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub Goo()
        Dim b As Base
        Dim d As Derived = [||]CType(b, Base)
    End Sub
End Class",
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub Goo()
        Dim b As Base
        Dim d As Derived = CType(b, Derived)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function RedundantCast2() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived1
        Inherits Base
    End Class

    Class Derived2
        Inherits Derived1
    End Class

    Private Sub Goo()
        Dim b As Base
        Dim d As Derived2 = [||]CType(b, Derived1)
    End Sub
End Class",
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived1
        Inherits Base
    End Class

    Class Derived2
        Inherits Derived1
    End Class

    Private Sub Goo()
        Dim b As Base
        Dim d As Derived2 = CType(b, Derived2)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function RedundantCast3() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub M(ByVal d As Derived)
    End Sub

    Private Sub Goo()
        Dim b As Base
        M([||]CType(b, Base))
    End Sub
End Class",
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub M(ByVal d As Derived)
    End Sub

    Private Sub Goo()
        Dim b As Base
        M(CType(b, Derived))
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function RedundantCast4() As Task
            ' Currently not offered, but could be allowed in the future.
            Await TestMissingAsync(
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived1
        Inherits Base
    End Class

    Class Derived2
        Inherits Base
    End Class

    Private Sub M(ByRef d As Derived2)
    End Sub

    Private Sub Goo()
        Dim b As Base
        M([||]CType(b, Derived1))
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function LambdaFunction9_Arguments() As Task
            Await TestMissingInRegularAndScriptAsync(
"Option Strict On
Imports System

Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Shared Sub M(ByVal d As Derived, ParamArray action As Action(Of Derived)())
    End Sub

    Private Shared Sub Goo()
        Dim b1 As Base = New Derived()
        Dim list() As Action(Of Derived) = {}
        Dim action As Action(Of Derived) = Sub(b)
                                           End Sub
        M([||]b1, list, action)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function ExactMethodCandidate() As Task
            Await TestMissingInRegularAndScriptAsync(
"Option Strict On
Class Program
    Class Base
        Public Sub Testing(ByRef d As Base)
        End Sub
    End Class

    Class Derived
        Inherits Base

        Public Overloads Sub Testing(ByRef d As Derived)
        End Sub
    End Class

    Private Sub M()
        Dim b As Base = New Base()
        Dim d As Derived = New Derived()
        d.Testing([||]b)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function MethodCandidates1_ArgumentsInOrder_NoLabels() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub Goo(ByVal s As String, ByRef d As Derived)
    End Sub

    Private Sub Goo(ByVal s As String, ByVal i As Integer)
    End Sub

    Private Sub M()
        Dim b As Base = New Base()
        [||]Goo("""", b)
    End Sub
End Class",
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub Goo(ByVal s As String, ByRef d As Derived)
    End Sub

    Private Sub Goo(ByVal s As String, ByVal i As Integer)
    End Sub

    Private Sub M()
        Dim b As Base = New Base()
        Goo("""", CType(b, Derived))
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function MethodCandidates2_ArgumentsInOrder_NoLabels() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub Goo(ByVal s As String, ByRef d As Derived)
    End Sub

    Private Sub Goo(ByVal s As String, ByRef d As Derived, ByVal i As Integer)
    End Sub

    Private Sub M()
        Dim b As Base = New Base()
        Goo("""", [||]b, 1)
    End Sub
End Class",
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub Goo(ByVal s As String, ByRef d As Derived)
    End Sub

    Private Sub Goo(ByVal s As String, ByRef d As Derived, ByVal i As Integer)
    End Sub

    Private Sub M()
        Dim b As Base = New Base()
        Goo("""", CType(b, Derived), 1)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function MethodCandidates3_ArgumentsInOrder_NoLabels_Params() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub Goo(ByVal s As String, ByVal d As Derived, ParamArray list As Object())
    End Sub

    Private Sub M()
        Dim b As Base = New Base()
        [|Goo("""", b)|]
    End Sub
End Class",
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub Goo(ByVal s As String, ByVal d As Derived, ParamArray list As Object())
    End Sub

    Private Sub M()
        Dim b As Base = New Base()
        Goo("""", CType(b, Derived))
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function MethodCandidates4_ArgumentsInOrder_NoLabels_Params() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub Goo(ByVal s As String, ByVal d As Derived, ParamArray list As Object())
    End Sub

    Private Sub M()
        Dim b As Base = New Base()
        [|Goo("""", b, 1, 2, 3)|]
    End Sub
End Class",
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub Goo(ByVal s As String, ByVal d As Derived, ParamArray list As Object())
    End Sub

    Private Sub M()
        Dim b As Base = New Base()
        Goo("""", CType(b, Derived), 1, 2, 3)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function MethodCandidates5_ArgumentsInOrder_NoLabels_Params() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub Goo(ByVal s As String, ByVal d As Derived, ParamArray list As Derived())
    End Sub

    Private Sub M()
        Dim b As Base = New Base()
        Dim d As Derived = New Derived()
        [|Goo("""", d, b, b)|]
    End Sub
End Class",
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub Goo(ByVal s As String, ByVal d As Derived, ParamArray list As Derived())
    End Sub

    Private Sub M()
        Dim b As Base = New Base()
        Dim d As Derived = New Derived()
        Goo("""", d, CType(b, Derived), b)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function MethodCandidates6_ArgumentsOutOfOrder_NoLabels() As Task
            Await TestMissingInRegularAndScriptAsync(
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub Goo(ByVal s As String, ByVal d As Derived)
    End Sub

    Private Sub M()
        Dim b As Base = New Base()
        [|Goo(b, """")|]
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function MethodCandidates7_ArgumentsInOrder_SomeLabels() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub Goo(ByVal s As String, ByRef d As Derived, i As Integer)
    End Sub

    Private Sub M()
        Dim b As Base = New Base()
        [|Goo("""", d:=b, 1)|]
    End Sub
End Class",
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub Goo(ByVal s As String, ByRef d As Derived, i As Integer)
    End Sub

    Private Sub M()
        Dim b As Base = New Base()
        Goo("""", d:=CType(b, Derived), 1)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function MethodCandidates8_ArgumentsInOrder_SomeLabels_Params() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub Goo(ByVal s As String, ByVal d As Derived, ParamArray list As Object())
    End Sub

    Private Sub M()
        Dim b As Base = New Base()
        Dim strlist = New String(0) {}
        [|Goo("""", d:=b, strlist)|]
    End Sub
End Class",
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub Goo(ByVal s As String, ByVal d As Derived, ParamArray list As Object())
    End Sub

    Private Sub M()
        Dim b As Base = New Base()
        Dim strlist = New String(0) {}
        Goo("""", d:=CType(b, Derived), strlist)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function MethodCandidates9_ArgumentsInOrder_SomeLabels_Params() As Task
            Await TestMissingInRegularAndScriptAsync(
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub Goo(ByVal s As String, ByVal d As Derived, ParamArray list As Object())
    End Sub

    Private Sub M()
        Dim b As Base = New Base()
        Dim strlist = New String(0) {}
        [|Goo("""", d:=b, list:=strlist)|]
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function MethodCandidates10_ArgumentsInOrder_SomeLabels_Params_OmittedArgument() As Task
            Await TestMissingInRegularAndScriptAsync(
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub Goo(ByVal s As String, ByVal d As Derived, ParamArray list As Object())
    End Sub

    Private Sub M()
        Dim b As Base = New Base()
        Dim strlist = New String(0) {}
        [|Goo("""", d:=b, )|]
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function MethodCandidates11_ArgumentsOutOfOrder_SomeLabels() As Task
            Await TestMissingInRegularAndScriptAsync(
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub Goo(ByVal s As String, ByVal d As Derived, i As Integer)
    End Sub

    Private Sub M()
        Dim b As Base = New Base()
        [|Goo(d:=b, """", 1)|]
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function MethodCandidates12_ArgumentsOutOfOrder_SomeLabels() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub Goo(ByVal s As String, ByVal d As Derived, i As Integer)
    End Sub

    Private Sub M()
        Dim b As Base = New Base()
        [|Goo("""", i:=1, d:=b)|]
    End Sub
End Class",
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub Goo(ByVal s As String, ByVal d As Derived, i As Integer)
    End Sub

    Private Sub M()
        Dim b As Base = New Base()
        Goo("""", i:=1, d:=CType(b, Derived))
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function MethodCandidates13_ArgumentsOutOfOrder_SomeLabels() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub Goo(ByVal s As String, ByVal d As Derived, ParamArray list As Object())
    End Sub

    Private Sub M()
        Dim b As Base = New Base()
        Dim strlist = New String(0) {}
        [|Goo(s:="""", d:=b, strlist)|]
    End Sub
End Class",
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub Goo(ByVal s As String, ByVal d As Derived, ParamArray list As Object())
    End Sub

    Private Sub M()
        Dim b As Base = New Base()
        Dim strlist = New String(0) {}
        Goo(s:="""", d:=CType(b, Derived), strlist)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function ConstructorCandidates() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Class Test
        Public Sub New(ByVal s As String, ByVal d As Derived, ByVal i As Integer)
        End Sub
    End Class

    Private Sub M()
        Dim b As Base = New Base()
        Dim t As Test = [|New Test(d:=b, s:="""", i:=1)|]
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
        Public Sub New(ByVal s As String, ByVal d As Derived, ByVal i As Integer)
        End Sub
    End Class

    Private Sub M()
        Dim b As Base = New Base()
        Dim t As Test = New Test(d:=CType(b, Derived), s:="""", i:=1)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function MultipleOptions1() As Task
            Dim initialMarkup = "
Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Class Derived2
        Inherits Derived
    End Class

    Class Test
        Public Sub New(ByVal s As String, ByRef b As Base, ByVal i As Integer, ParamArray list As Object())
            [|Me.New(d:=b, s:=s, i:=i)|]
        End Sub

        Private Sub New(ByVal s As String, ByRef d As Derived2, ByVal i As Integer)
        End Sub

        Private Sub New(ByVal s As String, ByRef d As Derived, ByVal i As Integer)
        End Sub
    End Class
End Class"

            Dim workspace = CreateWorkspaceFromOptions(initialMarkup, New TestParameters())
            Dim actions = Await GetCodeActionsAsync(workspace, New TestParameters())
            Assert.Equal(2, actions.Item1.Length)

            Dim expect_format = "
Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Class Derived2
        Inherits Derived
    End Class

    Class Test
        Public Sub New(ByVal s As String, ByRef b As Base, ByVal i As Integer, ParamArray list As Object())
            Me.New(d:=CType(b, {0}), s:=s, i:=i)
        End Sub

        Private Sub New(ByVal s As String, ByRef d As Derived2, ByVal i As Integer)
        End Sub

        Private Sub New(ByVal s As String, ByRef d As Derived, ByVal i As Integer)
        End Sub
    End Class
End Class"
            Await TestInRegularAndScriptAsync(initialMarkup, String.Format(expect_format, "Derived"), index:=0,
                title:=String.Format(CodeFixesResources.Convert_type_to_0, "Derived"))

            Await TestInRegularAndScriptAsync(initialMarkup, String.Format(expect_format, "Derived2"), index:=1,
                title:=String.Format(CodeFixesResources.Convert_type_to_0, "Derived2"))
        End Function

        <Fact>
        Public Async Function MultipleOptions2() As Task
            Dim initialMarkup = "
Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Class Derived2
        Inherits Derived
    End Class

    Class Test
        Public Sub New(ByVal s As String, ByRef b As Base, ByVal i As Integer, ParamArray list As Object())
            [|Me.New(d:=b, s:=s, i:=i)|]
        End Sub

        Private Sub New(ByRef d As Derived2, ByVal s As String, ByVal i As Integer)
        End Sub

        Private Sub New(ByVal s As String, ByRef d As Derived, ByVal i As Integer)
        End Sub
    End Class
End Class"

            Dim workspace = CreateWorkspaceFromOptions(initialMarkup, New TestParameters())
            Dim actions = Await GetCodeActionsAsync(workspace, New TestParameters())
            Assert.Equal(2, actions.Item1.Length)

            Dim expect_format = "
Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Class Derived2
        Inherits Derived
    End Class

    Class Test
        Public Sub New(ByVal s As String, ByRef b As Base, ByVal i As Integer, ParamArray list As Object())
            Me.New(d:=CType(b, {0}), s:=s, i:=i)
        End Sub

        Private Sub New(ByRef d As Derived2, ByVal s As String, ByVal i As Integer)
        End Sub

        Private Sub New(ByVal s As String, ByRef d As Derived, ByVal i As Integer)
        End Sub
    End Class
End Class"
            Await TestInRegularAndScriptAsync(initialMarkup, String.Format(expect_format, "Derived"), index:=0,
                title:=String.Format(CodeFixesResources.Convert_type_to_0, "Derived"))

            Await TestInRegularAndScriptAsync(initialMarkup, String.Format(expect_format, "Derived2"), index:=1,
                title:=String.Format(CodeFixesResources.Convert_type_to_0, "Derived2"))
        End Function

        <Fact>
        Public Async Function MultipleOptions3() As Task
            Dim initialMarkup = "
Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Class Derived2
        Inherits Derived
    End Class

    Class Test
        Public Sub New(ByVal s As String, ByRef b As Base, ByVal i As Integer, ParamArray list As Object())
            [|Me.New(d:=b, s:=s, i:=i)|]
        End Sub

        Private Sub New(ByVal s As String, ByRef d As Derived2, ByVal i As Integer, ParamArray list As Object())
        End Sub

        Private Sub New(ByVal s As String, ByRef d As Derived, ByVal i As Integer)
        End Sub
    End Class
End Class"

            Dim expect = "
Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Class Derived2
        Inherits Derived
    End Class

    Class Test
        Public Sub New(ByVal s As String, ByRef b As Base, ByVal i As Integer, ParamArray list As Object())
            Me.New(d:=CType(b, Derived), s:=s, i:=i)
        End Sub

        Private Sub New(ByVal s As String, ByRef d As Derived2, ByVal i As Integer, ParamArray list As Object())
        End Sub

        Private Sub New(ByVal s As String, ByRef d As Derived, ByVal i As Integer)
        End Sub
    End Class
End Class"

            Await TestInRegularAndScriptAsync(initialMarkup, expect)
        End Function

        <Fact>
        Public Async Function MultipleOptions4() As Task
            Dim initialMarkup = "
Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Class Derived2
        Inherits Derived
    End Class

    Private Sub Goo(ByVal s As String, ByVal j As Integer, ByVal i As Integer, ByVal d As Derived)
    End Sub

    Private Sub Goo(ByVal s As String, ByVal i As Integer, ByVal d As Derived2)
    End Sub

    Private Sub M()
        Dim b As Base = New Base()
        Dim strlist = New String(0) {}
        [|Goo("""", 1, i:=1, d:=b)|]
    End Sub
End Class"

            Dim expect = "
Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Class Derived2
        Inherits Derived
    End Class

    Private Sub Goo(ByVal s As String, ByVal j As Integer, ByVal i As Integer, ByVal d As Derived)
    End Sub

    Private Sub Goo(ByVal s As String, ByVal i As Integer, ByVal d As Derived2)
    End Sub

    Private Sub M()
        Dim b As Base = New Base()
        Dim strlist = New String(0) {}
        Goo("""", 1, i:=1, d:=CType(b, Derived))
    End Sub
End Class"

            Await TestInRegularAndScriptAsync(initialMarkup, expect)
        End Function

        <Fact>
        Public Async Function MultipleOptions5() As Task
            Dim initialMarkup = "
Option Strict On
Class Program
    Class Base
        Public Shared Narrowing Operator CType(x As Base) As String
            Return """"
        End Operator
    End Class

    Class Derived
        Inherits Base
    End Class

    Class Derived2
        Inherits Derived
    End Class

    Private Sub Goo(ByRef d As Derived, ByVal s As String, ByVal i As Integer)
    End Sub

    Private Sub Goo(ByVal s As String, ByRef d As Derived2, ByVal i As Integer)
    End Sub

    Private Sub Goo(ByVal s As String, ByVal d As String, ByVal i As Integer)
    End Sub

    Private Sub M()
        Dim b As Base = New Base()
        [|Goo(s:="""", i:=1, d:=b)|]
    End Sub
End Class"

            Dim workspace = CreateWorkspaceFromOptions(initialMarkup, New TestParameters())
            Dim actions = Await GetCodeActionsAsync(workspace, New TestParameters())
            Assert.Equal(3, actions.Item1.Length)

            Dim expect_format = "
Option Strict On
Class Program
    Class Base
        Public Shared Narrowing Operator CType(x As Base) As String
            Return """"
        End Operator
    End Class

    Class Derived
        Inherits Base
    End Class

    Class Derived2
        Inherits Derived
    End Class

    Private Sub Goo(ByRef d As Derived, ByVal s As String, ByVal i As Integer)
    End Sub

    Private Sub Goo(ByVal s As String, ByRef d As Derived2, ByVal i As Integer)
    End Sub

    Private Sub Goo(ByVal s As String, ByVal d As String, ByVal i As Integer)
    End Sub

    Private Sub M()
        Dim b As Base = New Base()
        Goo(s:="""", i:=1, d:={0})
    End Sub
End Class"

            Await TestInRegularAndScriptAsync(initialMarkup, String.Format(expect_format, "CStr(b)"), index:=0,
                title:=String.Format(CodeFixesResources.Convert_type_to_0, "String"))

            Await TestInRegularAndScriptAsync(initialMarkup, String.Format(expect_format, "CType(b, Derived)"), index:=1,
                title:=String.Format(CodeFixesResources.Convert_type_to_0, "Derived"))

            Await TestInRegularAndScriptAsync(initialMarkup, String.Format(expect_format, "CType(b, Derived2)"), index:=2,
                title:=String.Format(CodeFixesResources.Convert_type_to_0, "Derived2"))
        End Function

        <Fact>
        Public Async Function MultipleOptions6() As Task
            Await TestMissingInRegularAndScriptAsync(
"Option Strict On
Class Program
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Class Derived2
        Inherits Derived
    End Class

    Private Sub Goo(ByVal d1 As Derived)
    End Sub

    Private Sub Goo(ByVal d2 As Derived2)
    End Sub

    Private Sub M()
        [|Goo(New Base())|]
    End Sub
End Class")
        End Function
    End Class
End Namespace
