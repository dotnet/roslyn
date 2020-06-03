' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.OrderModifiers

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.RemoveUnnecessaryByVal

    Public Class RemoveUnnecessaryByValTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New VisualBasicRemoveUnnecessaryByValDiagnosticAnalyzer(),
                    New VisualBasicRemoveUnnecessaryByValCodeFixProvider())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveByVal)>
        Public Async Function TestRemoveByVal() As Task
            TestInRegularAndScript1Async(
"Public Class Program
    Public Sub MySub([|ByVal|] arg As String)
    End Sub
End Class
",
"Public Class Program
    Public Sub MySub(arg As String)
    End Sub
End Class
")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveByVal)>
        Public Async Function TestRemoveByValLowerCase() As Task
            TestInRegularAndScript1Async(
"Public Class Program
    Public Sub MySub([|byval|] arg As String)
    End Sub
End Class
",
"Public Class Program
    Public Sub MySub(arg As String)
    End Sub
End Class
")
        End Function

    End Class

End Namespace
