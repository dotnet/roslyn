﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class ParameterModifiersKeywordRecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllRecommendationsForFirstParameterTest()
            VerifyRecommendationsContain(<ClassDeclaration>Sub Goo(|</ClassDeclaration>, "ByVal", "ByRef", "Optional", "ParamArray")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllRecommendationsForSecondParameterAfterByRefFirstTest()
            VerifyRecommendationsContain(<ClassDeclaration>Sub Goo(ByRef first As Integer, |</ClassDeclaration>, "ByVal", "ByRef", "Optional", "ParamArray")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllRecommendationsForSecondParameterAfterByValFirstTest()
            VerifyRecommendationsContain(<ClassDeclaration>Sub Goo(ByVal first As Integer, |</ClassDeclaration>, "ByVal", "ByRef", "Optional", "ParamArray")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllRecommendationsForFirstParameterAfterGenericParamsTest()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Sub Goo(Of T)(|</ClassDeclaration>, "ByVal", "ByRef", "Optional", "ParamArray")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ByValAndByRefAfterOptionalTest()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Sub Goo(Optional |</ClassDeclaration>, "ByVal", "ByRef")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NothingAfterOptionalByValTest()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Sub Goo(Optional ByVal |</ClassDeclaration>, {})
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NothingAfterByRefOptionalTest()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Sub Goo(ByRef Optional |</ClassDeclaration>, {})
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NothingAfterByValOptionalTest()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Sub Goo(ByVal Optional |</ClassDeclaration>, {})
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NothingAfterOptionalByRefTest()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Sub Goo(Optional ByRef |</ClassDeclaration>, {})
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ByValAfterParamArrayTest()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Sub Goo(ParamArray |</ClassDeclaration>, "ByVal")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NothingAfterPreviousParamArrayTest()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Sub Goo(ParamArray arg1 As Integer(), |</ClassDeclaration>, {})
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OptionalRecommendedAfterPreviousOptionalTest()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Sub Goo(Optional arg1 = 2, |</ClassDeclaration>, "Optional")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoByRefByValOrParamArrayAfterByValTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Sub Goo(ByVal |, |</ClassDeclaration>, "ByVal", "ByRef", "ParamArray")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoByRefByValAfterByRefTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Sub Goo(ByRef |, |</ClassDeclaration>, "ByVal", "ByRef")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllAppropriateInPropertyParametersTest()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Property Goo(| As Integer</ClassDeclaration>, "ByVal", "Optional", "ParamArray")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllInExternalMethodDeclarationTest()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Declare Sub Goo Lib "goo.dll" (|</ClassDeclaration>, "ByVal", "ByRef", "Optional", "ParamArray")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllInExternalDelegateDeclarationTest()
            VerifyRecommendationsContain(<ClassDeclaration>Delegate Sub Goo(|</ClassDeclaration>, "ByVal", "ByRef")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllRecommendationsForSubLambdaTest()
            VerifyRecommendationsAreExactly(<MethodBody>Dim x = Sub(|</MethodBody>, "ByVal", "ByRef")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NothingAfterByValInSubLambdaTest()
            VerifyRecommendationsAreExactly(<MethodBody>Dim x = Sub(ByVal |</MethodBody>, {})
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NothingAfterByRefInSubLambdaTest()
            VerifyRecommendationsAreExactly(<MethodBody>Dim x = Sub(ByRef |</MethodBody>, {})
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllRecommendationsForFunctionLambdaTest()
            VerifyRecommendationsAreExactly(<MethodBody>Dim x = Function(|</MethodBody>, "ByVal", "ByRef")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllRecommendationsForEventTest()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Event MyEvent(|</ClassDeclaration>, "ByVal", "ByRef")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NothingAfterByValInFunctionLambdaTest()
            VerifyRecommendationsAreExactly(<MethodBody>Dim x = Function(ByVal |</MethodBody>, {})
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NothingAfterByRefInFunctionLambdaTest()
            VerifyRecommendationsAreExactly(<MethodBody>Dim x = Function(ByRef |</MethodBody>, {})
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OnlyByValForFirstParameterOfOperatorTest()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Shared Operator &amp;(|</ClassDeclaration>, "ByVal")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OnlyByValForSecondParameterOfOperatorTest()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Shared Operator &amp;(i As Integer, |</ClassDeclaration>, "ByVal")
        End Sub

        <WorkItem(529209, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529209")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OnlyByValForPropertyAccessorTest()
            VerifyRecommendationsAreExactly(<PropertyDeclaration>Set(| value As String)</PropertyDeclaration>, "ByVal")
        End Sub

        <WorkItem(529209, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529209")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OnlyByValForAddHandlerAccessorTest()
            VerifyRecommendationsAreExactly(<CustomEventDeclaration>AddHandler(| value As EventHandler)</CustomEventDeclaration>, "ByVal")
        End Sub

        <WorkItem(529209, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529209")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OnlyByValForRemoveHandlerAccessorTest()
            VerifyRecommendationsAreExactly(<CustomEventDeclaration>RemoveHandler(| value As EventHandler)</CustomEventDeclaration>, "ByVal")
        End Sub

        <WorkItem(529209, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529209")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OnlyByValForRemoveHandlerWhenAllAccessorsPresentTest()
            Dim code =
<File>
Class C
        Public Custom Event Click As EventHandler
        AddHandler(v As EventHandler)
        End AddHandler
        RemoveHandler(| value As EventHandler)
        End RemoveHandler
        RaiseEvent(ByVal sender As Object, e As EventArgs)
        End RaiseEvent
    End Event
End Class
</File>
            VerifyRecommendationsAreExactly(code, "ByVal")
        End Sub

        <WorkItem(529209, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529209")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OnlyByValForRaiseEventHandlerAccessorTest()
            VerifyRecommendationsAreExactly(<CustomEventDeclaration>RaiseEvent(| sender As Object, e As EventArgs)</CustomEventDeclaration>, "ByVal")
        End Sub

        <WorkItem(529209, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529209")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OnlyByValForRaiseEventHandlerWhenAllAccessorsPresentTest()
            Dim code =
<File>
Class C
        Public Custom Event Click As EventHandler
        AddHandler(v As EventHandler)
        End AddHandler
        RemoveHandler(ByVal value As EventHandler)
        End RemoveHandler
        RaiseEvent(| sender As Object, e As EventArgs)
        End RaiseEvent
    End Event
End Class
</File>
            VerifyRecommendationsAreExactly(code, "ByVal")
        End Sub

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterLineContinuationTest()
            VerifyRecommendationsContain(
<ClassDeclaration>Sub Goo(
|</ClassDeclaration>, "ByVal", "ByRef", "Optional", "ParamArray")
        End Sub
    End Class
End Namespace
