﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    Public Class BinaryOperatorKeywordRecommenderTests
        Private Shared ReadOnly s_expectedKeywords As String() = BinaryOperatorKeywordRecommender.KeywordList.Select(Function(k) k.Keyword).ToArray()

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInClassDeclarationTest()
            VerifyRecommendationsMissing(<ClassDeclaration>|</ClassDeclaration>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInMethodDeclarationTest()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllAfterLocalDeclarationNumericLiteralInitializerTest()
            VerifyRecommendationsContain(<MethodBody>Dim i As Integer = 1 |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllAfterNumberInAssignmentTest()
            VerifyRecommendationsContain(<MethodBody>x = 1 |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllAfterStringInAssignmentTest()
            VerifyRecommendationsContain(<MethodBody>x = "asdf" |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllAfterObjectCreationInAssignmentTest()
            VerifyRecommendationsContain(<MethodBody>x = New Object() |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneAfterObjectCreationInDeclarationTest()
            VerifyRecommendationsMissing(<MethodBody>Dim x = New Object |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneAfterObjectCreationWithParensInDeclarationTest()
            VerifyRecommendationsMissing(<MethodBody>Dim x = New Object() |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneAfterAsNewInDeclarationTest()
            VerifyRecommendationsMissing(<MethodBody>Dim x As New Object |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneAfterAsNewWithParensInDeclarationTest()
            VerifyRecommendationsMissing(<MethodBody>Dim x As New Object() |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllAfterMethodCallInAsNewClauseTest()
            VerifyRecommendationsContain(<MethodBody>Dim x As New Object(Goo() |)</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllAfterPropertyAccessInAssignmentTest()
            VerifyRecommendationsContain(<MethodBody>x = Goo.Bar |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllAfterMethodCallInAssignmentTest()
            VerifyRecommendationsContain(<MethodBody>x = Goo.Bar() |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneAfterDotInImportsTest()
            VerifyRecommendationsMissing(<File>Imports System.|</File>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInSubLambdaParameterListTest()
            VerifyRecommendationsMissing(<MethodBody>Dim x = Sub(x |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInFunctionLambdaParameterListTest()
            VerifyRecommendationsMissing(<MethodBody>Dim x = Function(x |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInQueryVariableListTest()
            VerifyRecommendationsMissing(<MethodBody>Dim x = From y |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInQueryVariableList2Test()
            VerifyRecommendationsMissing(<MethodBody>Dim x = From y In {1, 2, 3} Let z |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllInSubLambdaBodyTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = Sub(x As Integer) x |</MethodBody>, s_expectedKeywords)
        End Sub

        <WorkItem(541354, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541354")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterStringLiteralTest()
            VerifyRecommendationsContain(<MethodBody>test = "F" |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllInFunctionLambdaBodyTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = Function(x As Integer) x |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInObjectMemberInitializer1Test()
            VerifyRecommendationsMissing(<MethodBody>Dim y = New goo() With {|</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInObjectMemberInitializer2Test()
            VerifyRecommendationsMissing(<MethodBody>Dim y = New goo() With {.|</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInObjectMemberInitializer3Test()
            VerifyRecommendationsMissing(<MethodBody>Dim y = New goo() With {.x|</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInObjectMemberInitializer4Test()
            VerifyRecommendationsMissing(<MethodBody>Dim y = New goo() With {.x |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInCatchStatement1Test()
            VerifyRecommendationsMissing(<MethodBody>
                                             Try
                                             Catch ex |
                                         </MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInCatchStatement2Test()
            VerifyRecommendationsMissing(<MethodBody>
                                             Try
                                             Catch ex As Exception |
                                         </MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInDimArrayBounds1Test()
            VerifyRecommendationsMissing(<MethodBody>Dim i(0 |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllInDimArrayBounds2Test()
            VerifyRecommendationsContain(<MethodBody>Dim i(0 To 4 |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInReDimArrayBounds1Test()
            VerifyRecommendationsMissing(<MethodBody>ReDim i(0 |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllInReDimArrayBounds2Test()
            VerifyRecommendationsContain(<MethodBody>ReDim i(0 To 4 |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneAfterQueryFromTest()
            VerifyRecommendationsMissing(<MethodBody>Dim query = From |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneAfterQueryAggregateTest()
            VerifyRecommendationsMissing(<MethodBody>Dim query = Aggregate |</MethodBody>, s_expectedKeywords)
        End Sub

        <WorkItem(543637, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543637")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInTypeArgumentListTest()
            Dim code =
            <File>
Module M
    Sub Goo(Of T As Class)()
        Goo(Of T |
    End Sub
End Module
</File>

            VerifyRecommendationsMissing(code, s_expectedKeywords)
        End Sub

        <WorkItem(544106, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544106")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneAfterAddHandlerEventNameTest()
            VerifyRecommendationsMissing(<MethodBody>AddHandler System.Console.CancelKeyPress |</MethodBody>, s_expectedKeywords)
        End Sub

        <WorkItem(544106, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544106")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneAfterAddHandlerIdentifierTest()
            VerifyRecommendationsContain(<MethodBody>AddHandler System.Console.CancelKeyPress, Goo |</MethodBody>, s_expectedKeywords)
        End Sub

        <WorkItem(544106, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544106")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneAfterAddHandlerAddressOfIdentifierTest()
            VerifyRecommendationsMissing(<MethodBody>AddHandler System.Console.CancelKeyPress, AddressOf Goo |</MethodBody>, s_expectedKeywords)
        End Sub

        <WorkItem(544106, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544106")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneAfterRemoveHandlerEventNameTest()
            VerifyRecommendationsMissing(<MethodBody>RemoveHandler System.Console.CancelKeyPress |</MethodBody>, s_expectedKeywords)
        End Sub

        <WorkItem(544106, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544106")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneAfterRemoveHandlerIdentifierTest()
            VerifyRecommendationsContain(<MethodBody>RemoveHandler System.Console.CancelKeyPress, Goo |</MethodBody>, s_expectedKeywords)
        End Sub

        <WorkItem(544106, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544106")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneAfterRemoveHandlerAddressOfIdentifierTest()
            VerifyRecommendationsMissing(<MethodBody>RemoveHandler System.Console.CancelKeyPress, AddressOf Goo |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneAfterNamespaceAliasInImportsTest()
            Dim code =
            <File>
Imports S |
</File>

            VerifyRecommendationsMissing(code, s_expectedKeywords)
        End Sub

        <WorkItem(546505, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546505")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoCrashInVariableDeclarationTest()
            VerifyRecommendationsMissing(<MethodBody>Dim x As New TypeInfo(New |)</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending), WorkItem(544278, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544278")>
        Public Sub NoneAfterMidStatementTest()
            VerifyRecommendationsMissing(<MethodBody>Mid(s, 1, 1) |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending), WorkItem(544576, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544576")>
        Public Sub NoneAfterExternalMethodDeclarationTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Declare Sub S Lib "L" Alias "A" |</ClassDeclaration>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending), WorkItem(545988, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545988")>
        Public Sub NoneAfterNamedArgumentTest()
            VerifyRecommendationsMissing(<MethodBody>Goo(f:=|</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending), WorkItem(546659, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546659")>
        Public Sub NoneInUsingStatementTest()
            VerifyRecommendationsMissing(<MethodBody>Using Goo |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending), WorkItem(531329, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531329")>
        Public Sub NoneInForStatementTest()
            VerifyRecommendationsMissing(<MethodBody>For i = 1 |</MethodBody>, s_expectedKeywords)
        End Sub

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterEolTest()
            VerifyRecommendationsMissing(
<MethodBody>test = "F" 
|</MethodBody>, s_expectedKeywords)
        End Sub

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterExplicitLineContinuationTest()
            VerifyRecommendationsContain(
<MethodBody>test = "F" _
|</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterExplicitLineContinuationTestCommentsAfterLineContinuation()
            VerifyRecommendationsContain(
<MethodBody>test = "F" _ ' Test
|</MethodBody>, s_expectedKeywords)
        End Sub

        <WorkItem(975804, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/975804")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterIncompleteObjectCreationTest()
            VerifyRecommendationsMissing(
<MethodBody>Dim x = new Goo.|
</MethodBody>, s_expectedKeywords)
        End Sub
    End Class
End Namespace
