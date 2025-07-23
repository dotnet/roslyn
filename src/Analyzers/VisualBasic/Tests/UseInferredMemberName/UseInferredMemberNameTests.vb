' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.UseInferredMemberName

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.UseInferredMemberName
    <Trait(Traits.Feature, Traits.Features.CodeActionsUseInferredMemberName)>
    Public Class UseInferredMemberNameTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest_NoEditor

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
", New TestParameters(parseOptions:=s_parseOptions))
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24480")>
        Public Async Function TestInferredTupleName_WithAmbiguity() As Task
            Await TestMissingAsync(
"
Class C
    Sub M()
        Dim alice As Integer = 1
        Dim Alice As Integer = 2
        Dim t = ( [||]alice:= alice, Alice)
    End Sub
End Class
", parameters:=New TestParameters(s_parseOptions))
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23659")>
        Public Async Function TestMissingForObjectCreation() As Task
            Await TestMissingAsync(
"
Public Class C
    Public Property P As Integer

    Sub M(p As Integer)
        Dim f = New C With { [|.P|] = p }
    End Sub
End Class
", New TestParameters(s_parseOptions))
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
", New TestParameters(parseOptions:=s_parseOptions))
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
", New TestParameters(parseOptions:=s_parseOptions))
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
", New TestParameters(parseOptions:=s_parseOptions))
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24480")>
        Public Async Function TestInferredAnonymousTypeMemberName_WithAmbiguity() As Task
            Await TestMissingAsync("
Class C
    Sub M()
        Dim alice As Integer = 1
        Dim Alice As Integer = 2
        Dim t = New With {[|.alice =|] alice, Alice}
    End Sub
End Class
", parameters:=New TestParameters(s_parseOptions))
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
", New TestParameters(parseOptions:=s_parseOptions))
        End Function
    End Class
End Namespace
