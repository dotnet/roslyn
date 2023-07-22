' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class AsKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub AsInAggregateClause1Test()
            VerifyRecommendationsContain(<MethodBody>Dim x = From x In {1, 2, 3} Aggregate x |</MethodBody>, "As")
        End Sub

        <Fact>
        Public Sub AsInAggregateClause2Test()
            VerifyRecommendationsContain(<MethodBody>Dim x = From x In {1, 2, 3} Aggregate x | As Type1 In collection, element2 |</MethodBody>, "As")
        End Sub

        <Fact>
        Public Sub AsInConst1Test()
            VerifyRecommendationsContain(<ClassDeclaration>Const goo |</ClassDeclaration>, "As")
        End Sub

        <Fact>
        Public Sub AsInConst2Test()
            VerifyRecommendationsContain(<ClassDeclaration>Const goo As Integer = 42, bar |</ClassDeclaration>, "As")
        End Sub

        <Fact>
        Public Sub AsInExternalMethodSub1Test()
            VerifyRecommendationsContain(<ClassDeclaration>Declare Sub goo Lib "goo.dll" (x |</ClassDeclaration>, "As")
        End Sub

        <Fact>
        Public Sub AsInExternalMethodSub2Test()
            VerifyRecommendationsContain(<ClassDeclaration>Declare Sub goo Lib "goo.dll" (x As Integer, y |</ClassDeclaration>, "As")
        End Sub

        <Fact>
        Public Sub AsNotInExternalMethodSubReturnTypeTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Declare Sub goo Lib "goo.dll" (x As Integer) |</ClassDeclaration>, "As")
        End Sub

        <Fact>
        Public Sub AsInExternalMethodFunction1Test()
            VerifyRecommendationsContain(<ClassDeclaration>Declare Function goo Lib "goo.dll" (x |</ClassDeclaration>, "As")
        End Sub

        <Fact>
        Public Sub AsInExternalMethodFunction2Test()
            VerifyRecommendationsContain(<ClassDeclaration>Declare Function goo Lib "goo.dll" (x As Integer, y |</ClassDeclaration>, "As")
        End Sub

        <Fact>
        Public Sub AsInExternalMethodFunctionReturnTypeTest()
            VerifyRecommendationsContain(<ClassDeclaration>Declare Function goo Lib "goo.dll" (x As Integer) |</ClassDeclaration>, "As")
        End Sub

        <Fact>
        Public Sub AsInDelegateSub1Test()
            VerifyRecommendationsContain(<ClassDeclaration>Delegate Sub goo (x |</ClassDeclaration>, "As")
        End Sub

        <Fact>
        Public Sub AsInDelegateSub2Test()
            VerifyRecommendationsContain(<ClassDeclaration>Delegate Sub goo (x As Integer, y |</ClassDeclaration>, "As")
        End Sub

        <Fact>
        Public Sub AsNotInDelegateSubReturnTypeTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Delegate Sub goo (x As Integer) |</ClassDeclaration>, "As")
        End Sub

        <Fact>
        Public Sub AsInDelegateFunction1Test()
            VerifyRecommendationsContain(<ClassDeclaration>Delegate Function goo (x |</ClassDeclaration>, "As")
        End Sub

        <Fact>
        Public Sub AsInDelegateFunction2Test()
            VerifyRecommendationsContain(<ClassDeclaration>Delegate Function goo (x As Integer, y |</ClassDeclaration>, "As")
        End Sub

        <Fact>
        Public Sub AsInDelegateFunctionReturnTypeTest()
            VerifyRecommendationsContain(<ClassDeclaration>Delegate Function goo (x As Integer) |</ClassDeclaration>, "As")
        End Sub

        <Fact>
        Public Sub AsInDim1Test()
            VerifyRecommendationsContain(<ClassDeclaration>Dim x |</ClassDeclaration>, "As")
        End Sub

        <Fact>
        Public Sub AsInDim2Test()
            VerifyRecommendationsContain(<ClassDeclaration>Dim x As Integer, y |</ClassDeclaration>, "As")
        End Sub

        <Fact>
        Public Sub AsInEnumTest()
            VerifyRecommendationsContain(<ClassDeclaration>Enum Goo |</ClassDeclaration>, "As")
        End Sub

        <Fact>
        Public Sub AsInAddHandlerTest()
            VerifyRecommendationsContain(<ClassDeclaration>
Custom Event Goo As Action
AddHandler(value |</ClassDeclaration>, "As")
        End Sub

        <Fact>
        Public Sub AsInRemoveHandlerTest()
            VerifyRecommendationsContain(<ClassDeclaration>
Custom Event Goo As Action
RemoveHandler(value |</ClassDeclaration>, "As")
        End Sub

        <Fact>
        Public Sub AsInForLoopTest()
            VerifyRecommendationsContain(<MethodBody>For x |</MethodBody>, "As")
        End Sub

        <Fact>
        Public Sub AsInForLoopWithTypeCharacterTest()
            VerifyRecommendationsMissing(<MethodBody>For x% |</MethodBody>, "As")
        End Sub

        <Fact>
        Public Sub AsInForEachLoopTest()
            VerifyRecommendationsContain(<MethodBody>For Each x |</MethodBody>, "As")
        End Sub

        <Fact>
        Public Sub AsInFromClause1Test()
            VerifyRecommendationsContain(<MethodBody>Dim x = From x |</MethodBody>, "As")
        End Sub

        <Fact>
        Public Sub AsInFromClause2Test()
            VerifyRecommendationsContain(<MethodBody>Dim x = From x As Integer in collection1, y |</MethodBody>, "As")
        End Sub

        <Fact>
        Public Sub AsInFunctionArguments1Test()
            VerifyRecommendationsContain(<ClassDeclaration>Function Goo(x |</ClassDeclaration>, "As")
        End Sub

        <Fact>
        Public Sub AsInFunctionArguments2Test()
            VerifyRecommendationsContain(<ClassDeclaration>Function Goo(x As Integer, y |</ClassDeclaration>, "As")
        End Sub

        <Fact>
        Public Sub AsNotInFunctionArgumentsWithTypeCharacterTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Function Goo(x% |</ClassDeclaration>, "As")
        End Sub

        <Fact>
        Public Sub AsInFunctionReturnValueTest()
            VerifyRecommendationsContain(<ClassDeclaration>Function Goo(x As Integer) |</ClassDeclaration>, "As")
        End Sub

        <Fact>
        Public Sub AsInFunctionLambdaArguments1Test()
            VerifyRecommendationsContain(<ClassDeclaration>Dim x = Function(x |</ClassDeclaration>, "As")
        End Sub

        <Fact>
        Public Sub AsInFunctionLambdaArguments2Test()
            VerifyRecommendationsContain(<ClassDeclaration>Dim x = Function(x As Integer, y |</ClassDeclaration>, "As")
        End Sub

        <Fact>
        Public Sub AsInFunctionLambdaReturnValueTest()
            VerifyRecommendationsContain(<ClassDeclaration>Dim x = Function(x As Integer) |</ClassDeclaration>, "As")
        End Sub

        <Fact>
        Public Sub AsInGroupJoinTest()
            VerifyRecommendationsContain(<ClassDeclaration>Dim x = From i In {1, 2, 3} Group Join x |</ClassDeclaration>, "As")
        End Sub

        <Fact>
        Public Sub AsInOperatorArguments1Test()
            VerifyRecommendationsContain(<ClassDeclaration>Public Shared Operator +(x |</ClassDeclaration>, "As")
        End Sub

        <Fact>
        Public Sub AsInOperatorArguments2Test()
            VerifyRecommendationsContain(<ClassDeclaration>Public Shared Operator +(x As Integer, y |</ClassDeclaration>, "As")
        End Sub

        <Fact>
        Public Sub AsInOperatorReturnValueTest()
            VerifyRecommendationsContain(<ClassDeclaration>Public Shared Operator +(x As Integer, y As Integer) |</ClassDeclaration>, "As")
        End Sub

        <Fact>
        Public Sub AsInPropertyArguments1Test()
            VerifyRecommendationsContain(<ClassDeclaration>Public Property Goo(x |</ClassDeclaration>, "As")
        End Sub

        <Fact>
        Public Sub AsInPropertyArguments2Test()
            VerifyRecommendationsContain(<ClassDeclaration>Public Property Goo(x As Integer, y |</ClassDeclaration>, "As")
        End Sub

        <Fact>
        Public Sub AsInPropertyTypeTest()
            VerifyRecommendationsContain(<ClassDeclaration>Public Property Goo(x As Integer, y As Integer) |</ClassDeclaration>, "As")
        End Sub

        <Fact>
        Public Sub AsInPropertySetArgumentTest()
            VerifyRecommendationsContain(<ClassDeclaration>
Public Property Goo(x As Integer, y As Integer) 
    Set(value |</ClassDeclaration>, "As")
        End Sub

        <Fact>
        Public Sub AsInSubArguments1Test()
            VerifyRecommendationsContain(<ClassDeclaration>Sub Goo(x |</ClassDeclaration>, "As")
        End Sub

        <Fact>
        Public Sub AsInSubArguments2Test()
            VerifyRecommendationsContain(<ClassDeclaration>Sub Goo(x As Integer, y |</ClassDeclaration>, "As")
        End Sub

        <Fact>
        Public Sub AsNotInSubReturnValueTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Sub Goo(x As Integer) |</ClassDeclaration>, "As")
        End Sub

        <Fact>
        Public Sub AsInSubLambdaArguments1Test()
            VerifyRecommendationsContain(<ClassDeclaration>Dim x = Sub(x |</ClassDeclaration>, "As")
        End Sub

        <Fact>
        Public Sub AsInSubLambdaArguments2Test()
            VerifyRecommendationsContain(<ClassDeclaration>Dim x = Sub(x As Integer, y |</ClassDeclaration>, "As")
        End Sub

        <Fact>
        Public Sub AsNotInSubLambdaReturnValueTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Dim x = Sub(x As Integer) |</ClassDeclaration>, "As")
        End Sub

        <Fact>
        Public Sub AsInCatchBlockTest()
            VerifyRecommendationsContain(<MethodBody>
Try
Catch goo |</MethodBody>, "As")
        End Sub

        <Fact>
        Public Sub AsInEventDeclarationTest()
            VerifyRecommendationsContain(<ClassDeclaration>Event Goo |</ClassDeclaration>, "As")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543118")>
        Public Sub AsAfterLetIdentifierTest()
            VerifyRecommendationsContain(<MethodBody>From i1 In New Integer() {4, 5} Let i2  |</MethodBody>, "As")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543637")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543637")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544192")>
        Public Sub AsAfterPropertyNameTest()
            Dim code =
<File>
Class C
    Public Property P |
End Class
</File>

            VerifyRecommendationsContain(code, "As")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544192")>
        Public Sub NoAsAfterPropertyOpenParenTest()
            Dim code =
<File>
Class C
    Public Property P( |
End Class
</File>

            VerifyRecommendationsMissing(code, "As")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544192")>
        Public Sub AsAfterPropertyCloseParenTest()
            Dim code =
<File>
Class C
    Public Property P() |
End Class
</File>

            VerifyRecommendationsContain(code, "As")
        End Sub

        <Fact>
        Public Sub AsAfterFunctionNameTest()
            VerifyRecommendationsContain(<ClassDeclaration>Function Goo |</ClassDeclaration>, "As")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530387")>
        Public Sub NoAsAfterSubNameTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Sub Goo |</ClassDeclaration>, "As")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530387")>
        Public Sub NoAsAfterSubNameWithParensTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Sub Goo() |</ClassDeclaration>, "As")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530387")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530387")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546659")>
        Public Sub AsInUsingBlockTest()
            VerifyRecommendationsContain(<MethodBody>Using Goo |</MethodBody>, "As")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        Public Sub NoAsAfterEolTest()
            VerifyRecommendationsMissing(
<MethodBody>
Dim Goo 
| </MethodBody>,
                "As")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        Public Sub NoAsAfterColonTest()
            VerifyRecommendationsMissing(
<MethodBody>
Dim Goo : | 
</MethodBody>,
                "As")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        Public Sub AsAfterExplicitLineContinuationTest()
            VerifyRecommendationsContain(
<MethodBody>
Dim Goo _
| </MethodBody>,
                "As")
        End Sub

        <Fact>
        Public Sub AsAfterExplicitLineContinuationTestCommentsAfterLineContinuation()
            VerifyRecommendationsContain(
<MethodBody>
Dim Goo _ ' Test
| </MethodBody>,
                "As")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547254")>
        Public Sub AfterPublicAsyncTest()
            VerifyRecommendationsContain(<ClassDeclaration>Public Async |</ClassDeclaration>, "As")
        End Sub
    End Class
End Namespace
