' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.GenerateComparisonOperators

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.GenerateComparisonOperators
    <Trait(Traits.Feature, Traits.Features.CodeActionsGenerateComparisonOperators)>
    Public Class GenerateComparisonOperatorsTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New GenerateComparisonOperatorsCodeRefactoringProvider()
        End Function

        Protected Overrides Function MassageActions(actions As ImmutableArray(Of CodeAction)) As ImmutableArray(Of CodeAction)
            Return FlattenActions(actions)
        End Function

        <Fact>
        Public Async Function TestClass() As Task
            Await TestInRegularAndScript1Async(
"
imports System

[||]class C
    implements IComparable(Of C)

    public function CompareTo(c as C) as integer implements IComparable(of C).CompareTo
        Return 0
    end function
end class",
"
imports System

class C
    implements IComparable(Of C)

    public function CompareTo(c as C) as integer implements IComparable(of C).CompareTo
        Return 0
    end function

    Public Shared Operator >(left As C, right As C) As Boolean
        Return left.CompareTo(right) > 0
    End Operator

    Public Shared Operator <(left As C, right As C) As Boolean
        Return left.CompareTo(right) < 0
    End Operator

    Public Shared Operator >=(left As C, right As C) As Boolean
        Return left.CompareTo(right) >= 0
    End Operator

    Public Shared Operator <=(left As C, right As C) As Boolean
        Return left.CompareTo(right) <= 0
    End Operator
end class")
        End Function

        <Fact>
        Public Async Function TestExplicitImpl() As Task
            Await TestInRegularAndScript1Async(
"
imports System

[||]class C
    implements IComparable(Of C)

    private function CompareToImpl(c as C) as integer implements IComparable(of C).CompareTo
        Return 0
    end function
end class",
"
imports System

class C
    implements IComparable(Of C)

    private function CompareToImpl(c as C) as integer implements IComparable(of C).CompareTo
        Return 0
    end function

    Public Shared Operator >(left As C, right As C) As Boolean
        Return DirectCast(left, IComparable(Of C)).CompareTo(right) > 0
    End Operator

    Public Shared Operator <(left As C, right As C) As Boolean
        Return DirectCast(left, IComparable(Of C)).CompareTo(right) < 0
    End Operator

    Public Shared Operator >=(left As C, right As C) As Boolean
        Return DirectCast(left, IComparable(Of C)).CompareTo(right) >= 0
    End Operator

    Public Shared Operator <=(left As C, right As C) As Boolean
        Return DirectCast(left, IComparable(Of C)).CompareTo(right) <= 0
    End Operator
end class")
        End Function

        <Fact>
        Public Async Function TestOnInterface() As Task
            Await TestInRegularAndScript1Async(
"
imports System

class C
    implements [||]IComparable(Of C)

    public function CompareTo(c as C) as integer implements IComparable(of C).CompareTo
        Return 0
    end function
end class",
"
imports System

class C
    implements IComparable(Of C)

    public function CompareTo(c as C) as integer implements IComparable(of C).CompareTo
        Return 0
    end function

    Public Shared Operator >(left As C, right As C) As Boolean
        Return left.CompareTo(right) > 0
    End Operator

    Public Shared Operator <(left As C, right As C) As Boolean
        Return left.CompareTo(right) < 0
    End Operator

    Public Shared Operator >=(left As C, right As C) As Boolean
        Return left.CompareTo(right) >= 0
    End Operator

    Public Shared Operator <=(left As C, right As C) As Boolean
        Return left.CompareTo(right) <= 0
    End Operator
end class")
        End Function

        <Fact>
        Public Async Function TestAtEndOfInterface() As Task
            Await TestInRegularAndScript1Async(
"
imports System

class C
    implements IComparable(Of C)[||]

    public function CompareTo(c as C) as integer implements IComparable(of C).CompareTo
        Return 0
    end function
end class",
"
imports System

class C
    implements IComparable(Of C)

    public function CompareTo(c as C) as integer implements IComparable(of C).CompareTo
        Return 0
    end function

    Public Shared Operator >(left As C, right As C) As Boolean
        Return left.CompareTo(right) > 0
    End Operator

    Public Shared Operator <(left As C, right As C) As Boolean
        Return left.CompareTo(right) < 0
    End Operator

    Public Shared Operator >=(left As C, right As C) As Boolean
        Return left.CompareTo(right) >= 0
    End Operator

    Public Shared Operator <=(left As C, right As C) As Boolean
        Return left.CompareTo(right) <= 0
    End Operator
end class")
        End Function

        <Fact>
        Public Async Function TestInBody() As Task
            Await TestInRegularAndScript1Async(
"
imports System

class C
    implements IComparable(Of C)

    public function CompareTo(c as C) as integer implements IComparable(of C).CompareTo
        Return 0
    end function

[||]
end class",
"
imports System

class C
    implements IComparable(Of C)

    public function CompareTo(c as C) as integer implements IComparable(of C).CompareTo
        Return 0
    end function

    Public Shared Operator >(left As C, right As C) As Boolean
        Return left.CompareTo(right) > 0
    End Operator

    Public Shared Operator <(left As C, right As C) As Boolean
        Return left.CompareTo(right) < 0
    End Operator

    Public Shared Operator >=(left As C, right As C) As Boolean
        Return left.CompareTo(right) >= 0
    End Operator

    Public Shared Operator <=(left As C, right As C) As Boolean
        Return left.CompareTo(right) <= 0
    End Operator
end class")
        End Function

        <Fact>
        Public Async Function TestMissingWithoutCompareMethod() As Task
            Await TestMissingAsync(
"
imports System

class C
    implements IComparable(Of C)

[||]
end class")
        End Function

        <Fact>
        Public Async Function TestMissingWithUnknownType() As Task
            Await TestMissingAsync(
"
imports System

class C : IComparable<Goo>
    public int CompareTo(Goo g) => 0

[||]
end class")
        End Function

        <Fact>
        Public Async Function TestMissingWithAllExistingOperators() As Task
            Await TestMissingAsync(
"
imports System

class C
    implements IComparable(Of C)

    public function CompareTo(c as C) as integer implements IComparable(of C).CompareTo
        Return 0
    end function

    Public Shared Operator >(left As C, right As C) As Boolean
        Return left.CompareTo(right) > 0
    End Operator

    Public Shared Operator <(left As C, right As C) As Boolean
        Return left.CompareTo(right) < 0
    End Operator

    Public Shared Operator >=(left As C, right As C) As Boolean
        Return left.CompareTo(right) >= 0
    End Operator

    Public Shared Operator <=(left As C, right As C) As Boolean
        Return left.CompareTo(right) <= 0
    End Operator

[||]
end class")
        End Function

        <Fact>
        Public Async Function TestWithExistingOperator() As Task
            Await TestInRegularAndScript1Async(
"
imports System

class C
    implements IComparable(Of C)

    public function CompareTo(c as C) as integer implements IComparable(of C).CompareTo
        Return 0
    end function

    Public Shared Operator <(left As C, right As C) As Boolean
        Return left.CompareTo(right) < 0
    End Operator

[||]
end class",
"
imports System

class C
    implements IComparable(Of C)

    public function CompareTo(c as C) as integer implements IComparable(of C).CompareTo
        Return 0
    end function

    Public Shared Operator >(left As C, right As C) As Boolean
        Return left.CompareTo(right) > 0
    End Operator

    Public Shared Operator <(left As C, right As C) As Boolean
        Return left.CompareTo(right) < 0
    End Operator

    Public Shared Operator >=(left As C, right As C) As Boolean
        Return left.CompareTo(right) >= 0
    End Operator

    Public Shared Operator <=(left As C, right As C) As Boolean
        Return left.CompareTo(right) <= 0
    End Operator
end class")
        End Function

        <Fact>
        Public Async Function TestMultipleInterfaces() As Task
            Await TestInRegularAndScript1Async(
"
imports System

class C
    implements IComparable(Of C), IComparable(of integer)

    public function CompareTo(c as C) as integer implements IComparable(of C).CompareTo
        Return 0
    end function

    public function CompareTo(c as integer) as integer implements IComparable(of integer).CompareTo
        Return 0
    end function

[||]
end class",
"
imports System

class C
    implements IComparable(Of C), IComparable(of integer)

    public function CompareTo(c as C) as integer implements IComparable(of C).CompareTo
        Return 0
    end function

    public function CompareTo(c as integer) as integer implements IComparable(of integer).CompareTo
        Return 0
    end function

    Public Shared Operator >(left As C, right As Integer) As Boolean
        Return left.CompareTo(right) > 0
    End Operator

    Public Shared Operator <(left As C, right As Integer) As Boolean
        Return left.CompareTo(right) < 0
    End Operator

    Public Shared Operator >=(left As C, right As Integer) As Boolean
        Return left.CompareTo(right) >= 0
    End Operator

    Public Shared Operator <=(left As C, right As Integer) As Boolean
        Return left.CompareTo(right) <= 0
    End Operator
end class", index:=1)
        End Function
    End Class
End Namespace
