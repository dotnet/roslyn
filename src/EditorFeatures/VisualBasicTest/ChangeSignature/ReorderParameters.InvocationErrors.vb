' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ChangeSignature
    Partial Public Class ChangeSignatureTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Sub ReorderMethodParameters_InvokeOnClassName_ShouldFail()
            Dim markup = <Text><![CDATA[
Class C$$
    Sub M()
    End Sub
End Class]]></Text>.NormalizedValue()

            TestChangeSignatureViaCommand(LanguageNames.VisualBasic, markup, expectedSuccess:=False, expectedErrorText:=FeaturesResources.YouCanOnlyChangeTheSignatureOfAConstructorIndexerMethodOrDelegate)
        End Sub


        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Sub ReorderMethodParameters_InvokeOnField_ShouldFail()
            Dim markup = <Text><![CDATA[
Class C
    Dim t$$ = 7

    Sub M()
    End Sub
End Class]]></Text>.NormalizedValue()

            TestChangeSignatureViaCommand(LanguageNames.VisualBasic, markup, expectedSuccess:=False, expectedErrorText:=FeaturesResources.YouCanOnlyChangeTheSignatureOfAConstructorIndexerMethodOrDelegate)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Sub ReorderMethodParameters_InsufficientParameters_None()
            Dim markup = <Text><![CDATA[
Class C
    Sub $$M()
    End Sub
End Class]]></Text>.NormalizedValue()

            TestChangeSignatureViaCommand(LanguageNames.VisualBasic, markup, expectedSuccess:=False, expectedErrorText:=FeaturesResources.ThisSignatureDoesNotContainParametersThatCanBeChanged)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Sub ReorderMethodParameters_InvokeOnOperator_ShouldFail()
            Dim markup = <Text><![CDATA[
Class C
    Public Shared $$Operator +(c1 As C, c2 As C)
        Return Nothing
    End Operator
End Class]]></Text>.NormalizedValue()

            TestChangeSignatureViaCommand(LanguageNames.VisualBasic, markup, expectedSuccess:=False, expectedErrorText:=FeaturesResources.YouCanOnlyChangeTheSignatureOfAConstructorIndexerMethodOrDelegate)
        End Sub
    End Class
End Namespace
