' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class AsKeywordRecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsInAggregateClause1Test() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = From x In {1, 2, 3} Aggregate x |</MethodBody>, "As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsInAggregateClause2Test() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = From x In {1, 2, 3} Aggregate x | As Type1 In collection, element2 |</MethodBody>, "As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsInConst1Test() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Const foo |</ClassDeclaration>, "As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsInConst2Test() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Const foo As Integer = 42, bar |</ClassDeclaration>, "As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsInExternalMethodSub1Test() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Declare Sub foo Lib "foo.dll" (x |</ClassDeclaration>, "As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsInExternalMethodSub2Test() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Declare Sub foo Lib "foo.dll" (x As Integer, y |</ClassDeclaration>, "As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsNotInExternalMethodSubReturnTypeTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Declare Sub foo Lib "foo.dll" (x As Integer) |</ClassDeclaration>, "As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsInExternalMethodFunction1Test() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Declare Function foo Lib "foo.dll" (x |</ClassDeclaration>, "As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsInExternalMethodFunction2Test() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Declare Function foo Lib "foo.dll" (x As Integer, y |</ClassDeclaration>, "As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsInExternalMethodFunctionReturnTypeTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Declare Function foo Lib "foo.dll" (x As Integer) |</ClassDeclaration>, "As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsInDelegateSub1Test() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Delegate Sub foo (x |</ClassDeclaration>, "As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsInDelegateSub2Test() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Delegate Sub foo (x As Integer, y |</ClassDeclaration>, "As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsNotInDelegateSubReturnTypeTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Delegate Sub foo (x As Integer) |</ClassDeclaration>, "As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsInDelegateFunction1Test() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Delegate Function foo (x |</ClassDeclaration>, "As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsInDelegateFunction2Test() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Delegate Function foo (x As Integer, y |</ClassDeclaration>, "As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsInDelegateFunctionReturnTypeTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Delegate Function foo (x As Integer) |</ClassDeclaration>, "As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsInDim1Test() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Dim x |</ClassDeclaration>, "As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsInDim2Test() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Dim x As Integer, y |</ClassDeclaration>, "As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsInEnumTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Enum Foo |</ClassDeclaration>, "As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsInAddHandlerTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>
Custom Event Foo As Action
AddHandler(value |</ClassDeclaration>, "As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsInRemoveHandlerTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>
Custom Event Foo As Action
RemoveHandler(value |</ClassDeclaration>, "As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsInForLoopTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>For x |</MethodBody>, "As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsInForLoopWithTypeCharacterTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>For x% |</MethodBody>, "As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsInForEachLoopTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>For Each x |</MethodBody>, "As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsInFromClause1Test() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = From x |</MethodBody>, "As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsInFromClause2Test() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = From x As Integer in collection1, y |</MethodBody>, "As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsInFunctionArguments1Test() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Function Foo(x |</ClassDeclaration>, "As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsInFunctionArguments2Test() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Function Foo(x As Integer, y |</ClassDeclaration>, "As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsNotInFunctionArgumentsWithTypeCharacterTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Function Foo(x% |</ClassDeclaration>, "As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsInFunctionReturnValueTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Function Foo(x As Integer) |</ClassDeclaration>, "As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsInFunctionLambdaArguments1Test() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Dim x = Function(x |</ClassDeclaration>, "As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsInFunctionLambdaArguments2Test() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Dim x = Function(x As Integer, y |</ClassDeclaration>, "As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsInFunctionLambdaReturnValueTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Dim x = Function(x As Integer) |</ClassDeclaration>, "As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsInGroupJoinTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Dim x = From i In {1, 2, 3} Group Join x |</ClassDeclaration>, "As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsInOperatorArguments1Test() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Public Shared Operator +(x |</ClassDeclaration>, "As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsInOperatorArguments2Test() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Public Shared Operator +(x As Integer, y |</ClassDeclaration>, "As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsInOperatorReturnValueTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Public Shared Operator +(x As Integer, y As Integer) |</ClassDeclaration>, "As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsInPropertyArguments1Test() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Public Property Foo(x |</ClassDeclaration>, "As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsInPropertyArguments2Test() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Public Property Foo(x As Integer, y |</ClassDeclaration>, "As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsInPropertyTypeTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Public Property Foo(x As Integer, y As Integer) |</ClassDeclaration>, "As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsInPropertySetArgumentTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>
Public Property Foo(x As Integer, y As Integer) 
    Set(value |</ClassDeclaration>, "As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsInSubArguments1Test() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Sub Foo(x |</ClassDeclaration>, "As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsInSubArguments2Test() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Sub Foo(x As Integer, y |</ClassDeclaration>, "As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsNotInSubReturnValueTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Sub Foo(x As Integer) |</ClassDeclaration>, "As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsInSubLambdaArguments1Test() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Dim x = Sub(x |</ClassDeclaration>, "As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsInSubLambdaArguments2Test() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Dim x = Sub(x As Integer, y |</ClassDeclaration>, "As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsNotInSubLambdaReturnValueTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Dim x = Sub(x As Integer) |</ClassDeclaration>, "As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsInCatchBlockTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Try
Catch foo |</MethodBody>, "As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsInEventDeclarationTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Event Foo |</ClassDeclaration>, "As")
        End Function

        <WorkItem(543118)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsAfterLetIdentifierTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>From i1 In New Integer() {4, 5} Let i2  |</MethodBody>, "As")
        End Function

        <WorkItem(543637)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsInGenericTypeParameterListTest() As Task
            Dim code =
<File>
Module Module1
    Sub Foo(Of T |
    End Sub
End Module
</File>

            Await VerifyRecommendationsContainAsync(code, "As")
        End Function

        <WorkItem(543637)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoAsInGenericTypeArgumentListTest() As Task
            Dim code =
<File>
Module Module1
    Sub Foo(Of T)
        Foo(Of T |
    End Sub
End Module
</File>

            Await VerifyRecommendationsMissingAsync(code, "As")
        End Function

        <WorkItem(544192)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsAfterPropertyNameTest() As Task
            Dim code =
<File>
Class C
    Public Property P |
End Class
</File>

            Await VerifyRecommendationsContainAsync(code, "As")
        End Function

        <WorkItem(544192)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoAsAfterPropertyOpenParenTest() As Task
            Dim code =
<File>
Class C
    Public Property P( |
End Class
</File>

            Await VerifyRecommendationsMissingAsync(code, "As")
        End Function

        <WorkItem(544192)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsAfterPropertyCloseParenTest() As Task
            Dim code =
<File>
Class C
    Public Property P() |
End Class
</File>

            Await VerifyRecommendationsContainAsync(code, "As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsAfterFunctionNameTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Function Foo |</ClassDeclaration>, "As")
        End Function

        <WorkItem(530387)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoAsAfterSubNameTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Sub Foo |</ClassDeclaration>, "As")
        End Function

        <WorkItem(530387)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoAsAfterSubNameWithParensTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Sub Foo() |</ClassDeclaration>, "As")
        End Function

        <WorkItem(530387)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoAsAfterSubNameWithBodyTest() As Task
            Dim code =
<File>
Class C
    Sub Foo |
    End Sub
End Class
</File>
            Await VerifyRecommendationsMissingAsync(code, "As")
        End Function

        <WorkItem(530387)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoAsAfterSubNameWithBodyAndParametersTest() As Task
            Dim code =
<File>
Class C
    Sub Foo(x As String) |
    End Sub
End Class
</File>
            Await VerifyRecommendationsMissingAsync(code, "As")
        End Function

        <WorkItem(546659)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsInUsingBlockTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Using Foo |</MethodBody>, "As")
        End Function

        <WorkItem(530953)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoAsAfterEolTest() As Task
            Await VerifyRecommendationsMissingAsync(
<MethodBody>
Dim Foo 
| </MethodBody>,
                "As")
        End Function

        <WorkItem(530953)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoAsAfterColonTest() As Task
            Await VerifyRecommendationsMissingAsync(
<MethodBody>
Dim Foo : | 
</MethodBody>,
                "As")
        End Function

        <WorkItem(530953)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AsAfterExplicitLineContinuationTest() As Task
            Await VerifyRecommendationsContainAsync(
<MethodBody>
Dim Foo _
| </MethodBody>,
                "As")
        End Function

        <WorkItem(547254)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterPublicAsyncTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Public Async |</ClassDeclaration>, "As")
        End Function
    End Class
End Namespace
