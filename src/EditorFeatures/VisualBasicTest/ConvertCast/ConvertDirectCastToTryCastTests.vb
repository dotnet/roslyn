' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Testing
Imports VerifyVB = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.VisualBasicCodeRefactoringVerifier(Of
    Microsoft.CodeAnalysis.VisualBasic.ConvertCast.VisualBasicConvertDirectCastToTryCastCodeRefactoringProvider)

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ConvertCast
    <Trait(Traits.Feature, Traits.Features.ConvertCast)>
    Public Class ConvertDirectCastToTryCastTests

        <Fact>
        Public Async Function ConvertFromDirectCastToTryCast() As Task
            Dim markup = "
Module Program
    Sub M()
        Dim x = DirectCast(1[||], Object)
    End Sub
End Module
"

            Dim expected = "
Module Program
    Sub M()
        Dim x = TryCast(1, Object)
    End Sub
End Module
"
            Await New VerifyVB.Test With
            {
                .TestCode = markup,
                .FixedCode = expected
            }.RunAsync()
        End Function

        <Fact>
        Public Async Function ConvertFromDirectCastNoConversionIfTypeIsValueType() As Task
            Dim markup = "
Module Program
    Sub M()
        Dim o = new Object()
        Dim x = DirectCast(o[||], Byte)
    End Sub
End Module
"

            Await New VerifyVB.Test With
            {
                .TestCode = markup,
                .FixedCode = markup,
                .OffersEmptyRefactoring = False
            }.RunAsync()
        End Function

        <Fact>
        Public Async Function ConvertFromDirectCastNoConversionIfTypeIsValueType_GeneriDirectCastConstraint() As Task
            Dim markup = "
Module Program
    Sub M(Of T As Structure)()
        Dim o = new Object()
        Dim x = DirectCast([||]o, T)
    End Sub
End Module
"

            Await New VerifyVB.Test With
            {
                .TestCode = markup,
                .FixedCode = markup,
                .OffersEmptyRefactoring = False
            }.RunAsync()
        End Function

        <Fact>
        Public Async Function ConvertFromDirectCastConversionIfTypeIsRefernceType_Constraint() As Task
            Dim markup = "
Module Program
    Sub M(Of T As Class)()
        Dim o = new Object()
        Dim x = DirectCast([||]o, T)
    End Sub
End Module
"
            Dim expected = "
Module Program
    Sub M(Of T As Class)()
        Dim o = new Object()
        Dim x = TryCast(o, T)
    End Sub
End Module
"

            Await New VerifyVB.Test With
            {
                .TestCode = markup,
                .FixedCode = expected
            }.RunAsync()
        End Function

        <Fact>
        Public Async Function ConvertFromDirectCastNoConversionIfTypeIsMissing() As Task
            Dim markup = "
Module Program
    Sub M()
        Dim x = DirectCast([||]1, {|#0:MissingType|})
    End Sub
End Module
"

            Dim verify = New VerifyVB.Test With
            {
                .OffersEmptyRefactoring = False
            }
            verify.TestState.Sources.Add(markup)
            verify.TestState.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError("BC30002").WithLocation(0).WithArguments("MissingType"))
            verify.FixedState.Sources.Add(markup)
            verify.FixedState.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError("BC30002").WithLocation(0).WithArguments("MissingType"))
            Await verify.RunAsync()
        End Function

        <Fact>
        Public Async Function ConvertFromDirectCastNoConversionIfTypeIsValueType_GenericUnconstraint() As Task
            Dim markup = "
Module Program
    Sub M(Of T)()
        Dim o = new Object()
        Dim x = DirectCast([||]o, T)
    End Sub
End Module
"

            Await New VerifyVB.Test With
            {
                .TestCode = markup,
                .FixedCode = markup,
                .OffersEmptyRefactoring = False
            }.RunAsync()
        End Function

        <Fact>
        Public Async Function ConvertFromCBoolIsNotOffered() As Task
            Dim markup = "
Module Program
    Sub M()
        Dim x = CBool([||]1)
    End Sub
End Module
"

            Await New VerifyVB.Test With
            {
                .TestCode = markup,
                .FixedCode = markup,
                .OffersEmptyRefactoring = False
            }.RunAsync()
        End Function

        <Theory>
        <InlineData("DirectCast(DirectCast(1, [||]object), C)",
                    "DirectCast(TryCast(1, object), C)")>
        <InlineData("DirectCast(DirectCast(1, object), [||]C)",
                    "TryCast(DirectCast(1, object), C)")>
        Public Async Function ConvertFromDirectCastNested(DirectCastExpression As String, converted As String) As Task
            Dim markup = "
Public Class C
End Class

Module Program
    Sub M()
        Dim x = " + DirectCastExpression + "
    End Sub
End Module
"

            Dim fixed = "
Public Class C
End Class

Module Program
    Sub M()
        Dim x = " + converted + "
    End Sub
End Module
"

            Await New VerifyVB.Test With
            {
                .TestCode = markup,
                .FixedCode = fixed
            }.RunAsync()
        End Function
    End Class
End Namespace
