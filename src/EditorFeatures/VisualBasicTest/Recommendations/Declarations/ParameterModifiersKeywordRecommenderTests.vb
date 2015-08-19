' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

#Disable Warning RS0007 ' Avoid zero-length array allocations. This is non-shipping test code.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class ParameterModifiersKeywordRecommenderTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllRecommendationsForFirstParameter()
            VerifyRecommendationsContain(<ClassDeclaration>Sub Foo(|</ClassDeclaration>, "ByVal", "ByRef", "Optional", "ParamArray")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllRecommendationsForSecondParameterAfterByRefFirst()
            VerifyRecommendationsContain(<ClassDeclaration>Sub Foo(ByRef first As Integer, |</ClassDeclaration>, "ByVal", "ByRef", "Optional", "ParamArray")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllRecommendationsForSecondParameterAfterByValFirst()
            VerifyRecommendationsContain(<ClassDeclaration>Sub Foo(ByVal first As Integer, |</ClassDeclaration>, "ByVal", "ByRef", "Optional", "ParamArray")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllRecommendationsForFirstParameterAfterGenericParams()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Sub Foo(Of T)(|</ClassDeclaration>, "ByVal", "ByRef", "Optional", "ParamArray")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ByValAndByRefAfterOptional()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Sub Foo(Optional |</ClassDeclaration>, "ByVal", "ByRef")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NothingAfterOptionalByVal()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Sub Foo(Optional ByVal |</ClassDeclaration>, {})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NothingAfterByRefOptional()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Sub Foo(ByRef Optional |</ClassDeclaration>, {})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NothingAfterByValOptional()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Sub Foo(ByVal Optional |</ClassDeclaration>, {})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NothingAfterOptionalByRef()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Sub Foo(Optional ByRef |</ClassDeclaration>, {})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ByValAfterParamArray()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Sub Foo(ParamArray |</ClassDeclaration>, "ByVal")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NothingAfterPreviousParamArray()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Sub Foo(ParamArray arg1 As Integer(), |</ClassDeclaration>, {})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OptionalRecommendedAfterPreviousOptional()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Sub Foo(Optional arg1 = 2, |</ClassDeclaration>, "Optional")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoByRefByValOrParamArrayAfterByVal()
            VerifyRecommendationsMissing(<ClassDeclaration>Sub Foo(ByVal |, |</ClassDeclaration>, "ByVal", "ByRef", "ParamArray")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoByRefByValAfterByRef()
            VerifyRecommendationsMissing(<ClassDeclaration>Sub Foo(ByRef |, |</ClassDeclaration>, "ByVal", "ByRef")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllAppropriateInPropertyParameters()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Property Foo(| As Integer</ClassDeclaration>, "ByVal", "Optional", "ParamArray")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllInExternalMethodDeclaration()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Declare Sub Foo Lib "foo.dll" (|</ClassDeclaration>, "ByVal", "ByRef", "Optional", "ParamArray")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllInExternalDelegateDeclaration()
            VerifyRecommendationsContain(<ClassDeclaration>Delegate Sub Foo(|</ClassDeclaration>, "ByVal", "ByRef")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllRecommendationsForSubLambda()
            VerifyRecommendationsAreExactly(<MethodBody>Dim x = Sub(|</MethodBody>, "ByVal", "ByRef")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NothingAfterByValInSubLambda()
            VerifyRecommendationsAreExactly(<MethodBody>Dim x = Sub(ByVal |</MethodBody>, {})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NothingAfterByRefInSubLambda()
            VerifyRecommendationsAreExactly(<MethodBody>Dim x = Sub(ByRef |</MethodBody>, {})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllRecommendationsForFunctionLambda()
            VerifyRecommendationsAreExactly(<MethodBody>Dim x = Function(|</MethodBody>, "ByVal", "ByRef")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllRecommendationsForEvent()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Event MyEvent(|</ClassDeclaration>, "ByVal", "ByRef")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NothingAfterByValInFunctionLambda()
            VerifyRecommendationsAreExactly(<MethodBody>Dim x = Function(ByVal |</MethodBody>, {})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NothingAfterByRefInFunctionLambda()
            VerifyRecommendationsAreExactly(<MethodBody>Dim x = Function(ByRef |</MethodBody>, {})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OnlyByValForFirstParameterOfOperator()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Shared Operator &amp;(|</ClassDeclaration>, "ByVal")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OnlyByValForSecondParameterOfOperator()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Shared Operator &amp;(i As Integer, |</ClassDeclaration>, "ByVal")
        End Sub

        <WorkItem(529209)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OnlyByValForPropertyAccessor()
            VerifyRecommendationsAreExactly(<PropertyDeclaration>Set(| value As String)</PropertyDeclaration>, "ByVal")
        End Sub

        <WorkItem(529209)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OnlyByValForAddHandlerAccessor()
            VerifyRecommendationsAreExactly(<CustomEventDeclaration>AddHandler(| value As EventHandler)</CustomEventDeclaration>, "ByVal")
        End Sub

        <WorkItem(529209)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OnlyByValForRemoveHandlerAccessor()
            VerifyRecommendationsAreExactly(<CustomEventDeclaration>RemoveHandler(| value As EventHandler)</CustomEventDeclaration>, "ByVal")
        End Sub

        <WorkItem(529209)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OnlyByValForRemoveHandlerWhenAllAccessorsPresent()
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

        <WorkItem(529209)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OnlyByValForRaiseEventHandlerAccessor()
            VerifyRecommendationsAreExactly(<CustomEventDeclaration>RaiseEvent(| sender As Object, e As EventArgs)</CustomEventDeclaration>, "ByVal")
        End Sub

        <WorkItem(529209)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OnlyByValForRaiseEventHandlerWhenAllAccessorsPresent()
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

        <WorkItem(530953)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterLineContinuation()
            VerifyRecommendationsContain(
<ClassDeclaration>Sub Foo(
|</ClassDeclaration>, "ByVal", "ByRef", "Optional", "ParamArray")
        End Sub
    End Class
End Namespace
