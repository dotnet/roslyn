' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.ChangeSignature
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ChangeSignature
    <Trait(Traits.Feature, Traits.Features.ChangeSignature)>
    Partial Public Class ChangeSignatureTests
        <Fact>
        Public Async Function TestReorderMethodParameters_InvokeOnClassName_ShouldFail() As Task
            Dim markup = <Text><![CDATA[
Class C$$
    Sub M()
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, expectedSuccess:=False, expectedFailureReason:=ChangeSignatureFailureKind.IncorrectKind)
        End Function

        <Fact>
        Public Async Function TestReorderMethodParameters_InvokeOnField_ShouldFail() As Task
            Dim markup = <Text><![CDATA[
Class C
    Dim t$$ = 7

    Sub M()
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, expectedSuccess:=False, expectedFailureReason:=ChangeSignatureFailureKind.IncorrectKind)
        End Function

        <Fact>
        Public Async Function TestReorderMethodParameters_NoChangeableParameters() As Task
            Dim markup = <Text><![CDATA[
Class C
    Public Shared $$Operator +(c1 As C, c2 As C)
        Return Nothing
    End Operator
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, expectedSuccess:=False, expectedFailureReason:=ChangeSignatureFailureKind.IncorrectKind)
        End Function

        <Fact>
        Public Async Function TestChangeSignature_AllowedWithNoParameters() As Task
            Dim markup = <Text><![CDATA[
Class C
    Sub $$M()
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, expectedSuccess:=True)
        End Function
    End Class
End Namespace
