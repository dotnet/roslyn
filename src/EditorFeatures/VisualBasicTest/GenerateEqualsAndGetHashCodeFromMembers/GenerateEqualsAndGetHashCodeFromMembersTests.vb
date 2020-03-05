﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.GenerateEqualsAndGetHashCodeFromMembers
Imports Microsoft.CodeAnalysis.PickMembers
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.GenerateConstructorFromMembers
    Public Class GenerateEqualsAndGetHashCodeFromMembersTests
        Inherits AbstractVisualBasicCodeActionTest

        Private Const GenerateOperatorsId = GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider.GenerateOperatorsId
        Private Const ImplementIEquatableId = GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider.ImplementIEquatableId

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider(
                DirectCast(parameters.fixProviderData, IPickMembersService))
        End Function

        <WorkItem(541991, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541991")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)>
        Public Async Function TestEqualsOnSingleField() As Task
            Await TestInRegularAndScriptAsync(
"Class Z
    [|Private a As Integer|]
End Class",
"Class Z
    Private a As Integer

    Public Overrides Function Equals(obj As Object) As Boolean
        Dim z = TryCast(obj, Z)
        Return z IsNot Nothing AndAlso
               a = z.a
    End Function
End Class")
        End Function

        <WorkItem(541991, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541991")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)>
        Public Async Function TestGetHashCodeOnSingleField() As Task
            Await TestInRegularAndScriptAsync(
"Class Z
    [|Private a As Integer|]
End Class",
"Class Z
    Private a As Integer

    Public Overrides Function Equals(obj As Object) As Boolean
        Dim z = TryCast(obj, Z)
        Return z IsNot Nothing AndAlso
               a = z.a
    End Function

    Public Overrides Function GetHashCode() As Integer
        Dim hashCode As Long = -468965076
        hashCode = (hashCode * -1521134295 + a.GetHashCode()).GetHashCode()
        Return hashCode
    End Function
End Class",
index:=1)
        End Function

        <WorkItem(541991, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541991")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)>
        Public Async Function TestBothOnSingleField() As Task
            Await TestInRegularAndScriptAsync(
"Class Z
    [|Private a As Integer|]
End Class",
"Class Z
    Private a As Integer

    Public Overrides Function Equals(obj As Object) As Boolean
        Dim z = TryCast(obj, Z)
        Return z IsNot Nothing AndAlso
               a = z.a
    End Function

    Public Overrides Function GetHashCode() As Integer
        Dim hashCode As Long = -468965076
        hashCode = (hashCode * -1521134295 + a.GetHashCode()).GetHashCode()
        Return hashCode
    End Function
End Class",
index:=1)
        End Function

        <WorkItem(545205, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545205")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)>
        Public Async Function TestTypeWithNumberInName() As Task
            Await TestInRegularAndScriptAsync(
"Partial Class c1(Of V As {New}, U)
    [|Dim x As New V|]
End Class",
"Imports System.Collections.Generic

Partial Class c1(Of V As {New}, U)
    Dim x As New V

    Public Overrides Function Equals(obj As Object) As Boolean
        Dim c = TryCast(obj, c1(Of V, U))
        Return c IsNot Nothing AndAlso
               EqualityComparer(Of V).Default.Equals(x, c.x)
    End Function
End Class")
        End Function

        <WorkItem(17643, "https://github.com/dotnet/roslyn/issues/17643")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)>
        Public Async Function TestWithDialogNoBackingField() As Task
            Await TestWithPickMembersDialogAsync(
"
Class Program
    Public Property F() As Integer
    [||]
End Class",
"
Class Program
    Public Property F() As Integer

    Public Overrides Function Equals(obj As Object) As Boolean
        Dim program = TryCast(obj, Program)
        Return program IsNot Nothing AndAlso
               F = program.F
    End Function
End Class",
chosenSymbols:=Nothing)
        End Function

        <WorkItem(25690, "https://github.com/dotnet/roslyn/issues/25690")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)>
        Public Async Function TestWithDialogNoParameterizedProperty() As Task
            Await TestWithPickMembersDialogAsync(
"
Class Program
    Public ReadOnly Property P() As Integer
        Get
            Return 0
        End Get
    End Property
    Public ReadOnly Property I(index As Integer) As Integer
        Get
            Return 0
        End Get
    End Property
    [||]
End Class",
"
Class Program
    Public ReadOnly Property P() As Integer
        Get
            Return 0
        End Get
    End Property
    Public ReadOnly Property I(index As Integer) As Integer
        Get
            Return 0
        End Get
    End Property

    Public Overrides Function Equals(obj As Object) As Boolean
        Dim program = TryCast(obj, Program)
        Return program IsNot Nothing AndAlso
               P = program.P
    End Function
End Class",
chosenSymbols:=Nothing)
        End Function

        <WorkItem(25690, "https://github.com/dotnet/roslyn/issues/25690")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)>
        Public Async Function TestWithDialogNoIndexer() As Task
            Await TestWithPickMembersDialogAsync(
"
Class Program
    Public ReadOnly Property P() As Integer
        Get
            Return 0
        End Get
    End Property
    Default Public ReadOnly Property I(index As Integer) As Integer
        Get
            Return 0
        End Get
    End Property
    [||]
End Class",
"
Class Program
    Public ReadOnly Property P() As Integer
        Get
            Return 0
        End Get
    End Property
    Default Public ReadOnly Property I(index As Integer) As Integer
        Get
            Return 0
        End Get
    End Property

    Public Overrides Function Equals(obj As Object) As Boolean
        Dim program = TryCast(obj, Program)
        Return program IsNot Nothing AndAlso
               P = program.P
    End Function
End Class",
chosenSymbols:=Nothing)
        End Function

        <WorkItem(25707, "https://github.com/dotnet/roslyn/issues/25707")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)>
        Public Async Function TestWithDialogNoSetterOnlyProperty() As Task
            Await TestWithPickMembersDialogAsync(
"
Class Program
    Public ReadOnly Property P() As Integer
        Get
            Return 0
        End Get
    End Property
    Public WriteOnly Property S() As Integer
        Set
        End Set
    End Property
    [||]
End Class",
"
Class Program
    Public ReadOnly Property P() As Integer
        Get
            Return 0
        End Get
    End Property
    Public WriteOnly Property S() As Integer
        Set
        End Set
    End Property

    Public Overrides Function Equals(obj As Object) As Boolean
        Dim program = TryCast(obj, Program)
        Return program IsNot Nothing AndAlso
               P = program.P
    End Function
End Class",
chosenSymbols:=Nothing)
        End Function

        <WorkItem(41958, "https://github.com/dotnet/roslyn/issues/41958")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)>
        Public Async Function TestWithDialogInheritedMembers() As Task
            Await TestWithPickMembersDialogAsync(
"
Class Base
    Public Property C As Integer
End Class

Class Middle
    Inherits Base

    Public Property B As Integer
End Class

Class Derived
    Inherits Middle

    Public Property A As Integer
    [||]
End Class",
"
Class Base
    Public Property C As Integer
End Class

Class Middle
    Inherits Base

    Public Property B As Integer
End Class

Class Derived
    Inherits Middle

    Public Property A As Integer

    Public Overrides Function Equals(obj As Object) As Boolean
        Dim derived = TryCast(obj, Derived)
        Return derived IsNot Nothing AndAlso
               C = derived.C AndAlso
               B = derived.B AndAlso
               A = derived.A
    End Function
End Class",
chosenSymbols:=Nothing)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)>
        Public Async Function TestGenerateOperators1() As Task
            Await TestWithPickMembersDialogAsync(
"
Imports System.Collections.Generic

Class Program
    Public s As String
    [||]
End Class",
"
Imports System.Collections.Generic

Class Program
    Public s As String

    Public Overrides Function Equals(obj As Object) As Boolean
        Dim program = TryCast(obj, Program)
        Return program IsNot Nothing AndAlso
               s = program.s
    End Function

    Public Shared Operator =(left As Program, right As Program) As Boolean
        Return EqualityComparer(Of Program).Default.Equals(left, right)
    End Operator

    Public Shared Operator <>(left As Program, right As Program) As Boolean
        Return Not left = right
    End Operator
End Class",
chosenSymbols:=Nothing,
optionsCallback:=Sub(options) EnableOption(options, GenerateOperatorsId))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)>
        Public Async Function TestGenerateOperators3() As Task
            Await TestWithPickMembersDialogAsync(
"
Imports System.Collections.Generic

Class Program
    Public s As String
    [||]

    Public Shared Operator =(left As Program, right As Program) As Boolean
        Return True
    End Operator
End Class",
"
Imports System.Collections.Generic

Class Program
    Public s As String

    Public Overrides Function Equals(obj As Object) As Boolean
        Dim program = TryCast(obj, Program)
        Return program IsNot Nothing AndAlso
               s = program.s
    End Function

    Public Shared Operator =(left As Program, right As Program) As Boolean
        Return True
    End Operator
End Class",
chosenSymbols:=Nothing,
optionsCallback:=Sub(Options) Assert.Null(Options.FirstOrDefault(Function(o) o.Id = GenerateOperatorsId)))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)>
        Public Async Function TestGenerateOperators4() As Task
            Await TestWithPickMembersDialogAsync(
"
Imports System.Collections.Generic

Structure Program
    Public s As String
    [||]
End Structure",
"
Imports System.Collections.Generic

Structure Program
    Public s As String

    Public Overrides Function Equals(obj As Object) As Boolean
        If Not (TypeOf obj Is Program) Then
            Return False
        End If

        Dim program = DirectCast(obj, Program)
        Return s = program.s
    End Function

    Public Shared Operator =(left As Program, right As Program) As Boolean
        Return left.Equals(right)
    End Operator

    Public Shared Operator <>(left As Program, right As Program) As Boolean
        Return Not left = right
    End Operator
End Structure",
chosenSymbols:=Nothing,
optionsCallback:=Sub(options) EnableOption(options, GenerateOperatorsId))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)>
        Public Async Function TestImplementIEquatable1() As Task
            Await TestWithPickMembersDialogAsync(
"
Imports System.Collections.Generic

structure Program
    Public s As String
    [||]
End structure",
"
Imports System
Imports System.Collections.Generic

structure Program
    Implements IEquatable(Of Program)

    Public s As String

    Public Overrides Function Equals(obj As Object) As Boolean
        Return (TypeOf obj Is Program) AndAlso Equals(DirectCast(obj, Program))
    End Function

    Public Function Equals(other As Program) As Boolean Implements IEquatable(Of Program).Equals
        Return s = other.s
    End Function
End structure",
chosenSymbols:=Nothing,
optionsCallback:=Sub(Options) EnableOption(Options, ImplementIEquatableId))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)>
        Public Async Function TestImplementIEquatable2() As Task
            Await TestWithPickMembersDialogAsync(
"
Imports System.Collections.Generic

Class Program
    Public s As String
    [||]
End Class",
"
Imports System
Imports System.Collections.Generic

Class Program
    Implements IEquatable(Of Program)

    Public s As String

    Public Overrides Function Equals(obj As Object) As Boolean
        Return Equals(TryCast(obj, Program))
    End Function

    Public Function Equals(other As Program) As Boolean Implements IEquatable(Of Program).Equals
        Return other IsNot Nothing AndAlso
               s = other.s
    End Function
End Class",
chosenSymbols:=Nothing,
optionsCallback:=Sub(Options) EnableOption(Options, ImplementIEquatableId))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)>
        Public Async Function TestGetHashCodeWithOverflowChecking() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Class Z
    [|Private a As Integer
    Private b As Integer|]
