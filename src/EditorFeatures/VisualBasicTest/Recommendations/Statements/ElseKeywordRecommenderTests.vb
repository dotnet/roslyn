' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class ElseKeywordRecommenderTests
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ElseNotInMethodBody()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Else")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ElseInMultiLineIf()
            VerifyRecommendationsContain(<MethodBody>If True Then
|
End If</MethodBody>, "Else")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ElseInMultiLineElseIf1()
            VerifyRecommendationsContain(<MethodBody>If True Then
ElseIf True Then
|
End If</MethodBody>, "Else")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ElseInMultiLineElseIf2()
            VerifyRecommendationsContain(<MethodBody>If True Then
Else If True Then
|
End If</MethodBody>, "Else")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ElseNotInMultiLineElse()
            VerifyRecommendationsMissing(<MethodBody>If True Then
Else 
|
End If</MethodBody>, "Else")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SingleLineIfElseAfterInvocation()
            VerifyRecommendationsContain(<MethodBody>If True Then System.Console.Write("Foo") |</MethodBody>, "Else")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SingleLineIfElseAfterExpression()
            VerifyRecommendationsContain(<MethodBody>If True Then q = q + 3 |</MethodBody>, "Else")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SingleLineIfElseAfterDeclaration()
            VerifyRecommendationsContain(<MethodBody>If True Then Dim q = 3 |</MethodBody>, "Else")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SingleLineIfElseAfterStop()
            VerifyRecommendationsContain(<MethodBody>If True Then Stop |</MethodBody>, "Else")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SingleLineIfElseAfterEnd()
            VerifyRecommendationsContain(<MethodBody>If True Then End |</MethodBody>, "Else")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SingleLineIfElseAfterReDim()
            VerifyRecommendationsContain(<MethodBody>If True Then ReDim array(1, 1, 1) |</MethodBody>, "Else")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SingleLineIfElseAfterErase()
            VerifyRecommendationsContain(<MethodBody>If True Then Erase foo, quux |</MethodBody>, "Else")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SingleLineIfElseAfterError()
            VerifyRecommendationsContain(<MethodBody>If True Then Error 23 |</MethodBody>, "Else")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SingleLineIfElseAfterExit()
            VerifyRecommendationsContain(<MethodBody>If True Then Exit Do |</MethodBody>, "Else")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SingleLineIfElseAfterGoTo()
            VerifyRecommendationsContain(<MethodBody>If True Then GoTo foo |</MethodBody>, "Else")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SingleLineIfElseAfterResume()
            VerifyRecommendationsContain(<MethodBody>If True Then Resume |</MethodBody>, "Else")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SingleLineIfElseAfterResumeNext()
            VerifyRecommendationsContain(<MethodBody>If True Then Resume Next |</MethodBody>, "Else")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SingleLineIfElseAfterContinue()
            VerifyRecommendationsContain(<MethodBody>If True Then Continue Do |</MethodBody>, "Else")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SingleLineIfElseAfterReturn()
            VerifyRecommendationsContain(<MethodBody>If True Then Return |</MethodBody>, "Else")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SingleLineIfElseAfterReturnExpressionInPropertyGet()
            VerifyRecommendationsContain(
                <ClassDeclaration>
                    ReadOnly Property Foo As Integer
                        Get
                            If True Then Return |
                </ClassDeclaration>, "Else")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SingleLineIfElseAfterReturnExpression()
            VerifyRecommendationsContain(<ClassDeclaration>Function Foo as String 
                If True Then Return foo |</ClassDeclaration>, "Else")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SingleLineIfElseAfterThrow()
            VerifyRecommendationsContain(<MethodBody>Try
                If True Then Throw |
                Catch</MethodBody>, "Else")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SingleLineIfElseAfterThrowExpression()
            VerifyRecommendationsContain(<MethodBody>Try
            If True Then Throw foo |
            Catch</MethodBody>, "Else")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SingleLineIfElseAfterAddHandler()
            VerifyRecommendationsContain(<MethodBody>If True Then AddHandler Obj.Ev_Event, AddressOf EventHandler |</MethodBody>, "Else")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SingleLineIfElseAfterRaiseEvent()
            VerifyRecommendationsContain(<MethodBody>If True Then RaiseEvent Ev_Event() |</MethodBody>, "Else")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SingleLineIfElseAfterColonSeparatedStatements()
            VerifyRecommendationsContain(<MethodBody>If True Then Console.WriteLine("foo") : Console.WriteLine("bar")  |</MethodBody>, "Else")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ElseNotInSingleLineIf1()
            VerifyRecommendationsMissing(<MethodBody>If True Then |</MethodBody>, "Else")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ElseNotInSingleLineIf2()
            VerifyRecommendationsMissing(<MethodBody>If True Then Stop Else |</MethodBody>, "Else")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ElseNotSingleLineIfNoStatements()
            VerifyRecommendationsMissing(<MethodBody>If True Then Else |</MethodBody>, "Else")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ElseAfterCase()
            VerifyRecommendationsContain(
<MethodBody>
Select Case x
    Case |
</MethodBody>, "Else")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoElseAfterCaseIfExists()
            VerifyRecommendationsMissing(
<MethodBody>
Select Case x
    Case Else End
    Case |
</MethodBody>, "Else")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoElseAfterCaseIfNotLastCase()
            VerifyRecommendationsMissing(
<MethodBody>
Select Case x
    Case |
    Case 1
</MethodBody>, "Else")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ElseWithinIfWithinElse()
            VerifyRecommendationsContain(
<MethodBody>
If True Then

Else
    If False Then
    |
    End If
End If
</MethodBody>, "Else")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ElseWithinElseIfWithinElse()
            VerifyRecommendationsContain(
<MethodBody>
If True Then

Else
    If False Then
    ElseIf True
        |
    End If
End If
</MethodBody>, "Else")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ElseNotWithinDo()
            VerifyRecommendationsMissing(
<MethodBody>
If True Then
Do While True
    |
End While
End If
</MethodBody>, "Else")
        End Sub
    End Class

End Namespace
