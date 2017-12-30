' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.Ambiguity
    Public Class AmbiguousTypeTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (Nothing, New VisualBasicAmbiguousTypeCodeFixProvider())
        End Function

        Private Function GetAmbiguousDefinition(ByVal typeDefinion As String) As String
            Return $"
Namespace N1
    {typeDefinion}
End Namespace 
Namespace N2
    {typeDefinion}
End Namespace"
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAliasType)>
        Public Async Function TestAmbiguousClassObjectCreationUsingsInNamespace() As Task
            Dim classDef = GetAmbiguousDefinition("
Public Class Ambiguous
End Class")
            Dim initialMarkup = "
Imports N1
Imports N2
" & classDef & "

Namespace N3
    Class C
        Private Sub M()
            Dim a = New [|Ambiguous|]()
        End Sub
    End Class
End Namespace"
            Dim expectedMarkupTemplate = "
Imports N1
Imports N2
#
" & classDef & "

Namespace N3
    Class C
        Private Sub M()
            Dim a = New Ambiguous()
        End Sub
    End Class
End Namespace"
            Await TestInRegularAndScriptAsync(initialMarkup, expectedMarkupTemplate.Replace("#", "Imports Ambiguous = N1.Ambiguous"), 0)
            Await TestInRegularAndScriptAsync(initialMarkup, expectedMarkupTemplate.Replace("#", "Imports Ambiguous = N2.Ambiguous"), 1)
        End Function
    End Class
End Namespace