End Class",
"Option Strict On
Class Z
    Private a As Integer
    Private b As Integer

    Public Overrides Function Equals(obj As Object) As Boolean
        Dim z = TryCast(obj, Z)
        Return z IsNot Nothing AndAlso
               a = z.a AndAlso
               b = z.b
    End Function

    Public Overrides Function GetHashCode() As Integer
        Return (a, b).GetHashCode()
    End Function
End Class",
index:=1, compilationOptions:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, checkOverflow:=True))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)>
        Public Async Function TestGetHashCodeWithoutOverflowChecking() As Task
            Await TestInRegularAndScriptAsync(
"Class Z
    [|Private a As Integer|]
End Class",
"Class Z
    Private a As Integer

    Public Overrides Function Equals(obj As Object) As Boolean
        Dim z = TryCast(obj, Z)
        Return z IsNot Nothing AndAlso
               a = z.a
    End Function

    Public Overrides Function GetHashCode() As Integer
        Return -1757793268 + a.GetHashCode()
    End Function
End Class",
index:=1, compilationOptions:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, checkOverflow:=False))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)>
        Public Async Function TestMultipleValuesWithoutValueTuple() As Task

            Await TestInRegularAndScriptAsync("
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferencesWithoutValueTuple=""true"">
        <Document>
Class Z
    [|Private a As Integer
    Private b As Integer|]
End Class
        </Document>
    </Project>
</Workspace>", "
Class Z
    Private a As Integer
    Private b As Integer

    Public Overrides Function Equals(obj As Object) As Boolean
        Dim z = TryCast(obj, Z)
        Return z IsNot Nothing AndAlso
               a = z.a AndAlso
               b = z.b
    End Function

    Public Overrides Function GetHashCode() As Integer
        Dim hashCode = 2118541809
        hashCode = hashCode * -1521134295 + a.GetHashCode()
        hashCode = hashCode * -1521134295 + b.GetHashCode()
        Return hashCode
    End Function
End Class
        ",
index:=1)

        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)>
        Public Async Function TestMultipleValuesWithValueTupleOneValue() As Task
            Await TestInRegularAndScriptAsync(
"
Namespace System
    Public Structure ValueTuple
    End Structure
End Namespace
Class Z
    [|Private a As Integer|]
    Private b As Integer
End Class",
"
Namespace System
    Public Structure ValueTuple
    End Structure
End Namespace
Class Z
    Private a As Integer
    Private b As Integer

    Public Overrides Function Equals(obj As Object) As Boolean
        Dim z = TryCast(obj, Z)
        Return z IsNot Nothing AndAlso
               a = z.a
    End Function

    Public Overrides Function GetHashCode() As Integer
        Dim hashCode As Long = -468965076
        hashCode = (hashCode * -1521134295 + a.GetHashCode()).GetHashCode()
        Return hashCode
    End Function
End Class",
index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)>
        Public Async Function TestMultipleValuesWithValueTupleTwoValues() As Task
            Await TestInRegularAndScriptAsync(
"
Namespace System
    Public Structure ValueTuple
    End Structure
End Namespace
Class Z
    [|Private a As Integer
    Private b As Integer|]
End Class",
"
Namespace System
    Public Structure ValueTuple
    End Structure
End Namespace
Class Z
    Private a As Integer
    Private b As Integer

    Public Overrides Function Equals(obj As Object) As Boolean
        Dim z = TryCast(obj, Z)
        Return z IsNot Nothing AndAlso
               a = z.a AndAlso
               b = z.b
    End Function

    Public Overrides Function GetHashCode() As Integer
        Return (a, b).GetHashCode()
    End Function
End Class",
index:=1)
        End Function

        <WorkItem(33601, "https://github.com/dotnet/roslyn/issues/33601")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)>
        Public Async Function TestPartialSelection() As Task
            Await TestMissingAsync(
"Class Z
    Private [|a|] As Integer
End Class")
        End Function
    End Class
End Namespace
