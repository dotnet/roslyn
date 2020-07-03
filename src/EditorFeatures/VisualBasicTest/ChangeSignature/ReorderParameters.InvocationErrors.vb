' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ChangeSignature
    Partial Public Class ChangeSignatureTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderMethodParameters_InvokeOnClassName_ShouldFail() As Task
            Dim markup = <Text><![CDATA[
Class C$$
    Sub M()
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, expectedSuccess:=False, expectedErrorText:=FeaturesResources.You_can_only_change_the_signature_of_a_constructor_indexer_method_or_delegate)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderMethodParameters_InvokeOnField_ShouldFail() As Task
            Dim markup = <Text><![CDATA[
Class C
    Dim t$$ = 7

    Sub M()
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, expectedSuccess:=False, expectedErrorText:=FeaturesResources.You_can_only_change_the_signature_of_a_constructor_indexer_method_or_delegate)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderMethodParameters_NoChangeableParameters() As Task
            Dim markup = <Text><![CDATA[
Class C
    Public Shared $$Operator +(c1 As C, c2 As C)
        Return Nothing
    End Operator
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, expectedSuccess:=False, expectedErrorText:=FeaturesResources.You_can_only_change_the_signature_of_a_constructor_indexer_method_or_delegate)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
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
