' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.UseInferredMemberName

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.UseInferredMemberName
    <Trait(Traits.Feature, Traits.Features.CodeActionsUseInferredMemberName)>
    Public Class UseInferredMemberNameTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New VisualBasicUseInferredMemberNameDiagnosticAnalyzer(),
                    New VisualBasicUseInferredMemberNameCodeFixProvider())
        End Function

        <Fact>
        Public Async Function TestInferredTupleName() As Task
            Await TestInRegularAndScriptAsync(
"
Class C
    Sub M()
        Dim a As Integer = 1
        Dim t = ([||]a:=a, 2)
    End Sub
End Class",
"
Class C
    Sub M()
        Dim a As Integer = 1
        Dim t = (a, 2)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestInferredTupleName2() As Task
            Await TestInRegularAndScriptAsync(
"
Class C
    Sub M()
        Dim a As Integer = 2
        Dim t = (1, [||]a:=a)
    End Sub
End Class",
"
Class C
    Sub M()
        Dim a As Integer = 2
        Dim t = (1, a)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestNoFixAllInferredTupleName() As Task
            Await TestActionCountAsync(
"
Class C
    Sub M()
        Dim a As Integer = 2
        Dim t = (1, [||]a:=a)
    End Sub
End Class",
count:=1)
        End Function

        <Fact>
        Public Async Function TestInferredAnonymousTypeMemberName() As Task
            Await TestInRegularAndScriptAsync(
"
Class C
    Sub M()
        Dim a As Integer = 1
        Dim t = New With {[||].a = a, .b = 2}
    End Sub
End Class",
"
Class C
    Sub M()
        Dim a As Integer = 1
        Dim t = New With {a, .b = 2}
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestNoFixAllInferredAnonymousTypeMemberName() As Task
            Await TestActionCountAsync(
"
Class C
    Sub M()
        Dim a As Integer = 1
        Dim t = New With {[||].a = a, .b = 2}
    End Sub
End Class",
count:=1)
        End Function
    End Class
End Namespace
