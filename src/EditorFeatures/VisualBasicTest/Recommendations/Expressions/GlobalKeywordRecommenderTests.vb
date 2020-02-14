﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    Public Class GlobalKeywordRecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoneInClassDeclarationTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>|</ClassDeclaration>, "Global")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GlobalInStatementTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>|</MethodBody>, "Global")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GlobalAfterReturnTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Return |</MethodBody>, "Global")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GlobalAfterArgument1Test() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Goo(|</MethodBody>, "Global")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GlobalAfterArgument2Test() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Goo(bar, |</MethodBody>, "Global")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GlobalAfterBinaryExpressionTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Goo(bar + |</MethodBody>, "Global")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GlobalAfterNotTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Goo(Not |</MethodBody>, "Global")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GlobalAfterTypeOfTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>If TypeOf |</MethodBody>, "Global")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GlobalAfterDoWhileTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Do While |</MethodBody>, "Global")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GlobalAfterDoUntilTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Do Until |</MethodBody>, "Global")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GlobalAfterLoopWhileTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Do
Loop While |</MethodBody>, "Global")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GlobalAfterLoopUntilTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Do
Loop Until |</MethodBody>, "Global")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GlobalAfterIfTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>If |</MethodBody>, "Global")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GlobalAfterElseIfTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>ElseIf |</MethodBody>, "Global")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GlobalAfterElseSpaceIfTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Else If |</MethodBody>, "Global")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GlobalAfterErrorTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Error |</MethodBody>, "Global")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GlobalAfterThrowTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Throw |</MethodBody>, "Global")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GlobalAfterInitializerTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = |</MethodBody>, "Global")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GlobalAfterArrayInitializerSquiggleTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = {|</MethodBody>, "Global")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GlobalAfterArrayInitializerCommaTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = {0, |</MethodBody>, "Global")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GlobalNotAfterItselfTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>Global.|</MethodBody>, "Global")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GlobalNotAfterImportsTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>Imports |</File>, "Global")
        End Function

        <WorkItem(543270, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543270")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotInDelegateCreationTest() As Task
            Dim code =
<File>
Module Program
    Sub Main(args As String())
        Dim f1 As New Goo2( |
    End Sub

    Delegate Sub Goo2()

    Function Bar2() As Object
        Return Nothing
    End Function
End Module
</File>


            Await VerifyRecommendationsMissingAsync(code, "Global")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GlobalAfterInheritsTest() As Task
            Dim code =
<File>
Class C
    Inherits |
End Class
</File>

            Await VerifyRecommendationsContainAsync(code, "Global")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GlobalAfterImplementsTest() As Task
            Dim code =
<File>
Class C
    Implements |
End Class
</File>

            Await VerifyRecommendationsContainAsync(code, "Global")
        End Function
    End Class
End Namespace
