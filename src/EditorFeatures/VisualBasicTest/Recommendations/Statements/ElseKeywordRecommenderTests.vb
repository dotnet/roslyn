' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class ElseKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub ElseNotInMethodBodyTest()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Else")
        End Sub

        <Fact>
        Public Sub ElseInMultiLineIfTest()
            VerifyRecommendationsContain(<MethodBody>If True Then
|
End If</MethodBody>, "Else")
        End Sub

        <Fact>
        Public Sub ElseInMultiLineElseIf1Test()
            VerifyRecommendationsContain(<MethodBody>If True Then
ElseIf True Then
|
End If</MethodBody>, "Else")
        End Sub

        <Fact>
        Public Sub ElseInMultiLineElseIf2Test()
            VerifyRecommendationsContain(<MethodBody>If True Then
Else If True Then
|
End If</MethodBody>, "Else")
        End Sub

        <Fact>
        Public Sub ElseNotInMultiLineElseTest()
            VerifyRecommendationsMissing(<MethodBody>If True Then
Else 
|
End If</MethodBody>, "Else")
        End Sub

        <Fact>
        Public Sub SingleLineIfElseAfterInvocationTest()
            VerifyRecommendationsContain(<MethodBody>If True Then System.Console.Write("Goo") |</MethodBody>, "Else")
        End Sub

        <Fact>
        Public Sub SingleLineIfElseAfterExpressionTest()
            VerifyRecommendationsContain(<MethodBody>If True Then q = q + 3 |</MethodBody>, "Else")
        End Sub

        <Fact>
        Public Sub SingleLineIfElseAfterDeclarationTest()
            VerifyRecommendationsContain(<MethodBody>If True Then Dim q = 3 |</MethodBody>, "Else")
        End Sub

        <Fact>
        Public Sub SingleLineIfElseAfterStopTest()
            VerifyRecommendationsContain(<MethodBody>If True Then Stop |</MethodBody>, "Else")
        End Sub

        <Fact>
        Public Sub SingleLineIfElseAfterEndTest()
            VerifyRecommendationsContain(<MethodBody>If True Then End |</MethodBody>, "Else")
        End Sub

        <Fact>
        Public Sub SingleLineIfElseAfterReDimTest()
            VerifyRecommendationsContain(<MethodBody>If True Then ReDim array(1, 1, 1) |</MethodBody>, "Else")
        End Sub

        <Fact>
        Public Sub SingleLineIfElseAfterEraseTest()
            VerifyRecommendationsContain(<MethodBody>If True Then Erase goo, quux |</MethodBody>, "Else")
        End Sub

        <Fact>
        Public Sub SingleLineIfElseAfterErrorTest()
            VerifyRecommendationsContain(<MethodBody>If True Then Error 23 |</MethodBody>, "Else")
        End Sub

        <Fact>
        Public Sub SingleLineIfElseAfterExitTest()
            VerifyRecommendationsContain(<MethodBody>If True Then Exit Do |</MethodBody>, "Else")
        End Sub

        <Fact>
        Public Sub SingleLineIfElseAfterGoToTest()
            VerifyRecommendationsContain(<MethodBody>If True Then GoTo goo |</MethodBody>, "Else")
        End Sub

        <Fact>
        Public Sub SingleLineIfElseAfterResumeTest()
            VerifyRecommendationsContain(<MethodBody>If True Then Resume |</MethodBody>, "Else")
        End Sub

        <Fact>
        Public Sub SingleLineIfElseAfterResumeNextTest()
            VerifyRecommendationsContain(<MethodBody>If True Then Resume Next |</MethodBody>, "Else")
        End Sub

        <Fact>
        Public Sub SingleLineIfElseAfterContinueTest()
            VerifyRecommendationsContain(<MethodBody>If True Then Continue Do |</MethodBody>, "Else")
        End Sub

        <Fact>
        Public Sub SingleLineIfElseAfterReturnTest()
            VerifyRecommendationsContain(<MethodBody>If True Then Return |</MethodBody>, "Else")
        End Sub

        <Fact>
        Public Sub SingleLineIfElseAfterReturnExpressionInPropertyGetTest()
            VerifyRecommendationsContain(
                <ClassDeclaration>
                    ReadOnly Property Goo As Integer
                        Get
                            If True Then Return |
                </ClassDeclaration>, "Else")
        End Sub

        <Fact>
        Public Sub SingleLineIfElseAfterReturnExpressionTest()
            VerifyRecommendationsContain(<ClassDeclaration>Function Goo as String 
                If True Then Return goo |</ClassDeclaration>, "Else")
        End Sub

        <Fact>
        Public Sub SingleLineIfElseAfterThrowTest()
            VerifyRecommendationsContain(<MethodBody>Try
                If True Then Throw |
                Catch</MethodBody>, "Else")
        End Sub

        <Fact>
        Public Sub SingleLineIfElseAfterThrowExpressionTest()
            VerifyRecommendationsContain(<MethodBody>Try
            If True Then Throw goo |
            Catch</MethodBody>, "Else")
        End Sub

        <Fact>
        Public Sub SingleLineIfElseAfterAddHandlerTest()
            VerifyRecommendationsContain(<MethodBody>If True Then AddHandler Obj.Ev_Event, AddressOf EventHandler |</MethodBody>, "Else")
        End Sub

        <Fact>
        Public Sub SingleLineIfElseAfterRaiseEventTest()
            VerifyRecommendationsContain(<MethodBody>If True Then RaiseEvent Ev_Event() |</MethodBody>, "Else")
        End Sub

        <Fact>
        Public Sub SingleLineIfElseAfterColonSeparatedStatementsTest()
            VerifyRecommendationsContain(<MethodBody>If True Then Console.WriteLine("goo") : Console.WriteLine("bar")  |</MethodBody>, "Else")
        End Sub

        <Fact>
        Public Sub ElseNotInSingleLineIf1Test()
            VerifyRecommendationsMissing(<MethodBody>If True Then |</MethodBody>, "Else")
        End Sub

        <Fact>
        Public Sub ElseNotInSingleLineIf2Test()
            VerifyRecommendationsMissing(<MethodBody>If True Then Stop Else |</MethodBody>, "Else")
        End Sub

        <Fact>
        Public Sub ElseNotSingleLineIfNoStatementsTest()
            VerifyRecommendationsMissing(<MethodBody>If True Then Else |</MethodBody>, "Else")
        End Sub

        <Fact>
        Public Sub ElseAfterCaseTest()
            VerifyRecommendationsContain(
<MethodBody>
Select Case x
    Case |
</MethodBody>, "Else")
        End Sub

        <Fact>
        Public Sub NoElseAfterCaseIfExistsTest()
            VerifyRecommendationsMissing(
<MethodBody>
Select Case x
    Case Else End
    Case |
</MethodBody>, "Else")
        End Sub

        <Fact>
        Public Sub NoElseAfterCaseIfNotLastCaseTest()
            VerifyRecommendationsMissing(
<MethodBody>
Select Case x
    Case |
    Case 1
</MethodBody>, "Else")
        End Sub

        <Fact>
        Public Sub ElseWithinIfWithinElseTest()
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

        <Fact>
        Public Sub ElseWithinElseIfWithinElseTest()
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

        <Fact>
        Public Sub ElseNotWithinDoTest()
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
