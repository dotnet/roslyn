' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        Public Async Function TestReorderMethodParameters_InsufficientParameters_None() As Task
            Dim markup = <Text><![CDATA[
Class C
    Sub $$M()
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, expectedSuccess:=False, expectedErrorText:=FeaturesResources.This_signature_does_not_contain_parameters_that_can_be_changed)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderMethodParameters_InvokeOnOperator_ShouldFail() As Task
            Dim markup = <Text><![CDATA[
Class C
    Public Shared $$Operator +(c1 As C, c2 As C)
        Return Nothing
    End Operator
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, expectedSuccess:=False, expectedErrorText:=FeaturesResources.You_can_only_change_the_signature_of_a_constructor_indexer_method_or_delegate)
        End Function
    End Class
End Namespace
