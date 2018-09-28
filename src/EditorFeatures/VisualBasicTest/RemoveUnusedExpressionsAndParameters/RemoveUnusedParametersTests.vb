' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.RemoveUnusedExpressionsAndParameters

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.RemoveUnusedExpressionsAndParameters
    Public Class RemoveUnusedParametersTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest
        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New VisualBasicRemoveUnusedExpressionsAndParametersDiagnosticAnalyzer(), New VisualBasicRemoveUnusedExpressionsAndParametersCodeFixProvider())
        End Function

        ' Ensure that we explicitly test missing IDE0058, which has no corresponding code fix (non-fixable diagnostic).
        Private Overloads Function TestDiagnosticMissingAsync(initialMarkup As String) As Task
            Return TestDiagnosticMissingAsync(initialMarkup, New TestParameters(retainNonFixableDiagnostics:=True))
        End Function

        Private Shared Function Diagnostic(id As String) As DiagnosticDescription
            Return TestHelpers.Diagnostic(id)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function Parameter_Used() As Task
            Await TestDiagnosticMissingAsync(
$"Class C
    Sub M([|p|] As Integer)
        Dim x = p
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function Parameter_Unused() As Task
            Await TestDiagnosticsAsync(
$"Class C
    Sub M([|p|] As Integer)
    End Sub
End Class", parameters:=Nothing,
            Diagnostic(IDEDiagnosticIds.ParameterCanBeRemovedDiagnosticId))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function Parameter_WrittenOnly() As Task
            Await TestDiagnosticsAsync(
$"Class C
    Sub M([|p|] As Integer)
        p = 1
    End Sub
End Class", parameters:=Nothing,
            Diagnostic(IDEDiagnosticIds.ParameterCanBeRemovedDiagnosticId))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function Parameter_WrittenThenRead() As Task
            Await TestDiagnosticsAsync(
$"Class C
    Function M([|p|] As Integer) As Integer
        p = 1
        Return p
    End Function
End Class", parameters:=Nothing,
            Diagnostic(IDEDiagnosticIds.ParameterCanBeRemovedDiagnosticId))
        End Function
    End Class
End Namespace
