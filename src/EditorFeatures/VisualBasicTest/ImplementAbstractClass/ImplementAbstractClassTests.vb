' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.ImplementType
Imports Microsoft.CodeAnalysis.SymbolSearch
Imports Microsoft.CodeAnalysis.VisualBasic.ImplementAbstractClass

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ImplementAbstractClass
    Partial Public Class ImplementAbstractClassTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (Nothing, New VisualBasicImplementAbstractClassCodeFixProvider)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestSimpleCases() As Task
            Await TestInRegularAndScriptAsync(
"Public MustInherit Class Goo
    Public MustOverride Sub Goo(i As Integer)
    Protected MustOverride Function Bar(s As String, ByRef d As Double) As Boolean
End Class
Public Class [|Bar|]
    Inherits Goo
End Class",
"Public MustInherit Class Goo
    Public MustOverride Sub Goo(i As Integer)
    Protected MustOverride Function Bar(s As String, ByRef d As Double) As Boolean
End Class
Public Class Bar
    Inherits Goo

    Public Overrides Sub Goo(i As Integer)
        Throw New System.NotImplementedException()
    End Sub

    Protected Overrides Function Bar(s As String, ByRef d As Double) As Boolean
        Throw New System.NotImplementedException()
    End Function
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestMethodWithTupleNames() As Task
            Await TestInRegularAndScriptAsync(
"Public MustInherit Class Base
    Protected MustOverride Function Bar(x As (a As Integer, Integer)) As (c As Integer, Integer)
End Class
Public Class [|Derived|]
    Inherits Base
End Class",
"Public MustInherit Class Base
    Protected MustOverride Function Bar(x As (a As Integer, Integer)) As (c As Integer, Integer)
End Class
Public Class Derived
    Inherits Base

    Protected Overrides Function Bar(x As (a As Integer, Integer)) As (c As Integer, Integer)
        Throw New System.NotImplementedException()
    End Function
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalIntParameter() As Task
            Await TestInRegularAndScriptAsync(
"MustInherit Class b
    Public MustOverride Sub g(Optional x As Integer = 3)
End Class
Class [|c|]
    Inherits b
End Class",
"MustInherit Class b
    Public MustOverride Sub g(Optional x As Integer = 3)
End Class
Class c
    Inherits b

    Public Overrides Sub g(Optional x As Integer = 3)
        Throw New System.NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalTrueParameter() As Task
            Await TestInRegularAndScriptAsync(
"MustInherit Class b
    Public MustOverride Sub g(Optional x As Boolean = True)
End Class
Class [|c|]
    Inherits b
End Class",
"MustInherit Class b
    Public MustOverride Sub g(Optional x As Boolean = True)
End Class
Class c
    Inherits b

    Public Overrides Sub g(Optional x As Boolean = True)
        Throw New System.NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalFalseParameter() As Task
            Await TestInRegularAndScriptAsync(
"MustInherit Class b
    Public MustOverride Sub g(Optional x As Boolean = False)
End Class
Class [|c|]
    Inherits b
End Class",
"MustInherit Class b
    Public MustOverride Sub g(Optional x As Boolean = False)
End Class
Class c
    Inherits b

    Public Overrides Sub g(Optional x As Boolean = False)
        Throw New System.NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalStringParameter() As Task
            Await TestInRegularAndScriptAsync(
"MustInherit Class b
    Public MustOverride Sub g(Optional x As String = ""a"")
End Class
Class [|c|]
    Inherits b
End Class",
"MustInherit Class b
    Public MustOverride Sub g(Optional x As String = ""a"")
End Class
Class c
    Inherits b

    Public Overrides Sub g(Optional x As String = ""a"")
        Throw New System.NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalCharParameter() As Task
            Await TestInRegularAndScriptAsync(
"MustInherit Class b
    Public MustOverride Sub g(Optional x As Char = ""c""c)
End Class
Class [|c|]
    Inherits b
End Class",
"MustInherit Class b
    Public MustOverride Sub g(Optional x As Char = ""c""c)
End Class
Class c
    Inherits b

    Public Overrides Sub g(Optional x As Char = ""c""c)
        Throw New System.NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalLongParameter() As Task
            Await TestInRegularAndScriptAsync(
"MustInherit Class b
    Public MustOverride Sub g(Optional x As Long = 3)
End Class
Class [|c|]
    Inherits b
End Class",
"MustInherit Class b
    Public MustOverride Sub g(Optional x As Long = 3)
End Class
Class c
    Inherits b

    Public Overrides Sub g(Optional x As Long = 3)
        Throw New System.NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalShortParameter() As Task
            Await TestInRegularAndScriptAsync(
"MustInherit Class b
    Public MustOverride Sub g(Optional x As Short = 3)
End Class
Class [|c|]
    Inherits b
End Class",
"MustInherit Class b
    Public MustOverride Sub g(Optional x As Short = 3)
End Class
Class c
    Inherits b

    Public Overrides Sub g(Optional x As Short = 3)
        Throw New System.NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalUShortParameter() As Task
            Await TestInRegularAndScriptAsync(
"MustInherit Class b
    Public MustOverride Sub g(Optional x As UShort = 3)
End Class
Class [|c|]
    Inherits b
End Class",
"MustInherit Class b
    Public MustOverride Sub g(Optional x As UShort = 3)
End Class
Class c
    Inherits b

    Public Overrides Sub g(Optional x As UShort = 3)
        Throw New System.NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalNegativeIntParameter() As Task
            Await TestInRegularAndScriptAsync(
"MustInherit Class b
    Public MustOverride Sub g(Optional x As Integer = -3)
End Class
Class [|c|]
    Inherits b
End Class",
"MustInherit Class b
    Public MustOverride Sub g(Optional x As Integer = -3)
End Class
Class c
    Inherits b

    Public Overrides Sub g(Optional x As Integer = -3)
        Throw New System.NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalUIntParameter() As Task
            Await TestInRegularAndScriptAsync(
"MustInherit Class b
    Public MustOverride Sub g(Optional x As UInteger = 3)
End Class
Class [|c|]
    Inherits b
End Class",
"MustInherit Class b
    Public MustOverride Sub g(Optional x As UInteger = 3)
End Class
Class c
    Inherits b

    Public Overrides Sub g(Optional x As UInteger = 3)
        Throw New System.NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalULongParameter() As Task
            Await TestInRegularAndScriptAsync(
"MustInherit Class b
    Public MustOverride Sub g(Optional x As ULong = 3)
End Class
Class [|c|]
    Inherits b
End Class",
"MustInherit Class b
    Public MustOverride Sub g(Optional x As ULong = 3)
End Class
Class c
    Inherits b

    Public Overrides Sub g(Optional x As ULong = 3)
        Throw New System.NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalDecimalParameter() As Task
            Await TestInRegularAndScriptAsync(
"MustInherit Class b
    Public MustOverride Sub g(Optional x As Decimal = 3)
End Class
Class [|c|]
    Inherits b
End Class",
"MustInherit Class b
    Public MustOverride Sub g(Optional x As Decimal = 3)
End Class
Class c
    Inherits b

    Public Overrides Sub g(Optional x As Decimal = 3)
        Throw New System.NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalDoubleParameter() As Task
            Await TestInRegularAndScriptAsync(
"MustInherit Class b
    Public MustOverride Sub g(Optional x As Double = 3)
End Class
Class [|c|]
    Inherits b
End Class",
"MustInherit Class b
    Public MustOverride Sub g(Optional x As Double = 3)
End Class
Class c
    Inherits b

    Public Overrides Sub g(Optional x As Double = 3)
        Throw New System.NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalStructParameter() As Task
            Await TestInRegularAndScriptAsync(
"Structure S
End Structure
MustInherit Class b
    Public MustOverride Sub g(Optional x As S = Nothing)
End Class
Class [|c|]
    Inherits b
End Class",
"Structure S
End Structure
MustInherit Class b
    Public MustOverride Sub g(Optional x As S = Nothing)
End Class
Class c
    Inherits b

    Public Overrides Sub g(Optional x As S = Nothing)
        Throw New System.NotImplementedException()
    End Sub
End Class")
        End Function

        <WorkItem(916114, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/916114")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalNullableStructParameter() As Task
            Await TestInRegularAndScriptAsync(
"Structure S
End Structure
MustInherit Class b
    Public MustOverride Sub g(Optional x As S? = Nothing)
End Class
Class [|c|]
    Inherits b
End Class",
"Structure S
End Structure
MustInherit Class b
    Public MustOverride Sub g(Optional x As S? = Nothing)
End Class
Class c
    Inherits b

    Public Overrides Sub g(Optional x As S? = Nothing)
        Throw New System.NotImplementedException()
    End Sub
End Class")
        End Function

        <WorkItem(916114, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/916114")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalNullableIntParameter() As Task
            Await TestInRegularAndScriptAsync(
"MustInherit Class b
    Public MustOverride Sub g(Optional x As Integer? = Nothing, Optional y As Integer? = 5)
End Class
Class [|c|]
    Inherits b
End Class",
"MustInherit Class b
    Public MustOverride Sub g(Optional x As Integer? = Nothing, Optional y As Integer? = 5)
End Class
Class c
    Inherits b

    Public Overrides Sub g(Optional x As Integer? = Nothing, Optional y As Integer? = 5)
        Throw New System.NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestOptionalClassParameter() As Task
            Await TestInRegularAndScriptAsync(
"Class S
End Class
MustInherit Class b
    Public MustOverride Sub g(Optional x As S = Nothing)
End Class
Class [|c|]
    Inherits b
End Class",
"Class S
End Class
MustInherit Class b
    Public MustOverride Sub g(Optional x As S = Nothing)
End Class
Class c
    Inherits b

    Public Overrides Sub g(Optional x As S = Nothing)
        Throw New System.NotImplementedException()
    End Sub
End Class")
        End Function

        <WorkItem(544641, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544641")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestClassStatementTerminators1() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
MustInherit Class D
    MustOverride Sub Goo()
End Class
Class [|C|] : Inherits D : End Class",
"Imports System
MustInherit Class D
    MustOverride Sub Goo()
End Class
Class C : Inherits D

    Public Overrides Sub Goo()
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <WorkItem(544641, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544641")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestClassStatementTerminators2() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
MustInherit Class D
    MustOverride Sub Goo()
End Class
Class [|C|] : Inherits D : Implements IDisposable : End Class",
"Imports System
MustInherit Class D
    MustOverride Sub Goo()
End Class
Class C : Inherits D : Implements IDisposable

    Public Overrides Sub Goo()
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <WorkItem(530737, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530737")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestRenameTypeParameters() As Task
            Await TestInRegularAndScriptAsync(
"MustInherit Class A(Of T)
    MustOverride Sub Goo(Of S As T)()
End Class
Class [|C(Of S)|]
    Inherits A(Of S)
End Class",
"MustInherit Class A(Of T)
    MustOverride Sub Goo(Of S As T)()
End Class
Class C(Of S)
    Inherits A(Of S)

    Public Overrides Sub Goo(Of S1 As S)()
        Throw New System.NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestFormattingInImplementAbstractClass() As Task
            Await TestInRegularAndScriptAsync(
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
</Text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <WorkItem(2407, "https://github.com/dotnet/roslyn/issues/2407")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Async Function TestImplementClassWithInaccessibleMembers() As Task
            Await TestInRegularAndScriptAsync(
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

    Friend Overrides ReadOnly Property MinCalendarYear As Integer
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Friend Overrides ReadOnly Property MaxCalendarYear As Integer
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Friend Overrides ReadOnly Property CalEraInfo As EraInfo()
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Friend Overrides ReadOnly Property MinDate As Date
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Friend Overrides ReadOnly Property MaxDate As Date
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Public Overrides Function GetEra(time As Date) As Integer
        Throw New NotImplementedException()
    End Function

    Friend Overrides Function GetYearInfo(LunarYear As Integer, Index As Integer) As Integer
        Throw New NotImplementedException()
    End Function

    Friend Overrides Function GetYear(year As Integer, time As Date) As Integer
        Throw New NotImplementedException()
    End Function

    Friend Overrides Function GetGregorianYear(year As Integer, era As Integer) As Integer
        Throw New NotImplementedException()
    End Function
End Class")
        End Function

        <WorkItem(13932, "https://github.com/dotnet/roslyn/issues/13932")>
        <WorkItem(5898, "https://github.com/dotnet/roslyn/issues/5898")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestAutoProperties() As Task
            Await TestInRegularAndScript1Async(
"MustInherit Class AbstractClass
    MustOverride ReadOnly Property ReadOnlyProp As Integer
    MustOverride Property ReadWriteProp As Integer
    MustOverride WriteOnly Property WriteOnlyProp As Integer
End Class

Class [|C|]
    Inherits AbstractClass

End Class",
"MustInherit Class AbstractClass
    MustOverride ReadOnly Property ReadOnlyProp As Integer
    MustOverride Property ReadWriteProp As Integer
    MustOverride WriteOnly Property WriteOnlyProp As Integer
End Class

Class C
    Inherits AbstractClass

    Public Overrides ReadOnly Property ReadOnlyProp As Integer
    Public Overrides Property ReadWriteProp As Integer

    Public Overrides WriteOnly Property WriteOnlyProp As Integer
        Set(value As Integer)
            Throw New System.NotImplementedException()
        End Set
    End Property
End Class", parameters:=New TestParameters(globalOptions:=[Option](ImplementTypeOptionsStorage.PropertyGenerationBehavior, ImplementTypePropertyGenerationBehavior.PreferAutoProperties)))
        End Function
    End Class
End Namespace
