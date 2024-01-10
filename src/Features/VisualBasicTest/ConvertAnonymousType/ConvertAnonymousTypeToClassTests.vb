' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.ConvertAnonymousType

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ConvertAnonymousType
    <Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToClass)>
    Public Class ConvertAnonymousTypeToClassTests
        Inherits AbstractVisualBasicCodeActionTest_NoEditor

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As TestWorkspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicConvertAnonymousTypeToClassCodeRefactoringProvider()
        End Function

        <Fact>
        Public Async Function ConvertSingleAnonymousType() As Task
            Dim text = "
class Test
    sub Method()
        dim t1 = [||]new with { key .a = 1, key .b = 2 }
    end sub
end class
"
            Dim expected = "
class Test
    sub Method()
        dim t1 = New {|Rename:NewClass|}(1, 2)
    end sub
end class

Friend Class NewClass
    Public ReadOnly Property A As Integer
    Public ReadOnly Property B As Integer

    Public Sub New(a As Integer, b As Integer)
        Me.A = a
        Me.B = b
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        Dim other = TryCast(obj, NewClass)
        Return other IsNot Nothing AndAlso
               A = other.A AndAlso
               B = other.B
    End Function

    Public Overrides Function GetHashCode() As Integer
        Return (A, B).GetHashCode()
    End Function
