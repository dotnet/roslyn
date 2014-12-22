' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.SyntaxFacts
Imports Roslyn.Test.Utilities

Public Class InterpolatedStringParsingTests
    Inherits BasicTestBase

    <Fact>
    Public Sub EmptyString()

        ParseAndVerify(
"Module Program
    Sub Main()
        Console.WriteLine($"""")
    End Sub
End Module")

    End Sub

    <Fact>
    Public Sub NoInterpolations()

        ParseAndVerify(
"Module Program
    Sub Main()
        Console.WriteLine($""Hello, World!"")
    End Sub
End Module")

    End Sub

    <Fact>
    Public Sub OnlyInterpolation()

        ParseAndVerify(
"Module Program
    Sub Main()
        Console.WriteLine($""{""Hello, World!""}"")
    End Sub
End Module")

    End Sub

    <Fact>
    Public Sub SimpleInterpolation()

        ParseAndVerify(
"Module Program
    Sub Main()
        Console.WriteLine($""Hello, {name}!"")
    End Sub
End Module")

    End Sub

    <Fact>
    Public Sub ParenthesizedInterpolation()

        ParseAndVerify(
"Module Program
    Sub Main()
        Console.WriteLine($""Hello, {(firstName & lastName)}!"")
    End Sub
End Module")

    End Sub

    <Fact>
    Public Sub ComplexInterpolation_QueryExpression()

        ParseAndVerify(
"Module Program
    Sub Main()
        Console.WriteLine($""Hello, {From name In names Select name.Length}!"")
    End Sub
End Module")

    End Sub

    <Fact>
    Public Sub EscapedBraces()

        ParseAndVerify(
"Module Program
    Sub Main()
        Console.WriteLine($""{{ {x}, {y} }}"")
    End Sub
End Module")

    End Sub

    <Fact>
    Public Sub EmbeddedBracesWorkaround()

        ParseAndVerify(
"Module Program
    Sub Main()
        Console.WriteLine($""{""{""}{x}, {y}{""}""}"")
    End Sub
End Module")

    End Sub

    <Fact>
    Public Sub AlignmentClause()

        ParseAndVerify(
"Module Program
    Sub Main()
        Console.WriteLine(""Header 1 | Header 2 | Header 3"")
        Console.WriteLine($""{items(0),9}|{items(1),9}|{items(2),9}"")
    End Sub
End Module")

    End Sub

    <Fact>
    Public Sub FormatStringClause()

        ParseAndVerify(
"Module Program
    Sub Main()
        Console.WriteLine($""You owe: {balanceDue:C02}."")
    End Sub
End Module")

    End Sub

    <Fact>
    Public Sub FormatStringClause_WithTwoColons()

        ParseAndVerify(
"Module Program
    Sub Main()
        Console.WriteLine($""You owe: {balanceDue::C02}."")
    End Sub
End Module")

    End Sub

    <Fact>
    Public Sub AlignmentClauseAndFormatClause()

        ParseAndVerify(
"Module Program
    Sub Main()
        Console.WriteLine($""You owe: {balanceDue,10:C02}."")
    End Sub
End Module")

    End Sub

    <Fact>
    Public Sub MultilineText()

        ParseAndVerify(
"Module Program
    Sub Main()
        Console.WriteLine(
$""Name: 
    {name}
Age:
    {age}
====="")
    End Sub
End Module")

    End Sub

    <Fact>
    Public Sub ImplicitLineContinuation_AfterAfterOpenBraceAndBeforeCloseBraceWithoutFormatClause()

        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""Hello, {
                                        From name In names 
                                        Select name.Length
                                    }!"")
    End Sub
End Module")

    End Sub

    <Fact>
    Public Sub ImplicitLineContinuation_AfterAlignmentClause()

        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""Hello, {
                                        From name In names 
                                        Select name.Length,-5
                                    }!"")
    End Sub
End Module")

    End Sub

    <Fact>
    Public Sub ImplicitLineContinuation_AfterFormatClause()

        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""Hello, {
                                        From name In names 
                                        Select name.Length:C02
                                    }!"")
    End Sub
End Module")

    End Sub

    <Fact>
    Sub ErrorRecovery_DollarSignMissingDoubleQuote()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($)
    End Sub
End Module")
    End Sub

    <Fact>
    Sub ErrorRecovery_MissingClosingDoubleQuote()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($"")
    End Sub
End Module")
    End Sub

    <Fact>
    Sub ErrorRecovery_MissingCloseBrace()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{"")
    End Sub
End Module")
    End Sub

    <Fact>
    Sub ErrorRecovery_MissingExpressionWithAlignment()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{,5}"")
    End Sub
End Module")
    End Sub

    <Fact>
    Sub ErrorRecovery_MissingExpressionWithFormatString()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{:C02}"")
    End Sub
End Module")
    End Sub

    <Fact>
    Sub ErrorRecovery_MissingExpressionWithAlignmentAndFormatString()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{,5:C02}"")
    End Sub
End Module")
    End Sub

    <Fact>
    Sub ErrorRecovery_MissingExpressionAndAlignment()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{,}"")
    End Sub
End Module")
    End Sub

    <Fact>
    Sub ErrorRecovery_MissingExpressionAndAlignmentAndFormatString()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{,:}"")
    End Sub
End Module")
    End Sub

    <Fact>
    Sub ErrorRecovery_MissingExpression()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{}"")
    End Sub
End Module")
    End Sub

    <Fact>
    Sub ErrorRecovery_NonExpressionKeyword()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{For}"")
    End Sub
End Module")
    End Sub

    <Fact>
    Sub ErrorRecovery_NonExpressionCharacter()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{`}"")
    End Sub
End Module")
    End Sub

    <Fact>
    Sub ErrorRecovery_IncompleteExpression()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{(1 +}"")
    End Sub
End Module")
    End Sub

    <Fact>
    Sub ErrorRecovery_MissingAlignment()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{1,}"")
    End Sub
End Module")
    End Sub

    <Fact>
    Sub ErrorRecovery_BadAlignment()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{1,&}"")
    End Sub
End Module")
    End Sub

    <Fact>
    Sub ErrorRecovery_MissingFormatString()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{1:}"")
    End Sub
End Module")
    End Sub

    <Fact>
    Sub ErrorRecovery_AlignmentWithMissingFormatString()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{1,5:}"")
    End Sub
End Module")
    End Sub

    <Fact>
    Sub ErrorRecovery_AlignmentAndFormatStringOutOfOrder()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{1:C02,-5}"")
    End Sub
End Module")
    End Sub

    <Fact>
    Sub ErrorRecovery_MissingOpenBrace()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""}"")
    End Sub
End Module")
    End Sub

    <Fact>
    Sub ErrorRecovery_DollarSignMissingDoubleQuote_NestedInIncompleteExpression()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($
    End Sub
End Module")
    End Sub

    <Fact>
    Sub ErrorRecovery_MissingClosingDoubleQuote_NestedInIncompleteExpression()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""
    End Sub
End Module")
    End Sub

    <Fact>
    Sub ErrorRecovery_MissingCloseBrace_NestedInIncompleteExpression()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{""
    End Sub
End Module")
    End Sub

    <Fact>
    Sub ErrorRecovery_MissingExpression_NestedInIncompleteExpression()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{}""
    End Sub
End Module")
    End Sub

    <Fact>
    Sub ErrorRecovery_NonExpressionKeyword_NestedInIncompleteExpression()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{For}""
    End Sub
End Module")
    End Sub

    <Fact>
    Sub ErrorRecovery_NonExpressionCharacter_NestedInIncompleteExpression()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{`}""
    End Sub
End Module")
    End Sub

    <Fact>
    Sub ErrorRecovery_IncompleteExpression_NestedInIncompleteExpression()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{(1 +}""
    End Sub
End Module")
    End Sub

    <Fact>
    Sub ErrorRecovery_MissingAlignment_NestedInIncompleteExpression()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{1,}""
    End Sub
End Module")
    End Sub

    <Fact>
    Sub ErrorRecovery_BadAlignment_NestedInIncompleteExpression()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{1,&}""
    End Sub
End Module")
    End Sub

    <Fact>
    Sub ErrorRecovery_MissingFormatString_NestedInIncompleteExpression()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{1:}""
    End Sub
End Module")
    End Sub

    <Fact>
    Sub ErrorRecovery_AlignmentWithMissingFormatString_NestedInIncompleteExpression()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{1,5:}""
    End Sub
End Module")
    End Sub

    <Fact>
    Sub ErrorRecovery_AlignmentAndFormatStringOutOfOrder_NestedInIncompleteExpression()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{1:C02,-5}""
    End Sub
End Module")
    End Sub

    <Fact>
    Sub ErrorRecovery_MissingOpenBrace_NestedInIncompleteExpression()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""}""
    End Sub
End Module")
    End Sub

    <Fact>
    Sub ErrorRecovery_NonExpressionKeyword_InUnclosedInterpolation()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{For"")
    End Sub
End Module")
    End Sub

    <Fact>
    Sub ErrorRecovery_NonExpressionCharacter_InUnclosedInterpolation()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{`"")
    End Sub
End Module")
    End Sub

    <Fact>
    Sub ErrorRecovery_IncompleteExpression_InUnclosedInterpolation()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{(1 +"")
    End Sub
End Module")
    End Sub

    <Fact>
    Sub ErrorRecovery_MissingAlignment_InUnclosedInterpolation()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{1,"")
    End Sub
End Module")
    End Sub

    <Fact>
    Sub ErrorRecovery_BadAlignment_InUnclosedInterpolation()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{1,&"")
    End Sub
End Module")
    End Sub

    <Fact>
    Sub ErrorRecovery_MissingFormatString_InUnclosedInterpolation()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{1:"")
    End Sub
End Module")
    End Sub

    <Fact>
    Sub ErrorRecovery_AlignmentWithMissingFormatString_InUnclosedInterpolation()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{1,5:"")
    End Sub
End Module")
    End Sub

    <Fact>
    Sub ErrorRecovery_AlignmentAndFormatStringOutOfOrder_InUnclosedInterpolation()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{1:C02,-5"")
    End Sub
End Module")
    End Sub

    <Fact>
    Sub ErrorRecovery_NonExpressionKeyword_InUnclosedInterpolation_NestedInIncompleteExpression()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{For
    End Sub
End Module")
    End Sub

    <Fact>
    Sub ErrorRecovery_NonExpressionCharacter_InUnclosedInterpolation_NestedInIncompleteExpression()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{`
    End Sub
End Module")
    End Sub

    <Fact>
    Sub ErrorRecovery_IncompleteExpression_InUnclosedInterpolation_NestedInIncompleteExpression()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{(1 +
    End Sub
End Module")
    End Sub

    <Fact>
    Sub ErrorRecovery_MissingAlignment_InUnclosedInterpolation_NestedInIncompleteExpression()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{1,
    End Sub
End Module")
    End Sub

    <Fact>
    Sub ErrorRecovery_BadAlignment_InUnclosedInterpolation_NestedInIncompleteExpression()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{1,&
    End Sub
End Module")
    End Sub

    <Fact>
    Sub ErrorRecovery_MissingFormatString_InUnclosedInterpolation_NestedInIncompleteExpression()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{1:
    End Sub
End Module")
    End Sub

    <Fact>
    Sub ErrorRecovery_AlignmentWithMissingFormatString_InUnclosedInterpolation_NestedInIncompleteExpression()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{1,5:
    End Sub
End Module")
    End Sub

    <Fact>
    Sub ErrorRecovery_AlignmentAndFormatStringOutOfOrder_InUnclosedInterpolation_NestedInIncompleteExpression()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{1:C02,-5
    End Sub
End Module")
    End Sub

    <Fact>
    Sub ErrorRecovery_IncompleteExpression_FollowedByAColon()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{CStr(:C02}"")
        Console.WriteLine($""{CStr(1:C02}"")
    End Sub
End Module")
    End Sub

    <Fact>
    Sub ErrorRecovery_IncompleteExpression_FollowedByATwoColons()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{CStr(::C02}"")
        Console.WriteLine($""{CStr(1::C02}"")
    End Sub
End Module")
    End Sub

    <Fact>
    Sub ErrorRecovery_ExtraCloseBraceFollowingInterpolationWithNoFormatClause()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{1}}"")
    End Sub
End Module")
    End Sub

    <Fact>
    Sub SmartQuotes()
        Parse(
"Module Program
    Sub Main()

        Dim arr = {
            $““””,
            $””““,
            $““"",
            $""““,
            $""””,
            $””"",
            $"" ””““ "",
            $”” {1:x””““y} ““
        }

““””). 

    End Sub
End Module")
    End Sub


End Class