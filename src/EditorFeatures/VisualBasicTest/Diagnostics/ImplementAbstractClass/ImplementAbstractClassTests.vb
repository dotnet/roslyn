' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict Off

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.ImplementAbstractClass

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.ImplementAbstractClass
    Partial Public Class ImplementAbstractClassTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
            Return New Tuple(Of DiagnosticAnalyzer, CodeFixProvider)(
                Nothing, New ImplementAbstractClassCodeFixProvider)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestSimpleCases() As Task
            Await TestAsync(
                NewLines("Public MustInherit Class Foo \n Public MustOverride Sub Foo(i As Integer) \n Protected MustOverride Function Bar(s As String, ByRef d As Double) As Boolean \n End Class \n Public Class [|Bar|] \n Inherits Foo \n End Class"),
                NewLines("Imports System \n Public MustInherit Class Foo \n Public MustOverride Sub Foo(i As Integer) \n Protected MustOverride Function Bar(s As String, ByRef d As Double) As Boolean \n End Class \n Public Class Bar \n Inherits Foo \n Public Overrides Sub Foo(i As Integer) \n Throw New NotImplementedException() \n End Sub \n Protected Overrides Function Bar(s As String, ByRef d As Double) As Boolean \n Throw New NotImplementedException() \n End Function \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalIntParameter() As Task
            Await TestAsync(
                NewLines("MustInherit Class b \n Public MustOverride Sub g(Optional x As Integer = 3) \n End Class \n Class [|c|] \n Inherits b \n End Class"),
                NewLines("Imports System \n MustInherit Class b \n Public MustOverride Sub g(Optional x As Integer = 3) \n End Class \n Class c \n Inherits b \n Public Overrides Sub g(Optional x As Integer = 3) \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalTrueParameter() As Task
            Await TestAsync(
                NewLines("MustInherit Class b \n Public MustOverride Sub g(Optional x As Boolean = True) \n End Class \n Class [|c|] \n Inherits b \n End Class"),
                NewLines("Imports System \n MustInherit Class b \n Public MustOverride Sub g(Optional x As Boolean = True) \n End Class \n Class c \n Inherits b \n Public Overrides Sub g(Optional x As Boolean = True) \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalFalseParameter() As Task
            Await TestAsync(
                NewLines("MustInherit Class b \n Public MustOverride Sub g(Optional x As Boolean = False) \n End Class \n Class [|c|] \n Inherits b \n End Class"),
                NewLines("Imports System \n MustInherit Class b \n Public MustOverride Sub g(Optional x As Boolean = False) \n End Class \n Class c \n Inherits b \n Public Overrides Sub g(Optional x As Boolean = False) \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalStringParameter() As Task
            Await TestAsync(
                NewLines("MustInherit Class b \n Public MustOverride Sub g(Optional x As String = ""a"") \n End Class \n Class [|c|] \n Inherits b \n End Class"),
                NewLines("Imports System \n MustInherit Class b \n Public MustOverride Sub g(Optional x As String = ""a"") \n End Class \n Class c \n Inherits b \n Public Overrides Sub g(Optional x As String = ""a"") \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalCharParameter() As Task
            Await TestAsync(
                NewLines("MustInherit Class b \n Public MustOverride Sub g(Optional x As Char = ""c""c) \n End Class \n Class [|c|] \n Inherits b \n End Class"),
                NewLines("Imports System \n MustInherit Class b \n Public MustOverride Sub g(Optional x As Char = ""c""c) \n End Class \n Class c \n Inherits b \n Public Overrides Sub g(Optional x As Char = ""c""c) \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalLongParameter() As Task
            Await TestAsync(
                NewLines("MustInherit Class b \n Public MustOverride Sub g(Optional x As Long = 3) \n End Class \n Class [|c|] \n Inherits b \n End Class"),
                NewLines("Imports System \n MustInherit Class b \n Public MustOverride Sub g(Optional x As Long = 3) \n End Class \n Class c \n Inherits b \n Public Overrides Sub g(Optional x As Long = 3) \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalShortParameter() As Task
            Await TestAsync(
                NewLines("MustInherit Class b \n Public MustOverride Sub g(Optional x As Short = 3) \n End Class \n Class [|c|] \n Inherits b \n End Class"),
                NewLines("Imports System \n MustInherit Class b \n Public MustOverride Sub g(Optional x As Short = 3) \n End Class \n Class c \n Inherits b \n Public Overrides Sub g(Optional x As Short = 3) \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalUShortParameter() As Task
            Await TestAsync(
                NewLines("MustInherit Class b \n Public MustOverride Sub g(Optional x As UShort = 3) \n End Class \n Class [|c|] \n Inherits b \n End Class"),
                NewLines("Imports System \n MustInherit Class b \n Public MustOverride Sub g(Optional x As UShort = 3) \n End Class \n Class c \n Inherits b \n Public Overrides Sub g(Optional x As UShort = 3) \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalNegativeIntParameter() As Task
            Await TestAsync(
                NewLines("MustInherit Class b \n Public MustOverride Sub g(Optional x As Integer = -3) \n End Class \n Class [|c|] \n Inherits b \n End Class"),
                NewLines("Imports System \n MustInherit Class b \n Public MustOverride Sub g(Optional x As Integer = -3) \n End Class \n Class c \n Inherits b \n Public Overrides Sub g(Optional x As Integer = -3) \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalUIntParameter() As Task
            Await TestAsync(
                NewLines("MustInherit Class b \n Public MustOverride Sub g(Optional x As UInteger = 3) \n End Class \n Class [|c|] \n Inherits b \n End Class"),
                NewLines("Imports System \n MustInherit Class b \n Public MustOverride Sub g(Optional x As UInteger = 3) \n End Class \n Class c \n Inherits b \n Public Overrides Sub g(Optional x As UInteger = 3) \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalULongParameter() As Task
            Await TestAsync(
                NewLines("MustInherit Class b \n Public MustOverride Sub g(Optional x As ULong = 3) \n End Class \n Class [|c|] \n Inherits b \n End Class"),
                NewLines("Imports System \n MustInherit Class b \n Public MustOverride Sub g(Optional x As ULong = 3) \n End Class \n Class c \n Inherits b \n Public Overrides Sub g(Optional x As ULong = 3) \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalDecimalParameter() As Task
            Await TestAsync(
                NewLines("MustInherit Class b \n Public MustOverride Sub g(Optional x As Decimal = 3) \n End Class \n Class [|c|] \n Inherits b \n End Class"),
                NewLines("Imports System \n MustInherit Class b \n Public MustOverride Sub g(Optional x As Decimal = 3) \n End Class \n Class c \n Inherits b \n Public Overrides Sub g(Optional x As Decimal = 3) \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalDoubleParameter() As Task
            Await TestAsync(
                NewLines("MustInherit Class b \n Public MustOverride Sub g(Optional x As Double = 3) \n End Class \n Class [|c|] \n Inherits b \n End Class"),
                NewLines("Imports System \n MustInherit Class b \n Public MustOverride Sub g(Optional x As Double = 3) \n End Class \n Class c \n Inherits b \n Public Overrides Sub g(Optional x As Double = 3) \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalStructParameter() As Task
            Await TestAsync(
                NewLines("Structure S \n End Structure \n MustInherit Class b \n Public MustOverride Sub g(Optional x As S = Nothing) \n End Class \n Class [|c|] \n Inherits b \n End Class"),
                NewLines("Imports System \n Structure S \n End Structure \n MustInherit Class b \n Public MustOverride Sub g(Optional x As S = Nothing) \n End Class \n Class c \n Inherits b \n Public Overrides Sub g(Optional x As S = Nothing) \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(916114)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalNullableStructParameter() As Task
            Await TestAsync(
NewLines("Structure S \n End Structure \n MustInherit Class b \n Public MustOverride Sub g(Optional x As S? = Nothing) \n End Class \n Class [|c|] \n Inherits b \n End Class"),
NewLines("Imports System \n Structure S \n End Structure \n MustInherit Class b \n Public MustOverride Sub g(Optional x As S? = Nothing) \n End Class \n Class c \n Inherits b \n Public Overrides Sub g(Optional x As S? = Nothing) \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(916114)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalNullableIntParameter() As Task
            Await TestAsync(
NewLines("MustInherit Class b \n Public MustOverride Sub g(Optional x As Integer? = Nothing, Optional y As Integer? = 5) \n End Class \n Class [|c|] \n Inherits b \n End Class"),
NewLines("Imports System \n MustInherit Class b \n Public MustOverride Sub g(Optional x As Integer? = Nothing, Optional y As Integer? = 5) \n End Class \n Class c \n Inherits b \n Public Overrides Sub g(Optional x As Integer? = Nothing, Optional y As Integer? = 5) \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalClassParameter() As Task
            Await TestAsync(
                NewLines("Class S \n End Class \n MustInherit Class b \n Public MustOverride Sub g(Optional x As S = Nothing) \n End Class \n Class [|c|] \n Inherits b \n End Class"),
                NewLines("Imports System \n Class S \n End Class \n MustInherit Class b \n Public MustOverride Sub g(Optional x As S = Nothing) \n End Class \n Class c \n Inherits b \n Public Overrides Sub g(Optional x As S = Nothing) \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(544641)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestClassStatementTerminators1() As Task
            Await TestAsync(
NewLines("Imports System \n MustInherit Class D \n MustOverride Sub Foo() \n End Class \n Class [|C|] : Inherits D : End Class"),
NewLines("Imports System \n MustInherit Class D \n MustOverride Sub Foo() \n End Class \n Class C : Inherits D \n Public Overrides Sub Foo() \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(544641)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestClassStatementTerminators2() As Task
            Await TestAsync(
NewLines("Imports System \n MustInherit Class D \n MustOverride Sub Foo() \n End Class \n Class [|C|] : Inherits D : Implements IDisposable : End Class"),
NewLines("Imports System \n MustInherit Class D \n MustOverride Sub Foo() \n End Class \n Class C : Inherits D : Implements IDisposable \n Public Overrides Sub Foo() \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(530737)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestRenameTypeParameters() As Task
            Await TestAsync(
NewLines("MustInherit Class A(Of T) \n MustOverride Sub Foo(Of S As T)() \n End Class \n Class [|C(Of S)|] \n Inherits A(Of S) \n End Class"),
NewLines("Imports System \n MustInherit Class A(Of T) \n MustOverride Sub Foo(Of S As T)() \n End Class \n Class C(Of S) \n Inherits A(Of S) \n Public Overrides Sub Foo(Of S1 As S)() \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestFormattingInImplementAbstractClass() As Task
            Await TestAsync(
<Text>Imports System

Class S
End Class
MustInherit Class b
    Public MustOverride Sub g(Optional x As S = Nothing)
End Class
Class [|c|]
    Inherits b
End Class
</Text>.Value.Replace(vbLf, vbCrLf),
<Text>Imports System

Class S
End Class
MustInherit Class b
    Public MustOverride Sub g(Optional x As S = Nothing)
End Class
Class c
    Inherits b

    Public Overrides Sub g(Optional x As S = Nothing)
        Throw New NotImplementedException()
    End Sub
End Class
</Text>.Value.Replace(vbLf, vbCrLf),
compareTokens:=False)
        End Function

        <WorkItem(2407, "https://github.com/dotnet/roslyn/issues/2407")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestImplementClassWithInaccessibleMembers() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Globalization \n Class [|x|] \n Inherits EastAsianLunisolarCalendar \n End Class"),
NewLines("Imports System \n Imports System.Globalization \n Class x \n Inherits EastAsianLunisolarCalendar \n Public Overrides ReadOnly Property Eras As Integer() \n Get \n Throw New NotImplementedException() \n End Get \n End Property \n Friend Overrides ReadOnly Property CalEraInfo As EraInfo() \n Get \n Throw New NotImplementedException() \n End Get \n End Property \n Friend Overrides ReadOnly Property MaxCalendarYear As Integer \n Get \n Throw New NotImplementedException() \n End Get \n End Property \n Friend Overrides ReadOnly Property MaxDate As Date \n Get \n Throw New NotImplementedException() \n End Get \n End Property \n Friend Overrides ReadOnly Property MinCalendarYear As Integer \n Get \n Throw New NotImplementedException() \n End Get \n End Property \n Friend Overrides ReadOnly Property MinDate As Date \n Get \n Throw New NotImplementedException() \n End Get \n End Property \n Public Overrides Function GetEra(time As Date) As Integer \n Throw New NotImplementedException() \n End Function \n Friend Overrides Function GetGregorianYear(year As Integer, era As Integer) As Integer \n Throw New NotImplementedException() \n End Function \n Friend Overrides Function GetYear(year As Integer, time As Date) As Integer \n Throw New NotImplementedException() \n End Function \n Friend Overrides Function GetYearInfo(LunarYear As Integer, Index As Integer) As Integer \n Throw New NotImplementedException() \n End Function \n End Class"))
        End Function
    End Class
End Namespace
