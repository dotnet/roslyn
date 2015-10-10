' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class AsKeywordRecommenderTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInAggregateClause1()
            VerifyRecommendationsContain(<MethodBody>Dim x = From x In {1, 2, 3} Aggregate x |</MethodBody>, "As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInAggregateClause2()
            VerifyRecommendationsContain(<MethodBody>Dim x = From x In {1, 2, 3} Aggregate x | As Type1 In collection, element2 |</MethodBody>, "As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInConst1()
            VerifyRecommendationsContain(<ClassDeclaration>Const foo |</ClassDeclaration>, "As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInConst2()
            VerifyRecommendationsContain(<ClassDeclaration>Const foo As Integer = 42, bar |</ClassDeclaration>, "As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInExternalMethodSub1()
            VerifyRecommendationsContain(<ClassDeclaration>Declare Sub foo Lib "foo.dll" (x |</ClassDeclaration>, "As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInExternalMethodSub2()
            VerifyRecommendationsContain(<ClassDeclaration>Declare Sub foo Lib "foo.dll" (x As Integer, y |</ClassDeclaration>, "As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsNotInExternalMethodSubReturnType()
            VerifyRecommendationsMissing(<ClassDeclaration>Declare Sub foo Lib "foo.dll" (x As Integer) |</ClassDeclaration>, "As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInExternalMethodFunction1()
            VerifyRecommendationsContain(<ClassDeclaration>Declare Function foo Lib "foo.dll" (x |</ClassDeclaration>, "As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInExternalMethodFunction2()
            VerifyRecommendationsContain(<ClassDeclaration>Declare Function foo Lib "foo.dll" (x As Integer, y |</ClassDeclaration>, "As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInExternalMethodFunctionReturnType()
            VerifyRecommendationsContain(<ClassDeclaration>Declare Function foo Lib "foo.dll" (x As Integer) |</ClassDeclaration>, "As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInDelegateSub1()
            VerifyRecommendationsContain(<ClassDeclaration>Delegate Sub foo (x |</ClassDeclaration>, "As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInDelegateSub2()
            VerifyRecommendationsContain(<ClassDeclaration>Delegate Sub foo (x As Integer, y |</ClassDeclaration>, "As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsNotInDelegateSubReturnType()
            VerifyRecommendationsMissing(<ClassDeclaration>Delegate Sub foo (x As Integer) |</ClassDeclaration>, "As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInDelegateFunction1()
            VerifyRecommendationsContain(<ClassDeclaration>Delegate Function foo (x |</ClassDeclaration>, "As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInDelegateFunction2()
            VerifyRecommendationsContain(<ClassDeclaration>Delegate Function foo (x As Integer, y |</ClassDeclaration>, "As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInDelegateFunctionReturnType()
            VerifyRecommendationsContain(<ClassDeclaration>Delegate Function foo (x As Integer) |</ClassDeclaration>, "As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInDim1()
            VerifyRecommendationsContain(<ClassDeclaration>Dim x |</ClassDeclaration>, "As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInDim2()
            VerifyRecommendationsContain(<ClassDeclaration>Dim x As Integer, y |</ClassDeclaration>, "As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInEnum()
            VerifyRecommendationsContain(<ClassDeclaration>Enum Foo |</ClassDeclaration>, "As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInAddHandler()
            VerifyRecommendationsContain(<ClassDeclaration>
Custom Event Foo As Action
AddHandler(value |</ClassDeclaration>, "As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInRemoveHandler()
            VerifyRecommendationsContain(<ClassDeclaration>
Custom Event Foo As Action
RemoveHandler(value |</ClassDeclaration>, "As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInForLoop()
            VerifyRecommendationsContain(<MethodBody>For x |</MethodBody>, "As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInForLoopWithTypeCharacter()
            VerifyRecommendationsMissing(<MethodBody>For x% |</MethodBody>, "As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInForEachLoop()
            VerifyRecommendationsContain(<MethodBody>For Each x |</MethodBody>, "As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInFromClause1()
            VerifyRecommendationsContain(<MethodBody>Dim x = From x |</MethodBody>, "As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInFromClause2()
            VerifyRecommendationsContain(<MethodBody>Dim x = From x As Integer in collection1, y |</MethodBody>, "As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInFunctionArguments1()
            VerifyRecommendationsContain(<ClassDeclaration>Function Foo(x |</ClassDeclaration>, "As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInFunctionArguments2()
            VerifyRecommendationsContain(<ClassDeclaration>Function Foo(x As Integer, y |</ClassDeclaration>, "As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsNotInFunctionArgumentsWithTypeCharacter()
            VerifyRecommendationsMissing(<ClassDeclaration>Function Foo(x% |</ClassDeclaration>, "As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInFunctionReturnValue()
            VerifyRecommendationsContain(<ClassDeclaration>Function Foo(x As Integer) |</ClassDeclaration>, "As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInFunctionLambdaArguments1()
            VerifyRecommendationsContain(<ClassDeclaration>Dim x = Function(x |</ClassDeclaration>, "As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInFunctionLambdaArguments2()
            VerifyRecommendationsContain(<ClassDeclaration>Dim x = Function(x As Integer, y |</ClassDeclaration>, "As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInFunctionLambdaReturnValue()
            VerifyRecommendationsContain(<ClassDeclaration>Dim x = Function(x As Integer) |</ClassDeclaration>, "As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInGroupJoin()
            VerifyRecommendationsContain(<ClassDeclaration>Dim x = From i In {1, 2, 3} Group Join x |</ClassDeclaration>, "As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInOperatorArguments1()
            VerifyRecommendationsContain(<ClassDeclaration>Public Shared Operator +(x |</ClassDeclaration>, "As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInOperatorArguments2()
            VerifyRecommendationsContain(<ClassDeclaration>Public Shared Operator +(x As Integer, y |</ClassDeclaration>, "As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInOperatorReturnValue()
            VerifyRecommendationsContain(<ClassDeclaration>Public Shared Operator +(x As Integer, y As Integer) |</ClassDeclaration>, "As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInPropertyArguments1()
            VerifyRecommendationsContain(<ClassDeclaration>Public Property Foo(x |</ClassDeclaration>, "As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInPropertyArguments2()
            VerifyRecommendationsContain(<ClassDeclaration>Public Property Foo(x As Integer, y |</ClassDeclaration>, "As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInPropertyType()
            VerifyRecommendationsContain(<ClassDeclaration>Public Property Foo(x As Integer, y As Integer) |</ClassDeclaration>, "As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInPropertySetArgument()
            VerifyRecommendationsContain(<ClassDeclaration>
Public Property Foo(x As Integer, y As Integer) 
    Set(value |</ClassDeclaration>, "As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInSubArguments1()
            VerifyRecommendationsContain(<ClassDeclaration>Sub Foo(x |</ClassDeclaration>, "As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInSubArguments2()
            VerifyRecommendationsContain(<ClassDeclaration>Sub Foo(x As Integer, y |</ClassDeclaration>, "As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsNotInSubReturnValue()
            VerifyRecommendationsMissing(<ClassDeclaration>Sub Foo(x As Integer) |</ClassDeclaration>, "As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInSubLambdaArguments1()
            VerifyRecommendationsContain(<ClassDeclaration>Dim x = Sub(x |</ClassDeclaration>, "As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInSubLambdaArguments2()
            VerifyRecommendationsContain(<ClassDeclaration>Dim x = Sub(x As Integer, y |</ClassDeclaration>, "As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsNotInSubLambdaReturnValue()
            VerifyRecommendationsMissing(<ClassDeclaration>Dim x = Sub(x As Integer) |</ClassDeclaration>, "As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInCatchBlock()
            VerifyRecommendationsContain(<MethodBody>
Try
Catch foo |</MethodBody>, "As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInEventDeclaration()
            VerifyRecommendationsContain(<ClassDeclaration>Event Foo |</ClassDeclaration>, "As")
        End Sub

        <WorkItem(543118)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsAfterLetIdentifier()
            VerifyRecommendationsContain(<MethodBody>From i1 In New Integer() {4, 5} Let i2  |</MethodBody>, "As")
        End Sub

        <WorkItem(543637)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInGenericTypeParameterList()
            Dim code =
<File>
Module Module1
    Sub Foo(Of T |
    End Sub
End Module
</File>

            VerifyRecommendationsContain(code, "As")
        End Sub

        <WorkItem(543637)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoAsInGenericTypeArgumentList()
            Dim code =
<File>
Module Module1
    Sub Foo(Of T)
        Foo(Of T |
    End Sub
End Module
</File>

            VerifyRecommendationsMissing(code, "As")
        End Sub

        <WorkItem(544192)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsAfterPropertyName()
            Dim code =
<File>
Class C
    Public Property P |
End Class
</File>

            VerifyRecommendationsContain(code, "As")
        End Sub

        <WorkItem(544192)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoAsAfterPropertyOpenParen()
            Dim code =
<File>
Class C
    Public Property P( |
End Class
</File>

            VerifyRecommendationsMissing(code, "As")
        End Sub

        <WorkItem(544192)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsAfterPropertyCloseParen()
            Dim code =
<File>
Class C
    Public Property P() |
End Class
</File>

            VerifyRecommendationsContain(code, "As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsAfterFunctionName()
            VerifyRecommendationsContain(<ClassDeclaration>Function Foo |</ClassDeclaration>, "As")
        End Sub

        <WorkItem(530387)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoAsAfterSubName()
            VerifyRecommendationsMissing(<ClassDeclaration>Sub Foo |</ClassDeclaration>, "As")
        End Sub

        <WorkItem(530387)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoAsAfterSubNameWithParens()
            VerifyRecommendationsMissing(<ClassDeclaration>Sub Foo() |</ClassDeclaration>, "As")
        End Sub

        <WorkItem(530387)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoAsAfterSubNameWithBody()
            Dim code =
<File>
Class C
    Sub Foo |
    End Sub
End Class
</File>
            VerifyRecommendationsMissing(code, "As")
        End Sub

        <WorkItem(530387)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoAsAfterSubNameWithBodyAndParameters()
            Dim code =
<File>
Class C
    Sub Foo(x As String) |
    End Sub
End Class
</File>
            VerifyRecommendationsMissing(code, "As")
        End Sub

        <WorkItem(546659)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsInUsingBlock()
            VerifyRecommendationsContain(<MethodBody>Using Foo |</MethodBody>, "As")
        End Sub

        <WorkItem(530953)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoAsAfterEol()
            VerifyRecommendationsMissing(
<MethodBody>
Dim Foo 
| </MethodBody>,
                "As")
        End Sub

        <WorkItem(530953)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoAsAfterColon()
            VerifyRecommendationsMissing(
<MethodBody>
Dim Foo : | 
</MethodBody>,
                "As")
        End Sub

        <WorkItem(530953)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AsAfterExplicitLineContinuation()
            VerifyRecommendationsContain(
<MethodBody>
Dim Foo _
| </MethodBody>,
                "As")
        End Sub

        <WorkItem(547254)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterPublicAsync()
            VerifyRecommendationsContain(<ClassDeclaration>Public Async |</ClassDeclaration>, "As")
        End Sub
    End Class
End Namespace
