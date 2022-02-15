' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.Wrapping

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Wrapping
    Public Class InitializerExpressionWrappingTests
        Inherits AbstractWrappingTests

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicWrappingCodeRefactoringProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestNoWrappingSuggestions() As Task
            Await TestMissingAsync(
"Class C
    Public Sub Bar()
        Dim test() As Integer = New Integer() [||]{1}
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestWrappingShortInitializerExpression() As Task
            Await TestAllWrappingCasesAsync(
"Class C
    Public Sub Bar()
        Dim test() As Integer = New Integer() [||]{1, 2}
    End Sub
End Class",
"Class C
    Public Sub Bar()
        Dim test() As Integer = New Integer() {
            1,
            2
        }
    End Sub
End Class", "Class C
    Public Sub Bar()
        Dim test() As Integer = New Integer() {
            1, 2
        }
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestWrappingLongIntializerExpression() As Task
            Await TestAllWrappingCasesAsync("Class C
    Public Sub Bar()
        Dim test() As String = New String() [||]{""the"", ""quick"", ""brown"", ""fox"", ""jumps"", ""over"", ""the"", ""lazy"", ""dog""}
    End Sub
}", "Class C
    Public Sub Bar()
        Dim test() As String = New String() {
            ""the"",
            ""quick"",
            ""brown"",
            ""fox"",
            ""jumps"",
            ""over"",
            ""the"",
            ""lazy"",
            ""dog""
        }
    End Sub
}", "Class C
    Public Sub Bar()
        Dim test() As String = New String() {
            ""the"", ""quick"", ""brown"", ""fox"", ""jumps"", ""over"", ""the"", ""lazy"", ""dog""
        }
    End Sub
}")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestWrappingMultiLineLongIntializerExpression() As Task
            Await TestAllWrappingCasesAsync("Class C
    Public Sub Bar()
        Dim test() As String = New String() [||]{""the"", ""quick"", ""brown"", ""fox"", ""jumps"", ""over"", ""the"", ""lazy"", ""dog"", ""the"", ""quick"", ""brown"", ""fox"", ""jumps"", ""over"", ""the"", ""lazy"", ""dog""}
    End Sub
}", "Class C
    Public Sub Bar()
        Dim test() As String = New String() {
            ""the"",
            ""quick"",
            ""brown"",
            ""fox"",
            ""jumps"",
            ""over"",
            ""the"",
            ""lazy"",
            ""dog"",
            ""the"",
            ""quick"",
            ""brown"",
            ""fox"",
            ""jumps"",
            ""over"",
            ""the"",
            ""lazy"",
            ""dog""
        }
    End Sub
}", "Class C
    Public Sub Bar()
        Dim test() As String = New String() {
            ""the"", ""quick"", ""brown"", ""fox"", ""jumps"", ""over"", ""the"", ""lazy"", ""dog"", ""the"", ""quick"", ""brown"", ""fox"",
            ""jumps"", ""over"", ""the"", ""lazy"", ""dog""
        }
    End Sub
}")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestShortInitializerExpressionRefactorings() As Task
            Await TestAllWrappingCasesAsync("Class C
    Public Sub Bar()
        Dim test() As Integer = New Integer() [||]{
            1,
            2
        }
    End Sub
End Class", "Class C
    Public Sub Bar()
        Dim test() As Integer = New Integer() {1, 2}
    End Sub
End Class", "Class C
    Public Sub Bar()
        Dim test() As Integer = New Integer() {
            1, 2
        }
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestLongIntializerExpressionRefactorings() As Task
            Await TestAllWrappingCasesAsync("Class C
    Public Sub Bar()
        Dim test() As String = New String() [||]{
            ""the"", ""quick"", ""brown"", ""fox"", ""jumps"", ""over"", ""the"", ""lazy"", ""dog""
        }
     End Sub
End Class", "Class C
    Public Sub Bar()
        Dim test() As String = New String() {
            ""the"",
            ""quick"",
            ""brown"",
            ""fox"",
            ""jumps"",
            ""over"",
            ""the"",
            ""lazy"",
            ""dog""
        }
     End Sub
End Class", "Class C
    Public Sub Bar()
        Dim test() As String = New String() {""the"", ""quick"", ""brown"", ""fox"", ""jumps"", ""over"", ""the"", ""lazy"", ""dog""}
     End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestListIntializerExpressionRefactorings() As Task
            Await TestAllWrappingCasesAsync("Class C
    Public Sub Bar()
        Dim test As New List(Of Integer) From [||]{0, 1, 2, 3, 4, 5, 6, 7, 8, 9}
     End Sub
End Class", "Class C
    Public Sub Bar()
        Dim test As New List(Of Integer) From {
            0,
            1,
            2,
            3,
            4,
            5,
            6,
            7,
            8,
            9
        }
     End Sub
End Class", "Class C
    Public Sub Bar()
        Dim test As New List(Of Integer) From {
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9
        }
     End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestWrappedListIntializerExpressionRefactorings() As Task
            Await TestAllWrappingCasesAsync("Class C
    Public Sub Bar()
        Dim test As New List(Of Integer) From [||]{
            0,
            1,
            2,
            3,
            4,
            5,
            6,
            7,
            8,
            9
        }
     End Sub
End Class", "Class C
    Public Sub Bar()
        Dim test As New List(Of Integer) From {0, 1, 2, 3, 4, 5, 6, 7, 8, 9}
     End Sub
End Class", "Class C
    Public Sub Bar()
        Dim test As New List(Of Integer) From {
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9
        }
     End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestObjectIntializerExpressionRefactorings() As Task
            Await TestAllWrappingCasesAsync("Class C
    Public Sub Bar()
        Dim test As New List(Of A) From [||]{New A() With {.B = 0, .C = 1}, New A() With {.B = 0, .C = 1}, New A() With {.B = 0, .C = 1}}
     End Sub
End Class", "Class C
    Public Sub Bar()
        Dim test As New List(Of A) From {
            New A() With {.B = 0, .C = 1},
            New A() With {.B = 0, .C = 1},
            New A() With {.B = 0, .C = 1}
        }
     End Sub
End Class", "Class C
    Public Sub Bar()
        Dim test As New List(Of A) From {
            New A() With {.B = 0, .C = 1}, New A() With {.B = 0, .C = 1}, New A() With {.B = 0, .C = 1}
        }
     End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestWrappedObjectIntializerExpressionRefactorings() As Task
            Await TestAllWrappingCasesAsync("Class C
    Public Sub Bar()
        Dim test As New List(Of A) From [||]{
            New A() With {.B = 0, .C = 1},
            New A() With {.B = 0, .C = 1},
            New A() With {.B = 0, .C = 1}
        }
     End Sub
End Class", "Class C
    Public Sub Bar()
        Dim test As New List(Of A) From {New A() With {.B = 0, .C = 1}, New A() With {.B = 0, .C = 1}, New A() With {.B = 0, .C = 1}}
     End Sub
End Class", "Class C
    Public Sub Bar()
        Dim test As New List(Of A) From {
            New A() With {.B = 0, .C = 1}, New A() With {.B = 0, .C = 1}, New A() With {.B = 0, .C = 1}
        }
     End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestReturnIntializerExpressionRefactorings() As Task
            Await TestAllWrappingCasesAsync("Class C
    Public Sub Bar()
        Return New List(Of Integer) From [||]{0, 1, 2, 3, 4, 5, 6, 7, 8, 9}
     End Sub
End Class", "Class C
    Public Sub Bar()
        Return New List(Of Integer) From {
            0,
            1,
            2,
            3,
            4,
            5,
            6,
            7,
            8,
            9
        }
     End Sub
End Class", "Class C
    Public Sub Bar()
        Return New List(Of Integer) From {
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9
        }
     End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestWrappedReturnIntializerExpressionRefactorings() As Task
            Await TestAllWrappingCasesAsync("Class C
    Public Sub Bar()
        Return New List(Of Integer) From [||]{
            0,
            1,
            2,
            3,
            4,
            5,
            6,
            7,
            8,
            9
        }
     End Sub
End Class", "Class C
    Public Sub Bar()
        Return New List(Of Integer) From {0, 1, 2, 3, 4, 5, 6, 7, 8, 9}
     End Sub
End Class", "Class C
    Public Sub Bar()
        Return New List(Of Integer) From {
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9
        }
     End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestClassPropertyIntializerExpressionRefactorings() As Task
            Await TestAllWrappingCasesAsync("Public Class C
    Public Property B As New List(Of Integer) From [||]{0, 1, 2, 3, 4, 5, 6, 7, 8, 9}
End Class", "Public Class C
    Public Property B As New List(Of Integer) From {
        0,
        1,
        2,
        3,
        4,
        5,
        6,
        7,
        8,
        9
    }
End Class", "Public Class C
    Public Property B As New List(Of Integer) From {
        0, 1, 2, 3, 4, 5, 6, 7, 8, 9
    }
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestWrappedClassPropertyIntializerExpressionRefactorings() As Task
            Await TestAllWrappingCasesAsync("Public Class C
    Public Property B As New List(Of Integer) From [||]{
        0,
        1,
        2,
        3,
        4,
        5,
        6,
        7,
        8,
        9
    }
End Class", "Public Class C
    Public Property B As New List(Of Integer) From {0, 1, 2, 3, 4, 5, 6, 7, 8, 9}
End Class", "Public Class C
    Public Property B As New List(Of Integer) From {
        0, 1, 2, 3, 4, 5, 6, 7, 8, 9
    }
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestArgumentIntializerExpressionRefactorings() As Task
            Await TestAllWrappingCasesAsync("Public Sub F
    Dim result = FakeFunction(New List(Of Integer) From [||]{0, 1, 2, 3, 4, 5, 6, 7, 8, 9})
End Sub", "Public Sub F
    Dim result = FakeFunction(New List(Of Integer) From {
        0,
        1,
        2,
        3,
        4,
        5,
        6,
        7,
        8,
        9
    })
End Sub", "Public Sub F
    Dim result = FakeFunction(New List(Of Integer) From {
        0, 1, 2, 3, 4, 5, 6, 7, 8, 9
    })
End Sub")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestWrappedArgumentIntializerExpressionRefactorings() As Task
            Await TestAllWrappingCasesAsync("Public Sub F
    Dim result = FakeFunction(New List(Of Integer) From [||]{
        0,
        1,
        2,
        3,
        4,
        5,
        6,
        7,
        8,
        9
    })
End Sub", "Public Sub F
    Dim result = FakeFunction(New List(Of Integer) From {0, 1, 2, 3, 4, 5, 6, 7, 8, 9})
End Sub", "Public Sub F
    Dim result = FakeFunction(New List(Of Integer) From {
        0, 1, 2, 3, 4, 5, 6, 7, 8, 9
    })
End Sub")
        End Function
    End Class
End Namespace
