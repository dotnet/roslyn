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
    End Class
End Namespace
