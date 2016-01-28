' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#Disable Warning RS0007 ' Avoid zero-length array allocations. This is non-shipping test code.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class ParameterModifiersKeywordRecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AllRecommendationsForFirstParameterTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Sub Foo(|</ClassDeclaration>, "ByVal", "ByRef", "Optional", "ParamArray")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AllRecommendationsForSecondParameterAfterByRefFirstTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Sub Foo(ByRef first As Integer, |</ClassDeclaration>, "ByVal", "ByRef", "Optional", "ParamArray")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AllRecommendationsForSecondParameterAfterByValFirstTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Sub Foo(ByVal first As Integer, |</ClassDeclaration>, "ByVal", "ByRef", "Optional", "ParamArray")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AllRecommendationsForFirstParameterAfterGenericParamsTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(<ClassDeclaration>Sub Foo(Of T)(|</ClassDeclaration>, "ByVal", "ByRef", "Optional", "ParamArray")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ByValAndByRefAfterOptionalTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(<ClassDeclaration>Sub Foo(Optional |</ClassDeclaration>, "ByVal", "ByRef")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NothingAfterOptionalByValTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(<ClassDeclaration>Sub Foo(Optional ByVal |</ClassDeclaration>, {})
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NothingAfterByRefOptionalTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(<ClassDeclaration>Sub Foo(ByRef Optional |</ClassDeclaration>, {})
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NothingAfterByValOptionalTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(<ClassDeclaration>Sub Foo(ByVal Optional |</ClassDeclaration>, {})
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NothingAfterOptionalByRefTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(<ClassDeclaration>Sub Foo(Optional ByRef |</ClassDeclaration>, {})
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ByValAfterParamArrayTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(<ClassDeclaration>Sub Foo(ParamArray |</ClassDeclaration>, "ByVal")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NothingAfterPreviousParamArrayTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(<ClassDeclaration>Sub Foo(ParamArray arg1 As Integer(), |</ClassDeclaration>, {})
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OptionalRecommendedAfterPreviousOptionalTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(<ClassDeclaration>Sub Foo(Optional arg1 = 2, |</ClassDeclaration>, "Optional")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoByRefByValOrParamArrayAfterByValTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Sub Foo(ByVal |, |</ClassDeclaration>, "ByVal", "ByRef", "ParamArray")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoByRefByValAfterByRefTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Sub Foo(ByRef |, |</ClassDeclaration>, "ByVal", "ByRef")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AllAppropriateInPropertyParametersTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(<ClassDeclaration>Property Foo(| As Integer</ClassDeclaration>, "ByVal", "Optional", "ParamArray")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AllInExternalMethodDeclarationTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(<ClassDeclaration>Declare Sub Foo Lib "foo.dll" (|</ClassDeclaration>, "ByVal", "ByRef", "Optional", "ParamArray")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AllInExternalDelegateDeclarationTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Delegate Sub Foo(|</ClassDeclaration>, "ByVal", "ByRef")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AllRecommendationsForSubLambdaTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(<MethodBody>Dim x = Sub(|</MethodBody>, "ByVal", "ByRef")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NothingAfterByValInSubLambdaTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(<MethodBody>Dim x = Sub(ByVal |</MethodBody>, {})
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NothingAfterByRefInSubLambdaTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(<MethodBody>Dim x = Sub(ByRef |</MethodBody>, {})
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AllRecommendationsForFunctionLambdaTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(<MethodBody>Dim x = Function(|</MethodBody>, "ByVal", "ByRef")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AllRecommendationsForEventTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(<ClassDeclaration>Event MyEvent(|</ClassDeclaration>, "ByVal", "ByRef")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NothingAfterByValInFunctionLambdaTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(<MethodBody>Dim x = Function(ByVal |</MethodBody>, {})
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NothingAfterByRefInFunctionLambdaTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(<MethodBody>Dim x = Function(ByRef |</MethodBody>, {})
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OnlyByValForFirstParameterOfOperatorTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(<ClassDeclaration>Shared Operator &amp;(|</ClassDeclaration>, "ByVal")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OnlyByValForSecondParameterOfOperatorTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(<ClassDeclaration>Shared Operator &amp;(i As Integer, |</ClassDeclaration>, "ByVal")
        End Function

        <WorkItem(529209)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OnlyByValForPropertyAccessorTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(<PropertyDeclaration>Set(| value As String)</PropertyDeclaration>, "ByVal")
        End Function

        <WorkItem(529209)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OnlyByValForAddHandlerAccessorTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(<CustomEventDeclaration>AddHandler(| value As EventHandler)</CustomEventDeclaration>, "ByVal")
        End Function

        <WorkItem(529209)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OnlyByValForRemoveHandlerAccessorTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(<CustomEventDeclaration>RemoveHandler(| value As EventHandler)</CustomEventDeclaration>, "ByVal")
        End Function

        <WorkItem(529209)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OnlyByValForRemoveHandlerWhenAllAccessorsPresentTest() As Task
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
            Await VerifyRecommendationsAreExactlyAsync(code, "ByVal")
        End Function

        <WorkItem(529209)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OnlyByValForRaiseEventHandlerAccessorTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(<CustomEventDeclaration>RaiseEvent(| sender As Object, e As EventArgs)</CustomEventDeclaration>, "ByVal")
        End Function

        <WorkItem(529209)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OnlyByValForRaiseEventHandlerWhenAllAccessorsPresentTest() As Task
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
            Await VerifyRecommendationsAreExactlyAsync(code, "ByVal")
        End Function

        <WorkItem(530953)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterLineContinuationTest() As Task
            Await VerifyRecommendationsContainAsync(
<ClassDeclaration>Sub Foo(
|</ClassDeclaration>, "ByVal", "ByRef", "Optional", "ParamArray")
        End Function
    End Class
End Namespace
