' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.UseObjectInitializer

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.UseObjectInitializer
    Public Class UseObjectInitializerTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
            Return New Tuple(Of DiagnosticAnalyzer, CodeFixProvider)(
                New VisualBasicUseObjectInitializerDiagnosticAnalyzer(),
                New VisualBasicUseObjectInitializerCodeFixProvider())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)>
        Public Async Function TestOnVariableDeclarator() As Task
            Await TestAsync(
"
Class C
    Dim i As Integer
    Sub M()
        Dim c = [||]New C()
        c.i = 1
    End Sub
End Class",
"
Class C
    Dim i As Integer
    Sub M()
        Dim c = New C() With {
            .i = 1
        }
    End Sub
End Class",
compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)>
        Public Async Function TestOnVariableDeclarator2() As Task
            Await TestAsync(
"
Class C
    Dim i As Integer
    Sub M()
        Dim c As [||]New C()
        c.i = 1
    End Sub
End Class",
"
Class C
    Dim i As Integer
    Sub M()
        Dim c As New C() With {
            .i = 1
        }
    End Sub
End Class",
compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)>
        Public Async Function TestOnAssignmentExpression() As Task
            Await TestAsync(
"
Class C
    Dim i As Integer
    Sub M()
        Dim c as C = Nothing
        c = [||]New C()
        c.i = 1
    End Sub
End Class",
"
Class C
    Dim i As Integer
    Sub M()
        Dim c as C = Nothing
        c = New C() With {
            .i = 1
        }
    End Sub
End Class",
compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)>
        Public Async Function TestStopOnDuplicateMember() As Task
            Await TestAsync(
"
Class C
    Dim i As Integer
    Sub M()
        Dim c = [||]New C()
        c.i = 1
        c.i = 2
    End Sub
End Class",
"
Class C
    Dim i As Integer
    Sub M()
        Dim c = New C() With {
            .i = 1
        }
        c.i = 2
    End Sub
End Class",
compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)>
        Public Async Function TestComplexInitializer() As Task
            Await TestAsync(
"
Class C
    Dim i As Integer
    Dim j As Integer
    Sub M()
        Dim array As C()

        array(0) = [||]New C()
        array(0).i = 1
        array(0).j = 2
    End Sub
End Class",
"
Class C
    Dim i As Integer
    Dim j As Integer
    Sub M()
        Dim array As C()

        array(0) = New C() With {
            .i = 1,
            .j = 2
        }
    End Sub
End Class",
compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)>
        Public Async Function TestNotOnCompoundAssignment() As Task
            Await TestAsync(
"
Class C
    Dim i As Integer
    Dim j As Integer
    Sub M()
        Dim c = [||]New C()
        c.i = 1
        c.j += 1
    End Sub
End Class",
"
Class C
    Dim i As Integer
    Dim j As Integer
    Sub M()
        Dim c = New C() With {
            .i = 1
        }
        c.j += 1
    End Sub
End Class",
compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)>
        Public Async Function TestMissingWithExistingInitializer() As Task
            Await TestMissingAsync(
"
Class C
    Dim i As Integer
    Dim j As Integer
    Sub M()
        Dim c = [||]New C() With {
            .i = 1
        }
        c.j = 1
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)>
        Public Async Function TestFixAllInDocument() As Task
            Await TestAsync(
"
Class C
    Dim i As Integer
    Dim j As Integer
    Sub M()
        Dim array As C()

        array(0) = {|FixAllInDocument:New|} C()
        array(0).i = 1
        array(0).j = 2

        array(1) = New C()
        array(1).i = 3
        array(1).j = 4
    End Sub
End Class",
"
Class C
    Dim i As Integer
    Dim j As Integer
    Sub M()
        Dim array As C()

        array(0) = New C() With {
            .i = 1,
            .j = 2
        }

        array(1) = New C() With {
            .i = 3,
            .j = 4
        }
    End Sub
End Class",
compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)>
        Public Async Function TestTrivia1() As Task
            Await TestAsync(
"
Class C
{
    Dim i As Integer
    Dim j As Integer
    Sub M()
        Dim c = [||]New C()
        c.i = 1 ' Foo
        c.j = 2 ' Bar
    End Sub
End Class",
"
Class C
{
    Dim i As Integer
    Dim j As Integer
    Sub M()
        Dim c = New C() With {
            .i = 1, ' Foo
            .j = 2 ' Bar
            }
    End Sub
End Class",
compareTokens:=False)
        End Function
    End Class
End Namespace