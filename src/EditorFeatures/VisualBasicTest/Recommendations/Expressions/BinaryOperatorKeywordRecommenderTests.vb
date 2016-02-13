' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    Public Class BinaryOperatorKeywordRecommenderTests
        Private Shared ReadOnly s_expectedKeywords As String() = BinaryOperatorKeywordRecommender.KeywordList.Select(Function(k) k.Keyword).ToArray()

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoneInClassDeclarationTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>|</ClassDeclaration>, s_expectedKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoneInMethodDeclarationTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>|</MethodBody>, s_expectedKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AllAfterLocalDeclarationNumericLiteralInitializerTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim i As Integer = 1 |</MethodBody>, s_expectedKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AllAfterNumberInAssignmentTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>x = 1 |</MethodBody>, s_expectedKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AllAfterStringInAssignmentTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>x = "asdf" |</MethodBody>, s_expectedKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AllAfterObjectCreationInAssignmentTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>x = New Object() |</MethodBody>, s_expectedKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoneAfterObjectCreationInDeclarationTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>Dim x = New Object |</MethodBody>, s_expectedKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoneAfterObjectCreationWithParensInDeclarationTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>Dim x = New Object() |</MethodBody>, s_expectedKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoneAfterAsNewInDeclarationTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>Dim x As New Object |</MethodBody>, s_expectedKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoneAfterAsNewWithParensInDeclarationTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>Dim x As New Object() |</MethodBody>, s_expectedKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AllAfterMethodCallInAsNewClauseTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x As New Object(Foo() |)</MethodBody>, s_expectedKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AllAfterPropertyAccessInAssignmentTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>x = Foo.Bar |</MethodBody>, s_expectedKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AllAfterMethodCallInAssignmentTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>x = Foo.Bar() |</MethodBody>, s_expectedKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoneAfterDotInImportsTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>Imports System.|</File>, s_expectedKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoneInSubLambdaParameterListTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>Dim x = Sub(x |</MethodBody>, s_expectedKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoneInFunctionLambdaParameterListTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>Dim x = Function(x |</MethodBody>, s_expectedKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoneInQueryVariableListTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>Dim x = From y |</MethodBody>, s_expectedKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoneInQueryVariableList2Test() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>Dim x = From y In {1, 2, 3} Let z |</MethodBody>, s_expectedKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AllInSubLambdaBodyTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = Sub(x As Integer) x |</MethodBody>, s_expectedKeywords)
        End Function

        <WorkItem(541354, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541354")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterStringLiteralTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>test = "F" |</MethodBody>, s_expectedKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AllInFunctionLambdaBodyTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = Function(x As Integer) x |</MethodBody>, s_expectedKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoneInObjectMemberInitializer1Test() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>Dim y = New foo() With {|</MethodBody>, s_expectedKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoneInObjectMemberInitializer2Test() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>Dim y = New foo() With {.|</MethodBody>, s_expectedKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoneInObjectMemberInitializer3Test() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>Dim y = New foo() With {.x|</MethodBody>, s_expectedKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoneInObjectMemberInitializer4Test() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>Dim y = New foo() With {.x |</MethodBody>, s_expectedKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoneInCatchStatement1Test() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>
                                             Try
                                             Catch ex |
                                         </MethodBody>, s_expectedKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoneInCatchStatement2Test() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>
                                             Try
                                             Catch ex As Exception |
                                         </MethodBody>, s_expectedKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoneInDimArrayBounds1Test() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>Dim i(0 |</MethodBody>, s_expectedKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AllInDimArrayBounds2Test() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim i(0 To 4 |</MethodBody>, s_expectedKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoneInReDimArrayBounds1Test() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>ReDim i(0 |</MethodBody>, s_expectedKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AllInReDimArrayBounds2Test() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>ReDim i(0 To 4 |</MethodBody>, s_expectedKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoneAfterQueryFromTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>Dim query = From |</MethodBody>, s_expectedKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoneAfterQueryAggregateTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>Dim query = Aggregate |</MethodBody>, s_expectedKeywords)
        End Function

        <WorkItem(543637, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543637")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoneInTypeArgumentListTest() As Task
            Dim code =
            <File>
Module M
    Sub Foo(Of T As Class)()
        Foo(Of T |
    End Sub
End Module
</File>

            Await VerifyRecommendationsMissingAsync(code, s_expectedKeywords)
        End Function

        <WorkItem(544106, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544106")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoneAfterAddHandlerEventNameTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>AddHandler System.Console.CancelKeyPress |</MethodBody>, s_expectedKeywords)
        End Function

        <WorkItem(544106, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544106")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoneAfterAddHandlerIdentifierTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>AddHandler System.Console.CancelKeyPress, Foo |</MethodBody>, s_expectedKeywords)
        End Function

        <WorkItem(544106, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544106")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoneAfterAddHandlerAddressOfIdentifierTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>AddHandler System.Console.CancelKeyPress, AddressOf Foo |</MethodBody>, s_expectedKeywords)
        End Function

        <WorkItem(544106, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544106")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoneAfterRemoveHandlerEventNameTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>RemoveHandler System.Console.CancelKeyPress |</MethodBody>, s_expectedKeywords)
        End Function

        <WorkItem(544106, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544106")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoneAfterRemoveHandlerIdentifierTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>RemoveHandler System.Console.CancelKeyPress, Foo |</MethodBody>, s_expectedKeywords)
        End Function

        <WorkItem(544106, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544106")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoneAfterRemoveHandlerAddressOfIdentifierTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>RemoveHandler System.Console.CancelKeyPress, AddressOf Foo |</MethodBody>, s_expectedKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoneAfterNamespaceAliasInImportsTest() As Task
            Dim code =
            <File>
Imports S |
</File>

            Await VerifyRecommendationsMissingAsync(code, s_expectedKeywords)
        End Function

        <WorkItem(546505, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546505")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoCrashInVariableDeclarationTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>Dim x As New TypeInfo(New |)</MethodBody>, s_expectedKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending), WorkItem(544278, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544278")>
        Public Async Function NoneAfterMidStatementTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>Mid(s, 1, 1) |</MethodBody>, s_expectedKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending), WorkItem(544576, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544576")>
        Public Async Function NoneAfterExternalMethodDeclarationTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Declare Sub S Lib "L" Alias "A" |</ClassDeclaration>, s_expectedKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending), WorkItem(545988, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545988")>
        Public Async Function NoneAfterNamedArgumentTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>Foo(f:=|</MethodBody>, s_expectedKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending), WorkItem(546659, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546659")>
        Public Async Function NoneInUsingStatementTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>Using Foo |</MethodBody>, s_expectedKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending), WorkItem(531329, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531329")>
        Public Async Function NoneInForStatementTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>For i = 1 |</MethodBody>, s_expectedKeywords)
        End Function

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterEolTest() As Task
            Await VerifyRecommendationsMissingAsync(
<MethodBody>test = "F" 
|</MethodBody>, s_expectedKeywords)
        End Function

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterExplicitLineContinuationTest() As Task
            Await VerifyRecommendationsContainAsync(
<MethodBody>test = "F" _
|</MethodBody>, s_expectedKeywords)
        End Function

        <WorkItem(975804, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/975804")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterIncompleteObjectCreationTest() As Task
            Await VerifyRecommendationsMissingAsync(
<MethodBody>Dim x = new Foo.|
</MethodBody>, s_expectedKeywords)
        End Function
    End Class
End Namespace
