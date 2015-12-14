' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    Public Class BinaryOperatorKeywordRecommenderTests
        Private Shared ReadOnly s_expectedKeywords As String() = BinaryOperatorKeywordRecommender.KeywordList.Select(Function(k) k.Keyword).ToArray()

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInClassDeclaration()
            VerifyRecommendationsMissing(<ClassDeclaration>|</ClassDeclaration>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInMethodDeclaration()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllAfterLocalDeclarationNumericLiteralInitializer()
            VerifyRecommendationsContain(<MethodBody>Dim i As Integer = 1 |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllAfterNumberInAssignment()
            VerifyRecommendationsContain(<MethodBody>x = 1 |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllAfterStringInAssignment()
            VerifyRecommendationsContain(<MethodBody>x = "asdf" |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllAfterObjectCreationInAssignment()
            VerifyRecommendationsContain(<MethodBody>x = New Object() |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneAfterObjectCreationInDeclaration()
            VerifyRecommendationsMissing(<MethodBody>Dim x = New Object |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneAfterObjectCreationWithParensInDeclaration()
            VerifyRecommendationsMissing(<MethodBody>Dim x = New Object() |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneAfterAsNewInDeclaration()
            VerifyRecommendationsMissing(<MethodBody>Dim x As New Object |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneAfterAsNewWithParensInDeclaration()
            VerifyRecommendationsMissing(<MethodBody>Dim x As New Object() |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllAfterMethodCallInAsNewClause()
            VerifyRecommendationsContain(<MethodBody>Dim x As New Object(Foo() |)</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllAfterPropertyAccessInAssignment()
            VerifyRecommendationsContain(<MethodBody>x = Foo.Bar |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllAfterMethodCallInAssignment()
            VerifyRecommendationsContain(<MethodBody>x = Foo.Bar() |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneAfterDotInImports()
            VerifyRecommendationsMissing(<File>Imports System.|</File>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInSubLambdaParameterList()
            VerifyRecommendationsMissing(<MethodBody>Dim x = Sub(x |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInFunctionLambdaParameterList()
            VerifyRecommendationsMissing(<MethodBody>Dim x = Function(x |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInQueryVariableList()
            VerifyRecommendationsMissing(<MethodBody>Dim x = From y |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInQueryVariableList2()
            VerifyRecommendationsMissing(<MethodBody>Dim x = From y In {1, 2, 3} Let z |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllInSubLambdaBody()
            VerifyRecommendationsContain(<MethodBody>Dim x = Sub(x As Integer) x |</MethodBody>, s_expectedKeywords)
        End Sub

        <WorkItem(541354)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterStringLiteral()
            VerifyRecommendationsContain(<MethodBody>test = "F" |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllInFunctionLambdaBody()
            VerifyRecommendationsContain(<MethodBody>Dim x = Function(x As Integer) x |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInObjectMemberInitializer1()
            VerifyRecommendationsMissing(<MethodBody>Dim y = New foo() With {|</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInObjectMemberInitializer2()
            VerifyRecommendationsMissing(<MethodBody>Dim y = New foo() With {.|</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInObjectMemberInitializer3()
            VerifyRecommendationsMissing(<MethodBody>Dim y = New foo() With {.x|</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInObjectMemberInitializer4()
            VerifyRecommendationsMissing(<MethodBody>Dim y = New foo() With {.x |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInCatchStatement1()
            VerifyRecommendationsMissing(<MethodBody>
                                             Try
                                             Catch ex |
                                         </MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInCatchStatement2()
            VerifyRecommendationsMissing(<MethodBody>
                                             Try
                                             Catch ex As Exception |
                                         </MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInDimArrayBounds1()
            VerifyRecommendationsMissing(<MethodBody>Dim i(0 |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllInDimArrayBounds2()
            VerifyRecommendationsContain(<MethodBody>Dim i(0 To 4 |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInReDimArrayBounds1()
            VerifyRecommendationsMissing(<MethodBody>ReDim i(0 |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllInReDimArrayBounds2()
            VerifyRecommendationsContain(<MethodBody>ReDim i(0 To 4 |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneAfterQueryFrom()
            VerifyRecommendationsMissing(<MethodBody>Dim query = From |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneAfterQueryAggregate()
            VerifyRecommendationsMissing(<MethodBody>Dim query = Aggregate |</MethodBody>, s_expectedKeywords)
        End Sub

        <WorkItem(543637)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInTypeArgumentList()
            Dim code =
            <File>
Module M
    Sub Foo(Of T As Class)()
        Foo(Of T |
    End Sub
End Module
</File>

            VerifyRecommendationsMissing(code, s_expectedKeywords)
        End Sub

        <WorkItem(544106)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneAfterAddHandlerEventName()
            VerifyRecommendationsMissing(<MethodBody>AddHandler System.Console.CancelKeyPress |</MethodBody>, s_expectedKeywords)
        End Sub

        <WorkItem(544106)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneAfterAddHandlerIdentifier()
            VerifyRecommendationsContain(<MethodBody>AddHandler System.Console.CancelKeyPress, Foo |</MethodBody>, s_expectedKeywords)
        End Sub

        <WorkItem(544106)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneAfterAddHandlerAddressOfIdentifier()
            VerifyRecommendationsMissing(<MethodBody>AddHandler System.Console.CancelKeyPress, AddressOf Foo |</MethodBody>, s_expectedKeywords)
        End Sub

        <WorkItem(544106)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneAfterRemoveHandlerEventName()
            VerifyRecommendationsMissing(<MethodBody>RemoveHandler System.Console.CancelKeyPress |</MethodBody>, s_expectedKeywords)
        End Sub

        <WorkItem(544106)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneAfterRemoveHandlerIdentifier()
            VerifyRecommendationsContain(<MethodBody>RemoveHandler System.Console.CancelKeyPress, Foo |</MethodBody>, s_expectedKeywords)
        End Sub

        <WorkItem(544106)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneAfterRemoveHandlerAddressOfIdentifier()
            VerifyRecommendationsMissing(<MethodBody>RemoveHandler System.Console.CancelKeyPress, AddressOf Foo |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneAfterNamespaceAliasInImports()
            Dim code =
            <File>
Imports S |
</File>

            VerifyRecommendationsMissing(code, s_expectedKeywords)
        End Sub

        <WorkItem(546505)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoCrashInVariableDeclaration()
            VerifyRecommendationsMissing(<MethodBody>Dim x As New TypeInfo(New |)</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending), WorkItem(544278)>
        Public Sub NoneAfterMidStatement()
            VerifyRecommendationsMissing(<MethodBody>Mid(s, 1, 1) |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending), WorkItem(544576)>
        Public Sub NoneAfterExternalMethodDeclaration()
            VerifyRecommendationsMissing(<ClassDeclaration>Declare Sub S Lib "L" Alias "A" |</ClassDeclaration>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending), WorkItem(545988)>
        Public Sub NoneAfterNamedArgument()
            VerifyRecommendationsMissing(<MethodBody>Foo(f:=|</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending), WorkItem(546659)>
        Public Sub NoneInUsingStatement()
            VerifyRecommendationsMissing(<MethodBody>Using Foo |</MethodBody>, s_expectedKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending), WorkItem(531329)>
        Public Sub NoneInForStatement()
            VerifyRecommendationsMissing(<MethodBody>For i = 1 |</MethodBody>, s_expectedKeywords)
        End Sub

        <WorkItem(530953)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterEol()
            VerifyRecommendationsMissing(
<MethodBody>test = "F" 
|</MethodBody>, s_expectedKeywords)
        End Sub

        <WorkItem(530953)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterExplicitLineContinuation()
            VerifyRecommendationsContain(
<MethodBody>test = "F" _
|</MethodBody>, s_expectedKeywords)
        End Sub

        <WorkItem(975804)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterIncompleteObjectCreation()
            VerifyRecommendationsMissing(
<MethodBody>Dim x = new Foo.|
</MethodBody>, s_expectedKeywords)
        End Sub
    End Class
End Namespace
