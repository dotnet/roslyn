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

        Private Shared ReadOnly s_parseOptions As VisualBasicParseOptions =
            VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest)

        <Fact>
        Public Async Function TestInferredTupleName() As Task
            Await TestAsync(
"
Class C
    Sub M()
        Dim a As Integer = 1
        Dim t = ( [||]a:= a, 2)
    End Sub
End Class
",
"
Class C
    Sub M()
        Dim a As Integer = 1
        Dim t = (a, 2)
    End Sub
End Class
", parseOptions:=s_parseOptions)
        End Function

        <Fact>
        Public Async Function TestInferredTupleName2() As Task
            Await TestAsync(
"
Class C
    Sub M()
        Dim a As Integer = 2
        Dim t = (1,  [||]a:= a )
    End Sub
End Class
",
"
Class C
    Sub M()
        Dim a As Integer = 2
        Dim t = (1, a)
    End Sub
End Class
", parseOptions:=s_parseOptions)
        End Function

        <Fact>
        Public Async Function TestFixAllInferredTupleName() As Task
            Await TestAsync(
"
Class C
    Sub M()
        Dim a As Integer = 1
        Dim b As Integer = 2
        Dim t = ({|FixAllInDocument:a:=|}a, b:=b)
    End Sub
End Class
",
"
Class C
    Sub M()
        Dim a As Integer = 1
        Dim b As Integer = 2
        Dim t = (a, b)
    End Sub
End Class
", parseOptions:=s_parseOptions)
        End Function

        <Fact>
        Public Async Function TestInferredAnonymousTypeMemberName() As Task
            Await TestAsync(
"
Class C
    Sub M()
        Dim a As Integer = 1
        Dim t = New With {[|.a =|] a, .b = 2}
    End Sub
End Class
",
"
Class C
    Sub M()
        Dim a As Integer = 1
        Dim t = New With {a, .b = 2}
    End Sub
End Class
", parseOptions:=s_parseOptions)
        End Function

        <Fact>
        Public Async Function TestFixAllInferredAnonymousTypeMemberName() As Task
            Await TestAsync(
"
Class C
    Sub M()
        Dim a As Integer = 1
        Dim b As Integer = 2
        Dim t = New With {{|FixAllInDocument:.a =|} a, .b = b}
    End Sub
End Class
",
"
Class C
    Sub M()
        Dim a As Integer = 1
        Dim b As Integer = 2
        Dim t = New With {a, b}
    End Sub
End Class
", parseOptions:=s_parseOptions)
        End Function

    End Class
End Namespace
