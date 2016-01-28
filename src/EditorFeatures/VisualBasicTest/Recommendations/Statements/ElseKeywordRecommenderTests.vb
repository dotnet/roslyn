' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class ElseKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ElseNotInMethodBodyTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>|</MethodBody>, "Else")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ElseInMultiLineIfTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>If True Then
|
End If</MethodBody>, "Else")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ElseInMultiLineElseIf1Test() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>If True Then
ElseIf True Then
|
End If</MethodBody>, "Else")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ElseInMultiLineElseIf2Test() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>If True Then
Else If True Then
|
End If</MethodBody>, "Else")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ElseNotInMultiLineElseTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>If True Then
Else 
|
End If</MethodBody>, "Else")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SingleLineIfElseAfterInvocationTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>If True Then System.Console.Write("Foo") |</MethodBody>, "Else")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SingleLineIfElseAfterExpressionTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>If True Then q = q + 3 |</MethodBody>, "Else")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SingleLineIfElseAfterDeclarationTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>If True Then Dim q = 3 |</MethodBody>, "Else")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SingleLineIfElseAfterStopTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>If True Then Stop |</MethodBody>, "Else")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SingleLineIfElseAfterEndTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>If True Then End |</MethodBody>, "Else")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SingleLineIfElseAfterReDimTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>If True Then ReDim array(1, 1, 1) |</MethodBody>, "Else")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SingleLineIfElseAfterEraseTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>If True Then Erase foo, quux |</MethodBody>, "Else")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SingleLineIfElseAfterErrorTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>If True Then Error 23 |</MethodBody>, "Else")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SingleLineIfElseAfterExitTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>If True Then Exit Do |</MethodBody>, "Else")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SingleLineIfElseAfterGoToTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>If True Then GoTo foo |</MethodBody>, "Else")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SingleLineIfElseAfterResumeTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>If True Then Resume |</MethodBody>, "Else")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SingleLineIfElseAfterResumeNextTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>If True Then Resume Next |</MethodBody>, "Else")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SingleLineIfElseAfterContinueTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>If True Then Continue Do |</MethodBody>, "Else")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SingleLineIfElseAfterReturnTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>If True Then Return |</MethodBody>, "Else")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SingleLineIfElseAfterReturnExpressionInPropertyGetTest() As Task
            Await VerifyRecommendationsContainAsync(
                <ClassDeclaration>
                    ReadOnly Property Foo As Integer
                        Get
                            If True Then Return |
                </ClassDeclaration>, "Else")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SingleLineIfElseAfterReturnExpressionTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Function Foo as String 
                If True Then Return foo |</ClassDeclaration>, "Else")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SingleLineIfElseAfterThrowTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Try
                If True Then Throw |
                Catch</MethodBody>, "Else")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SingleLineIfElseAfterThrowExpressionTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Try
            If True Then Throw foo |
            Catch</MethodBody>, "Else")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SingleLineIfElseAfterAddHandlerTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>If True Then AddHandler Obj.Ev_Event, AddressOf EventHandler |</MethodBody>, "Else")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SingleLineIfElseAfterRaiseEventTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>If True Then RaiseEvent Ev_Event() |</MethodBody>, "Else")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SingleLineIfElseAfterColonSeparatedStatementsTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>If True Then Console.WriteLine("foo") : Console.WriteLine("bar")  |</MethodBody>, "Else")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ElseNotInSingleLineIf1Test() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>If True Then |</MethodBody>, "Else")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ElseNotInSingleLineIf2Test() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>If True Then Stop Else |</MethodBody>, "Else")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ElseNotSingleLineIfNoStatementsTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>If True Then Else |</MethodBody>, "Else")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ElseAfterCaseTest() As Task
            Await VerifyRecommendationsContainAsync(
<MethodBody>
Select Case x
    Case |
</MethodBody>, "Else")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoElseAfterCaseIfExistsTest() As Task
            Await VerifyRecommendationsMissingAsync(
<MethodBody>
Select Case x
    Case Else End
    Case |
</MethodBody>, "Else")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoElseAfterCaseIfNotLastCaseTest() As Task
            Await VerifyRecommendationsMissingAsync(
<MethodBody>
Select Case x
    Case |
    Case 1
</MethodBody>, "Else")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ElseWithinIfWithinElseTest() As Task
            Await VerifyRecommendationsContainAsync(
<MethodBody>
If True Then

Else
    If False Then
    |
    End If
End If
</MethodBody>, "Else")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ElseWithinElseIfWithinElseTest() As Task
            Await VerifyRecommendationsContainAsync(
<MethodBody>
If True Then

Else
    If False Then
    ElseIf True
        |
    End If
End If
</MethodBody>, "Else")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ElseNotWithinDoTest() As Task
            Await VerifyRecommendationsMissingAsync(
<MethodBody>
If True Then
Do While True
    |
End While
End If
</MethodBody>, "Else")
        End Function
    End Class
End Namespace
