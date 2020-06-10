' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessaryByVal

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.RemoveUnnecessaryByVal
    Public Class RemoveUnnecessaryByValTests
        Private Shared Async Function VerifyCodeFixAsync(source As String, fixedSource As String) As Task
            Await New VerifyVB.Test With
            {
                .TestCode = source,
                .FixedCode = fixedSource,
                ' This analyzer has special behavior in generated code that needs to be tested separately
                TestBehaviors = TestBehaviors.SkipGeneratedCodeCheck,
            }.RunAsync()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveByVal)>
        Public Async Function TestRemoveByVal() As Task
            Await VerifyCodeFixAsync(
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
            Await VerifyCodeFixAsync(
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


        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveByVal)>
        Public Async Function TestRemoveByValMoreThanOneModifier() As Task
            Await VerifyCodeFixAsync(
"Public Class Program
    Public Sub MySub(Optional [|ByVal|] arg As String = "Default")
    End Sub
End Class
",
"Public Class Program
    Public Sub MySub(Optional arg As String = "Default")
    End Sub
End Class
")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveByVal)>
        Public Async Function TestRemoveByValCodeHasError() As Task
            Await VerifyCodeFixAsync(
"Public Class Program
    Public Sub MySub([|ByVal|] arg)
    End Sub
End Class
",
"Public Class Program
    Public Sub MySub(arg)
    End Sub
End Class
")
        End Function
    End Class
End Namespace
