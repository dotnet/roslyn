' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class AsKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInAggregateClause1Test()
            VerifyRecommendationsContain(<MethodBody>Dim x = From x In {1, 2, 3} Aggregate x |</MethodBody>, "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInAggregateClause2Test()
            VerifyRecommendationsContain(<MethodBody>Dim x = From x In {1, 2, 3} Aggregate x | As Type1 In collection, element2 |</MethodBody>, "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInConst1Test()
            VerifyRecommendationsContain(<ClassDeclaration>Const goo |</ClassDeclaration>, "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInConst2Test()
            VerifyRecommendationsContain(<ClassDeclaration>Const goo As Integer = 42, bar |</ClassDeclaration>, "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInExternalMethodSub1Test()
            VerifyRecommendationsContain(<ClassDeclaration>Declare Sub goo Lib "goo.dll" (x |</ClassDeclaration>, "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInExternalMethodSub2Test()
            VerifyRecommendationsContain(<ClassDeclaration>Declare Sub goo Lib "goo.dll" (x As Integer, y |</ClassDeclaration>, "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsNotInExternalMethodSubReturnTypeTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Declare Sub goo Lib "goo.dll" (x As Integer) |</ClassDeclaration>, "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInExternalMethodFunction1Test()
            VerifyRecommendationsContain(<ClassDeclaration>Declare Function goo Lib "goo.dll" (x |</ClassDeclaration>, "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInExternalMethodFunction2Test()
            VerifyRecommendationsContain(<ClassDeclaration>Declare Function goo Lib "goo.dll" (x As Integer, y |</ClassDeclaration>, "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInExternalMethodFunctionReturnTypeTest()
            VerifyRecommendationsContain(<ClassDeclaration>Declare Function goo Lib "goo.dll" (x As Integer) |</ClassDeclaration>, "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInDelegateSub1Test()
            VerifyRecommendationsContain(<ClassDeclaration>Delegate Sub goo (x |</ClassDeclaration>, "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInDelegateSub2Test()
            VerifyRecommendationsContain(<ClassDeclaration>Delegate Sub goo (x As Integer, y |</ClassDeclaration>, "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsNotInDelegateSubReturnTypeTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Delegate Sub goo (x As Integer) |</ClassDeclaration>, "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInDelegateFunction1Test()
            VerifyRecommendationsContain(<ClassDeclaration>Delegate Function goo (x |</ClassDeclaration>, "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInDelegateFunction2Test()
            VerifyRecommendationsContain(<ClassDeclaration>Delegate Function goo (x As Integer, y |</ClassDeclaration>, "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInDelegateFunctionReturnTypeTest()
            VerifyRecommendationsContain(<ClassDeclaration>Delegate Function goo (x As Integer) |</ClassDeclaration>, "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInDim1Test()
            VerifyRecommendationsContain(<ClassDeclaration>Dim x |</ClassDeclaration>, "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInDim2Test()
            VerifyRecommendationsContain(<ClassDeclaration>Dim x As Integer, y |</ClassDeclaration>, "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInEnumTest()
            VerifyRecommendationsContain(<ClassDeclaration>Enum Goo |</ClassDeclaration>, "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInAddHandlerTest()
            VerifyRecommendationsContain(<ClassDeclaration>
Custom Event Goo As Action
AddHandler(value |</ClassDeclaration>, "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInRemoveHandlerTest()
            VerifyRecommendationsContain(<ClassDeclaration>
Custom Event Goo As Action
RemoveHandler(value |</ClassDeclaration>, "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInForLoopTest()
            VerifyRecommendationsContain(<MethodBody>For x |</MethodBody>, "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInForLoopWithTypeCharacterTest()
            VerifyRecommendationsMissing(<MethodBody>For x% |</MethodBody>, "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInForEachLoopTest()
            VerifyRecommendationsContain(<MethodBody>For Each x |</MethodBody>, "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInFromClause1Test()
            VerifyRecommendationsContain(<MethodBody>Dim x = From x |</MethodBody>, "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInFromClause2Test()
            VerifyRecommendationsContain(<MethodBody>Dim x = From x As Integer in collection1, y |</MethodBody>, "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInFunctionArguments1Test()
            VerifyRecommendationsContain(<ClassDeclaration>Function Goo(x |</ClassDeclaration>, "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInFunctionArguments2Test()
            VerifyRecommendationsContain(<ClassDeclaration>Function Goo(x As Integer, y |</ClassDeclaration>, "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsNotInFunctionArgumentsWithTypeCharacterTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Function Goo(x% |</ClassDeclaration>, "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInFunctionReturnValueTest()
            VerifyRecommendationsContain(<ClassDeclaration>Function Goo(x As Integer) |</ClassDeclaration>, "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInFunctionLambdaArguments1Test()
            VerifyRecommendationsContain(<ClassDeclaration>Dim x = Function(x |</ClassDeclaration>, "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInFunctionLambdaArguments2Test()
            VerifyRecommendationsContain(<ClassDeclaration>Dim x = Function(x As Integer, y |</ClassDeclaration>, "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInFunctionLambdaReturnValueTest()
            VerifyRecommendationsContain(<ClassDeclaration>Dim x = Function(x As Integer) |</ClassDeclaration>, "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInGroupJoinTest()
            VerifyRecommendationsContain(<ClassDeclaration>Dim x = From i In {1, 2, 3} Group Join x |</ClassDeclaration>, "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInOperatorArguments1Test()
            VerifyRecommendationsContain(<ClassDeclaration>Public Shared Operator +(x |</ClassDeclaration>, "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInOperatorArguments2Test()
            VerifyRecommendationsContain(<ClassDeclaration>Public Shared Operator +(x As Integer, y |</ClassDeclaration>, "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInOperatorReturnValueTest()
            VerifyRecommendationsContain(<ClassDeclaration>Public Shared Operator +(x As Integer, y As Integer) |</ClassDeclaration>, "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInPropertyArguments1Test()
            VerifyRecommendationsContain(<ClassDeclaration>Public Property Goo(x |</ClassDeclaration>, "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInPropertyArguments2Test()
            VerifyRecommendationsContain(<ClassDeclaration>Public Property Goo(x As Integer, y |</ClassDeclaration>, "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInPropertyTypeTest()
            VerifyRecommendationsContain(<ClassDeclaration>Public Property Goo(x As Integer, y As Integer) |</ClassDeclaration>, "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInPropertySetArgumentTest()
            VerifyRecommendationsContain(<ClassDeclaration>
Public Property Goo(x As Integer, y As Integer) 
    Set(value |</ClassDeclaration>, "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInSubArguments1Test()
            VerifyRecommendationsContain(<ClassDeclaration>Sub Goo(x |</ClassDeclaration>, "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInSubArguments2Test()
            VerifyRecommendationsContain(<ClassDeclaration>Sub Goo(x As Integer, y |</ClassDeclaration>, "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsNotInSubReturnValueTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Sub Goo(x As Integer) |</ClassDeclaration>, "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInSubLambdaArguments1Test()
            VerifyRecommendationsContain(<ClassDeclaration>Dim x = Sub(x |</ClassDeclaration>, "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInSubLambdaArguments2Test()
            VerifyRecommendationsContain(<ClassDeclaration>Dim x = Sub(x As Integer, y |</ClassDeclaration>, "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsNotInSubLambdaReturnValueTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Dim x = Sub(x As Integer) |</ClassDeclaration>, "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInCatchBlockTest()
            VerifyRecommendationsContain(<MethodBody>
Try
Catch goo |</MethodBody>, "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInEventDeclarationTest()
            VerifyRecommendationsContain(<ClassDeclaration>Event Goo |</ClassDeclaration>, "As")
        End Sub

        <WorkItem(543118, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543118")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsAfterLetIdentifierTest()
            VerifyRecommendationsContain(<MethodBody>From i1 In New Integer() {4, 5} Let i2  |</MethodBody>, "As")
        End Sub

        <WorkItem(543637, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543637")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInGenericTypeParameterListTest()
            Dim code =
<File>
Module Module1
    Sub Goo(Of T |
    End Sub
End Module
</File>

            VerifyRecommendationsContain(code, "As")
        End Sub

        <WorkItem(543637, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543637")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoAsInGenericTypeArgumentListTest()
            Dim code =
<File>
Module Module1
    Sub Goo(Of T)
        Goo(Of T |
    End Sub
End Module
</File>

            VerifyRecommendationsMissing(code, "As")
        End Sub

        <WorkItem(544192, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544192")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsAfterPropertyNameTest()
            Dim code =
<File>
Class C
    Public Property P |
End Class
</File>

            VerifyRecommendationsContain(code, "As")
        End Sub

        <WorkItem(544192, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544192")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoAsAfterPropertyOpenParenTest()
            Dim code =
<File>
Class C
    Public Property P( |
End Class
</File>

            VerifyRecommendationsMissing(code, "As")
        End Sub

        <WorkItem(544192, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544192")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsAfterPropertyCloseParenTest()
            Dim code =
<File>
Class C
    Public Property P() |
End Class
</File>

            VerifyRecommendationsContain(code, "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsAfterFunctionNameTest()
            VerifyRecommendationsContain(<ClassDeclaration>Function Goo |</ClassDeclaration>, "As")
        End Sub

        <WorkItem(530387, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530387")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoAsAfterSubNameTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Sub Goo |</ClassDeclaration>, "As")
        End Sub

        <WorkItem(530387, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530387")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoAsAfterSubNameWithParensTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Sub Goo() |</ClassDeclaration>, "As")
        End Sub

        <WorkItem(530387, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530387")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoAsAfterSubNameWithBodyTest()
            Dim code =
<File>
Class C
    Sub Goo |
    End Sub
End Class
</File>
            VerifyRecommendationsMissing(code, "As")
        End Sub

        <WorkItem(530387, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530387")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoAsAfterSubNameWithBodyAndParametersTest()
            Dim code =
<File>
Class C
    Sub Goo(x As String) |
    End Sub
End Class
</File>
            VerifyRecommendationsMissing(code, "As")
        End Sub

        <WorkItem(546659, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546659")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInUsingBlockTest()
            VerifyRecommendationsContain(<MethodBody>Using Goo |</MethodBody>, "As")
        End Sub

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoAsAfterEolTest()
            VerifyRecommendationsMissing(
<MethodBody>
Dim Goo 
| </MethodBody>,
                "As")
        End Sub

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoAsAfterColonTest()
            VerifyRecommendationsMissing(
<MethodBody>
Dim Goo : | 
</MethodBody>,
                "As")
        End Sub

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsAfterExplicitLineContinuationTest()
            VerifyRecommendationsContain(
<MethodBody>
Dim Goo _
| </MethodBody>,
                "As")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsAfterExplicitLineContinuationTestCommentsAfterLineContinuation()
            VerifyRecommendationsContain(
<MethodBody>
Dim Goo _ ' Test
| </MethodBody>,
                "As")
        End Sub

        <WorkItem(547254, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547254")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterPublicAsyncTest()
            VerifyRecommendationsContain(<ClassDeclaration>Public Async |</ClassDeclaration>, "As")
        End Sub
    End Class
End Namespace
