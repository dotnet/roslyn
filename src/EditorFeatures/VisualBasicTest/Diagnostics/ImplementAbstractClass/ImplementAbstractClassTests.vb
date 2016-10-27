' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestSimpleCases() As Task
            Await TestAsync(
"Public MustInherit Class Foo
    Public MustOverride Sub Foo(i As Integer)
    Protected MustOverride Function Bar(s As String, ByRef d As Double) As Boolean
End Class
Public Class [|Bar|]
    Inherits Foo
End Class",
"Imports System
Public MustInherit Class Foo
    Public MustOverride Sub Foo(i As Integer)
    Protected MustOverride Function Bar(s As String, ByRef d As Double) As Boolean
End Class
Public Class Bar
    Inherits Foo
    Public Overrides Sub Foo(i As Integer)
        Throw New NotImplementedException()
    End Sub
    Protected Overrides Function Bar(s As String, ByRef d As Double) As Boolean
        Throw New NotImplementedException()
    End Function
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalIntParameter() As Task
            Await TestAsync(
"MustInherit Class b
    Public MustOverride Sub g(Optional x As Integer = 3)
End Class
Class [|c|]
    Inherits b
End Class",
"Imports System
MustInherit Class b
    Public MustOverride Sub g(Optional x As Integer = 3)
End Class
Class c
    Inherits b
    Public Overrides Sub g(Optional x As Integer = 3)
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalTrueParameter() As Task
            Await TestAsync(
"MustInherit Class b
    Public MustOverride Sub g(Optional x As Boolean = True)
End Class
Class [|c|]
    Inherits b
End Class",
"Imports System
MustInherit Class b
    Public MustOverride Sub g(Optional x As Boolean = True)
End Class
Class c
    Inherits b
    Public Overrides Sub g(Optional x As Boolean = True)
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalFalseParameter() As Task
            Await TestAsync(
"MustInherit Class b
    Public MustOverride Sub g(Optional x As Boolean = False)
End Class
Class [|c|]
    Inherits b
End Class",
"Imports System
MustInherit Class b
    Public MustOverride Sub g(Optional x As Boolean = False)
End Class
Class c
    Inherits b
    Public Overrides Sub g(Optional x As Boolean = False)
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalStringParameter() As Task
            Await TestAsync(
"MustInherit Class b
    Public MustOverride Sub g(Optional x As String = ""a"")
End Class
Class [|c|]
    Inherits b
End Class",
"Imports System
MustInherit Class b
    Public MustOverride Sub g(Optional x As String = ""a"")
End Class
Class c
    Inherits b
    Public Overrides Sub g(Optional x As String = ""a"")
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalCharParameter() As Task
            Await TestAsync(
"MustInherit Class b
    Public MustOverride Sub g(Optional x As Char = ""c""c)
End Class
Class [|c|]
    Inherits b
End Class",
"Imports System
MustInherit Class b
    Public MustOverride Sub g(Optional x As Char = ""c""c)
End Class
Class c
    Inherits b
    Public Overrides Sub g(Optional x As Char = ""c""c)
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalLongParameter() As Task
            Await TestAsync(
"MustInherit Class b
    Public MustOverride Sub g(Optional x As Long = 3)
End Class
Class [|c|]
    Inherits b
End Class",
"Imports System
MustInherit Class b
    Public MustOverride Sub g(Optional x As Long = 3)
End Class
Class c
    Inherits b
    Public Overrides Sub g(Optional x As Long = 3)
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalShortParameter() As Task
            Await TestAsync(
"MustInherit Class b
    Public MustOverride Sub g(Optional x As Short = 3)
End Class
Class [|c|]
    Inherits b
End Class",
"Imports System
MustInherit Class b
    Public MustOverride Sub g(Optional x As Short = 3)
End Class
Class c
    Inherits b
    Public Overrides Sub g(Optional x As Short = 3)
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalUShortParameter() As Task
            Await TestAsync(
"MustInherit Class b
    Public MustOverride Sub g(Optional x As UShort = 3)
End Class
Class [|c|]
    Inherits b
End Class",
"Imports System
MustInherit Class b
    Public MustOverride Sub g(Optional x As UShort = 3)
End Class
Class c
    Inherits b
    Public Overrides Sub g(Optional x As UShort = 3)
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalNegativeIntParameter() As Task
            Await TestAsync(
"MustInherit Class b
    Public MustOverride Sub g(Optional x As Integer = -3)
End Class
Class [|c|]
    Inherits b
End Class",
"Imports System
MustInherit Class b
    Public MustOverride Sub g(Optional x As Integer = -3)
End Class
Class c
    Inherits b
    Public Overrides Sub g(Optional x As Integer = -3)
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalUIntParameter() As Task
            Await TestAsync(
"MustInherit Class b
    Public MustOverride Sub g(Optional x As UInteger = 3)
End Class
Class [|c|]
    Inherits b
End Class",
"Imports System
MustInherit Class b
    Public MustOverride Sub g(Optional x As UInteger = 3)
End Class
Class c
    Inherits b
    Public Overrides Sub g(Optional x As UInteger = 3)
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalULongParameter() As Task
            Await TestAsync(
"MustInherit Class b
    Public MustOverride Sub g(Optional x As ULong = 3)
End Class
Class [|c|]
    Inherits b
End Class",
"Imports System
MustInherit Class b
    Public MustOverride Sub g(Optional x As ULong = 3)
End Class
Class c
    Inherits b
    Public Overrides Sub g(Optional x As ULong = 3)
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalDecimalParameter() As Task
            Await TestAsync(
"MustInherit Class b
    Public MustOverride Sub g(Optional x As Decimal = 3)
End Class
Class [|c|]
    Inherits b
End Class",
"Imports System
MustInherit Class b
    Public MustOverride Sub g(Optional x As Decimal = 3)
End Class
Class c
    Inherits b
    Public Overrides Sub g(Optional x As Decimal = 3)
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalDoubleParameter() As Task
            Await TestAsync(
"MustInherit Class b
    Public MustOverride Sub g(Optional x As Double = 3)
End Class
Class [|c|]
    Inherits b
End Class",
"Imports System
MustInherit Class b
    Public MustOverride Sub g(Optional x As Double = 3)
End Class
Class c
    Inherits b
    Public Overrides Sub g(Optional x As Double = 3)
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalStructParameter() As Task
            Await TestAsync(
"Structure S
End Structure
MustInherit Class b
    Public MustOverride Sub g(Optional x As S = Nothing)
End Class
Class [|c|]
    Inherits b
End Class",
"Imports System
Structure S
End Structure
MustInherit Class b
    Public MustOverride Sub g(Optional x As S = Nothing)
End Class
Class c
    Inherits b
    Public Overrides Sub g(Optional x As S = Nothing)
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <WorkItem(916114, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/916114")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalNullableStructParameter() As Task
            Await TestAsync(
"Structure S
End Structure
MustInherit Class b
    Public MustOverride Sub g(Optional x As S? = Nothing)
End Class
Class [|c|]
    Inherits b
End Class",
"Imports System
Structure S
End Structure
MustInherit Class b
    Public MustOverride Sub g(Optional x As S? = Nothing)
End Class
Class c
    Inherits b
    Public Overrides Sub g(Optional x As S? = Nothing)
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <WorkItem(916114, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/916114")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalNullableIntParameter() As Task
            Await TestAsync(
"MustInherit Class b
    Public MustOverride Sub g(Optional x As Integer? = Nothing, Optional y As Integer? = 5)
End Class
Class [|c|]
    Inherits b
End Class",
"Imports System
MustInherit Class b
    Public MustOverride Sub g(Optional x As Integer? = Nothing, Optional y As Integer? = 5)
End Class
Class c
    Inherits b
    Public Overrides Sub g(Optional x As Integer? = Nothing, Optional y As Integer? = 5)
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalClassParameter() As Task
            Await TestAsync(
"Class S
End Class
MustInherit Class b
    Public MustOverride Sub g(Optional x As S = Nothing)
End Class
Class [|c|]
    Inherits b
End Class",
"Imports System
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
End Class")
        End Function

        <WorkItem(544641, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544641")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestClassStatementTerminators1() As Task
            Await TestAsync(
"Imports System
MustInherit Class D
    MustOverride Sub Foo()
End Class
Class [|C|] : Inherits D : End Class",
"Imports System
MustInherit Class D
    MustOverride Sub Foo()
End Class
Class C : Inherits D
    Public Overrides Sub Foo()
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <WorkItem(544641, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544641")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestClassStatementTerminators2() As Task
            Await TestAsync(
"Imports System
MustInherit Class D
    MustOverride Sub Foo()
End Class
Class [|C|] : Inherits D : Implements IDisposable : End Class",
"Imports System
MustInherit Class D
    MustOverride Sub Foo()
End Class
Class C : Inherits D : Implements IDisposable
    Public Overrides Sub Foo()
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <WorkItem(530737, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530737")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestRenameTypeParameters() As Task
            Await TestAsync(
"MustInherit Class A(Of T)
    MustOverride Sub Foo(Of S As T)()
End Class
Class [|C(Of S)|]
    Inherits A(Of S)
End Class",
"Imports System
MustInherit Class A(Of T)
    MustOverride Sub Foo(Of S As T)()
End Class
Class C(Of S)
    Inherits A(Of S)
    Public Overrides Sub Foo(Of S1 As S)()
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
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
"Imports System
Imports System.Globalization
Class [|x|]
    Inherits EastAsianLunisolarCalendar
End Class",
"Imports System
Imports System.Globalization
Class x
    Inherits EastAsianLunisolarCalendar
    Public Overrides ReadOnly Property Eras As Integer()
        Get
            Throw New NotImplementedException()
        End Get
    End Property
    Friend Overrides ReadOnly Property CalEraInfo As EraInfo()
        Get
            Throw New NotImplementedException()
        End Get
    End Property
    Friend Overrides ReadOnly Property MaxCalendarYear As Integer
        Get
            Throw New NotImplementedException()
        End Get
    End Property
    Friend Overrides ReadOnly Property MaxDate As Date
        Get
            Throw New NotImplementedException()
        End Get
    End Property
    Friend Overrides ReadOnly Property MinCalendarYear As Integer
        Get
            Throw New NotImplementedException()
        End Get
    End Property
    Friend Overrides ReadOnly Property MinDate As Date
        Get
            Throw New NotImplementedException()
        End Get
    End Property
    Public Overrides Function GetEra(time As Date) As Integer
        Throw New NotImplementedException()
    End Function
    Friend Overrides Function GetGregorianYear(year As Integer, era As Integer) As Integer
        Throw New NotImplementedException()
    End Function
    Friend Overrides Function GetYear(year As Integer, time As Date) As Integer
        Throw New NotImplementedException()
    End Function
    Friend Overrides Function GetYearInfo(LunarYear As Integer, Index As Integer) As Integer
        Throw New NotImplementedException()
    End Function
End Class")
        End Function
    End Class
End Namespace