End Class
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact>
        Public Async Function OnEmptyAnonymousType() As Task
            Await TestInRegularAndScriptAsync("
class Test
    sub Method()
        dim t1 = [||]new with { }
    end sub
end class
",
"
class Test
    sub Method()
        dim t1 = New {|Rename:NewClass|}()
    end sub
end class

Friend Class NewClass
    Public Sub New()
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        Dim other = TryCast(obj, NewClass)
        Return other IsNot Nothing
    End Function

    Public Overrides Function GetHashCode() As Integer
        Return 0
    End Function
End Class
")
        End Function

        <Fact>
        Public Async Function OnSingleFieldAnonymousType() As Task
            Await TestInRegularAndScriptAsync("
class Test
    sub Method()
        dim t1 = [||]new with { key .a = 1 }
    end sub
end class
",
"
class Test
    sub Method()
        dim t1 = New {|Rename:NewClass|}(1)
    end sub
end class

Friend Class NewClass
    Public ReadOnly Property A As Integer

    Public Sub New(a As Integer)
        Me.A = a
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        Dim other = TryCast(obj, NewClass)
        Return other IsNot Nothing AndAlso
               A = other.A
    End Function

    Public Overrides Function GetHashCode() As Integer
        Dim hashCode As Long = -1005848884
        hashCode = (hashCode * -1521134295 + A.GetHashCode()).GetHashCode()
        Return hashCode
    End Function
End Class
")
        End Function

        <Fact>
        Public Async Function ConvertSingleAnonymousTypeWithInferredName() As Task
            Dim text = "
class Test
    sub Method(b as integer)
        dim t1 = [||]new with { key .a = 1, key b }
    end sub
end class
"
            Dim expected = "
class Test
    sub Method(b as integer)
        dim t1 = New {|Rename:NewClass|}(1, b)
    end sub
end class

Friend Class NewClass
    Public ReadOnly Property A As Integer
    Public ReadOnly Property B As Integer

    Public Sub New(a As Integer, b As Integer)
        Me.A = a
        Me.B = b
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        Dim other = TryCast(obj, NewClass)
        Return other IsNot Nothing AndAlso
               A = other.A AndAlso
               B = other.B
    End Function

    Public Overrides Function GetHashCode() As Integer
        Return (A, B).GetHashCode()
    End Function
End Class
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact>
        Public Async Function ConvertMultipleInstancesInSameMethod() As Task
            Dim text = "
class Test
    sub Method()
        dim t1 = [||]new with { key .a = 1, key .b = 2 }
        dim t2 = new with { key .a = 3, key .b = 4 }
    end sub
end class
"
            Dim expected = "
class Test
    sub Method()
        dim t1 = New {|Rename:NewClass|}(1, 2)
        dim t2 = New NewClass(3, 4)
    end sub
end class

Friend Class NewClass
    Public ReadOnly Property A As Integer
    Public ReadOnly Property B As Integer

    Public Sub New(a As Integer, b As Integer)
        Me.A = a
        Me.B = b
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        Dim other = TryCast(obj, NewClass)
        Return other IsNot Nothing AndAlso
               A = other.A AndAlso
               B = other.B
    End Function

    Public Overrides Function GetHashCode() As Integer
        Return (A, B).GetHashCode()
    End Function
End Class
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact>
        Public Async Function ConvertMultipleInstancesAcrossMethods() As Task
            Dim text = "
class Test
    sub Method()
        dim t1 = [||]new with { key .a = 1, key .b = 2 }
        dim t2 = new with { key .a = 3, key .b = 4 }
    end sub

    sub Method2()
        dim t1 = new with { key .a = 1, key .b = 2 }
        dim t2 = new with { key .a = 3, key .b = 4 }
    end sub
end class
"
            Dim expected = "
class Test
    sub Method()
        dim t1 = New {|Rename:NewClass|}(1, 2)
        dim t2 = New NewClass(3, 4)
    end sub

    sub Method2()
        dim t1 = new with { key .a = 1, key .b = 2 }
        dim t2 = new with { key .a = 3, key .b = 4 }
    end sub
end class

Friend Class NewClass
    Public ReadOnly Property A As Integer
    Public ReadOnly Property B As Integer

    Public Sub New(a As Integer, b As Integer)
        Me.A = a
        Me.B = b
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        Dim other = TryCast(obj, NewClass)
        Return other IsNot Nothing AndAlso
               A = other.A AndAlso
               B = other.B
    End Function

    Public Overrides Function GetHashCode() As Integer
        Return (A, B).GetHashCode()
    End Function
End Class
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact>
        Public Async Function OnlyConvertMatchingTypesInSameMethod() As Task
            Dim text = "
class Test
    sub Method(b as integer)
        dim t1 = [||]new with { key .a = 1, key .b = 2 }
        dim t2 = new with { key .a = 3, key b }
        dim t3 = new with { key .a = 4 }
        dim t4 = new with { key .b = 5, key .a = 6 }
    end sub
end class
"
            Dim expected = "
class Test
    sub Method(b as integer)
        dim t1 = New {|Rename:NewClass|}(1, 2)
        dim t2 = New NewClass(3, b)
        dim t3 = new with { key .a = 4 }
        dim t4 = new with { key .b = 5, key .a = 6 }
    end sub
end class

Friend Class NewClass
    Public ReadOnly Property A As Integer
    Public ReadOnly Property B As Integer

    Public Sub New(a As Integer, b As Integer)
        Me.A = a
        Me.B = b
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        Dim other = TryCast(obj, NewClass)
        Return other IsNot Nothing AndAlso
               A = other.A AndAlso
               B = other.B
    End Function

    Public Overrides Function GetHashCode() As Integer
        Return (A, B).GetHashCode()
    End Function
End Class
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact>
        Public Async Function TestFixAllMatchesInSingleMethod() As Task
            Dim text = "
class Test
    sub Method(b as integer)
        dim t1 = [||]new with { key .a = 1, key .b = 2 }
        dim t2 = new with { key .a = 3, key b }
        dim t3 = new with { key .a = 4 }
        dim t4 = new with { key .b = 5, key .a = 6 }
    end sub
end class
"
            Dim expected = "
class Test
    sub Method(b as integer)
        dim t1 = New {|Rename:NewClass|}(1, 2)
        dim t2 = New NewClass(3, b)
        dim t3 = new with { key .a = 4 }
        dim t4 = new with { key .b = 5, key .a = 6 }
    end sub
end class

Friend Class NewClass
    Public ReadOnly Property A As Integer
    Public ReadOnly Property B As Integer

    Public Sub New(a As Integer, b As Integer)
        Me.A = a
        Me.B = b
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        Dim other = TryCast(obj, NewClass)
        Return other IsNot Nothing AndAlso
               A = other.A AndAlso
               B = other.B
    End Function

    Public Overrides Function GetHashCode() As Integer
        Return (A, B).GetHashCode()
    End Function
End Class
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact>
        Public Async Function TestFixNotAcrossMethods() As Task
            Dim text = "
class Test
    sub Method()
        dim t1 = [||]new with { key .a = 1, key .b = 2 }
        dim t2 = new with { key .a = 3, key .b = 4 }
    end sub

    sub Method2()
        dim t1 = new with { key .a = 1, key .b = 2 }
        dim t2 = new with { key .a = 3, key .b = 4 }
    end sub
end class
"
            Dim expected = "
class Test
    sub Method()
        dim t1 = New {|Rename:NewClass|}(1, 2)
        dim t2 = New NewClass(3, 4)
    end sub

    sub Method2()
        dim t1 = new with { key .a = 1, key .b = 2 }
        dim t2 = new with { key .a = 3, key .b = 4 }
    end sub
end class

Friend Class NewClass
    Public ReadOnly Property A As Integer
    Public ReadOnly Property B As Integer

    Public Sub New(a As Integer, b As Integer)
        Me.A = a
        Me.B = b
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        Dim other = TryCast(obj, NewClass)
        Return other IsNot Nothing AndAlso
               A = other.A AndAlso
               B = other.B
    End Function

    Public Overrides Function GetHashCode() As Integer
        Return (A, B).GetHashCode()
    End Function
End Class
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact>
        Public Async Function NotIfReferencesAnonymousTypeInternally() As Task
            Dim text = "
class Test
    sub Method()
        dim t1 = [||]new with { key .a = 1, key .b = new with { key .c = 1, key .d = 2 } }
    end sub
end class
"

            Await TestMissingInRegularAndScriptAsync(text)
        End Function

        <Fact>
        Public Async Function ConvertMultipleNestedInstancesInSameMethod() As Task
            Dim text = "
class Test
    sub Method()
        dim t1 = [||]new with { key .a = 1, key .b = directcast(new with { key .a = 1, key .b = directcast(nothing, object) }, object) }
    end sub
end class
"
            Dim expected = "
Imports System.Collections.Generic

class Test
    sub Method()
        dim t1 = New {|Rename:NewClass|}(1, directcast(New NewClass(1, directcast(nothing, object)), object))
    end sub
end class

Friend Class NewClass
    Public ReadOnly Property A As Integer
    Public ReadOnly Property B As Object

    Public Sub New(a As Integer, b As Object)
        Me.A = a
        Me.B = b
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        Dim other = TryCast(obj, NewClass)
        Return other IsNot Nothing AndAlso
               A = other.A AndAlso
               EqualityComparer(Of Object).Default.Equals(B, other.B)
    End Function

    Public Overrides Function GetHashCode() As Integer
        Return (A, B).GetHashCode()
    End Function
End Class
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact>
        Public Async Function RenameAnnotationOnStartingPoint() As Task
            Dim text = "
class Test
    sub Method()
        dim t1 = new with { key .a = 1, key .b = 2 }
        dim t2 = [||]new with { key .a = 3, key .b = 4 }
    end sub
end class
"
            Dim expected = "
class Test
    sub Method()
        dim t1 = New NewClass(1, 2)
        dim t2 = New {|Rename:NewClass|}(3, 4)
    end sub
end class

Friend Class NewClass
    Public ReadOnly Property A As Integer
    Public ReadOnly Property B As Integer

    Public Sub New(a As Integer, b As Integer)
        Me.A = a
        Me.B = b
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        Dim other = TryCast(obj, NewClass)
        Return other IsNot Nothing AndAlso
               A = other.A AndAlso
               B = other.B
    End Function

    Public Overrides Function GetHashCode() As Integer
        Return (A, B).GetHashCode()
    End Function
End Class
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact>
        Public Async Function UpdateReferences() As Task
            Dim text = "
class Test
    sub Method()
        dim t1 = [||]new with { key .a = 1, key .b = 2 }
        Console.WriteLine(t1.a + t1?.b)
    end sub
end class
"
            Dim expected = "
class Test
    sub Method()
        dim t1 = New {|Rename:NewClass|}(1, 2)
        Console.WriteLine(t1.A + t1?.B)
    end sub
end class

Friend Class NewClass
    Public ReadOnly Property A As Integer
    Public ReadOnly Property B As Integer

    Public Sub New(a As Integer, b As Integer)
        Me.A = a
        Me.B = b
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        Dim other = TryCast(obj, NewClass)
        Return other IsNot Nothing AndAlso
               A = other.A AndAlso
               B = other.B
    End Function

    Public Overrides Function GetHashCode() As Integer
        Return (A, B).GetHashCode()
    End Function
End Class
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact>
        Public Async Function CapturedTypeParameters() As Task
            Dim text = "
imports System.Collections.Generic

class Test(of X as {structure})
    sub Method(of Y as {class, new})(lst as List(of X), arr as Y())
        dim t1 = [||]new with { key .a = lst, key .b = arr }
    end sub
end class
"
            Dim expected = "
imports System.Collections.Generic

class Test(of X as {structure})
    sub Method(of Y as {class, new})(lst as List(of X), arr as Y())
        dim t1 = New {|Rename:NewClass|}(Of X, Y)(lst, arr)
    end sub
end class

Friend Class NewClass(Of X As Structure, Y As {Class, New})
    Public ReadOnly Property A As List(Of X)
    Public ReadOnly Property B As Y()

    Public Sub New(a As List(Of X), b() As Y)
        Me.A = a
        Me.B = b
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        Dim other = TryCast(obj, NewClass(Of X, Y))
        Return other IsNot Nothing AndAlso
               EqualityComparer(Of List(Of X)).Default.Equals(A, other.A) AndAlso
               EqualityComparer(Of Y()).Default.Equals(B, other.B)
    End Function

    Public Overrides Function GetHashCode() As Integer
        Return (A, B).GetHashCode()
    End Function
End Class
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact>
        Public Async Function TestNonKeyProperties() As Task
            Dim text = "
class Test
    sub Method()
        dim t1 = [||]new with { key .a = 1, .b = 2 }
        dim t2 = new with { key .a = 3, .b = 4 }
        dim t3 = new with { key .a = 3, key .b = 4 }
        dim t4 = new with { .a = 3, key .b = 4 }
        dim t5 = new with { .a = 3, .b = 4 }
    end sub
end class
"
            Dim expected = "
class Test
    sub Method()
        dim t1 = New {|Rename:NewClass|}(1, 2)
        dim t2 = New NewClass(3, 4)
        dim t3 = new with { key .a = 3, key .b = 4 }
        dim t4 = new with { .a = 3, key .b = 4 }
        dim t5 = new with { .a = 3, .b = 4 }
    end sub
end class

Friend Class NewClass
    Public ReadOnly Property A As Integer
    Public Property B As Integer

    Public Sub New(a As Integer, b As Integer)
        Me.A = a
        Me.B = b
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        Dim other = TryCast(obj, NewClass)
        Return other IsNot Nothing AndAlso
               A = other.A
    End Function

    Public Overrides Function GetHashCode() As Integer
        Dim hashCode As Long = -1005848884
        hashCode = (hashCode * -1521134295 + A.GetHashCode()).GetHashCode()
        Return hashCode
    End Function
End Class
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact>
        Public Async Function TestNameCollision() As Task
            Dim text = "
class Test
    sub Method()
        dim t1 = [||]new with { key .a = 1, key .b = 2 }
    end sub
end class

class newclass
end class
"
            Dim expected = "
class Test
    sub Method()
        dim t1 = New {|Rename:NewClass1|}(1, 2)
    end sub
end class

class newclass
end class

Friend Class NewClass1
    Public ReadOnly Property A As Integer
    Public ReadOnly Property B As Integer

    Public Sub New(a As Integer, b As Integer)
        Me.A = a
        Me.B = b
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        Dim other = TryCast(obj, NewClass1)
        Return other IsNot Nothing AndAlso
               A = other.A AndAlso
               B = other.B
    End Function

    Public Overrides Function GetHashCode() As Integer
        Return (A, B).GetHashCode()
    End Function
End Class
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact>
        Public Async Function TestDuplicatedName() As Task
            Dim text = "
class Test
    sub Method()
        dim t1 = [||]new with { key .a = 1, key .a = 2 }
    end sub
end class
"
            Dim expected = "
class Test
    sub Method()
        dim t1 = New {|Rename:NewClass|}(1, 2)
    end sub
end class

Friend Class NewClass
    Public ReadOnly Property A As Integer
    Public ReadOnly Property A As Integer

    Public Sub New(a As Integer, a As Integer)
        Me.A = a
        Me.A = a
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        Dim other = TryCast(obj, NewClass)
        Return other IsNot Nothing AndAlso
               Me.A = other.A AndAlso
               Me.A = other.A
    End Function

    Public Overrides Function GetHashCode() As Integer
        Return (Me.A, Me.A).GetHashCode()
    End Function
End Class
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function
    End Class
End Namespace
