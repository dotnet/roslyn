' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    Public Sub ERR_InterpolationFormatWhitespace()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""Hello, {EventArgs.Empty:C02 }!"")
    End Sub
End Module"
        ).AssertTheseDiagnostics(
<expected>
BC37249: Format specifier may not contain trailing whitespace.
        Console.WriteLine($"Hello, {EventArgs.Empty:C02 }!")
                                                    ~~~
</expected>)

    End Sub

    <Fact>
    Public Sub Error_NewLineAfterAfterOpenBraceAndBeforeCloseBraceWithoutFormatClause()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""Hello, {
EventArgs.Empty
}!"")
    End Sub
End Module"
        ).AssertTheseDiagnostics(
<expected>
BC30625: 'Module' statement must end with a matching 'End Module'.
Module Program
~~~~~~~~~~~~~~
BC30026: 'End Sub' expected.
    Sub Main()
    ~~~~~~~~~~
BC30198: ')' expected.
        Console.WriteLine($"Hello, {
                                    ~
BC30201: Expression expected.
        Console.WriteLine($"Hello, {
                                    ~
BC30370: '}' expected.
        Console.WriteLine($"Hello, {
                                    ~
BC30648: String constants must end with a double quote.
}!")
  ~~~
</expected>)

    End Sub

    <Fact>
    Public Sub Error_NewLineAfterAlignmentClause()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""Hello, {EventArgs.Empty,-10
:C02}!"")
    End Sub
End Module"
        ).AssertTheseDiagnostics(
<expected>
BC30625: 'Module' statement must end with a matching 'End Module'.
Module Program
~~~~~~~~~~~~~~
BC30026: 'End Sub' expected.
    Sub Main()
    ~~~~~~~~~~
BC30198: ')' expected.
        Console.WriteLine($"Hello, {EventArgs.Empty,-10
                                                       ~
BC30370: '}' expected.
        Console.WriteLine($"Hello, {EventArgs.Empty,-10
                                                       ~
BC30201: Expression expected.
:C02}!")
    ~
BC30800: Method arguments must be enclosed in parentheses.
:C02}!")
    ~
BC30648: String constants must end with a double quote.
:C02}!")
      ~~~
</expected>)
    End Sub

    <Fact>
    Public Sub Error_NewLineAfterAlignmentClauseCommaToken()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""Hello, {EventArgs.Empty,
-10:C02}!"")
    End Sub
End Module"
        ).AssertTheseDiagnostics(
<expected>
BC30625: 'Module' statement must end with a matching 'End Module'.
Module Program
~~~~~~~~~~~~~~
BC30026: 'End Sub' expected.
    Sub Main()
    ~~~~~~~~~~
BC30198: ')' expected.
        Console.WriteLine($"Hello, {EventArgs.Empty,
                                                    ~
BC30204: Integer constant expected.
        Console.WriteLine($"Hello, {EventArgs.Empty,
                                                    ~
BC30370: '}' expected.
        Console.WriteLine($"Hello, {EventArgs.Empty,
                                                    ~
BC30035: Syntax error.
-10:C02}!")
~
BC30201: Expression expected.
-10:C02}!")
       ~
BC30800: Method arguments must be enclosed in parentheses.
-10:C02}!")
       ~
BC30648: String constants must end with a double quote.
-10:C02}!")
         ~~~
</expected>)
    End Sub

    <Fact>
    Public Sub Error_NewLineAfterFormatClause()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""Hello, {EventArgs.Empty:C02
}!"")
    End Sub
End Module"
        ).AssertTheseDiagnostics(
<expected>
BC30625: 'Module' statement must end with a matching 'End Module'.
Module Program
~~~~~~~~~~~~~~
BC30026: 'End Sub' expected.
    Sub Main()
    ~~~~~~~~~~
BC30198: ')' expected.
        Console.WriteLine($"Hello, {EventArgs.Empty:C02
                                                       ~
BC30370: '}' expected.
        Console.WriteLine($"Hello, {EventArgs.Empty:C02
                                                       ~
BC30648: String constants must end with a double quote.
}!")
  ~~~
</expected>)
    End Sub

    <Fact>
    Public Sub Error_NewLineAfterFormatClauseColonToken()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""Hello, {EventArgs.Empty:
C02}!"")
    End Sub
End Module"
        ).AssertTheseDiagnostics(
<expected>
BC30625: 'Module' statement must end with a matching 'End Module'.
Module Program
~~~~~~~~~~~~~~
BC30026: 'End Sub' expected.
    Sub Main()
    ~~~~~~~~~~
BC30198: ')' expected.
        Console.WriteLine($"Hello, {EventArgs.Empty:
                                                    ~
BC30370: '}' expected.
        Console.WriteLine($"Hello, {EventArgs.Empty:
                                                    ~
BC30201: Expression expected.
C02}!")
   ~
BC30800: Method arguments must be enclosed in parentheses.
C02}!")
   ~
BC30648: String constants must end with a double quote.
C02}!")
     ~~~
</expected>)
    End Sub

    <Fact>
    Public Sub ErrorRecovery_DollarSignMissingDoubleQuote()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($)
    End Sub
End Module")
    End Sub

    <Fact>
    Public Sub ErrorRecovery_MissingClosingDoubleQuote()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($"")
    End Sub
End Module")
    End Sub

    <Fact>
    Public Sub ErrorRecovery_MissingCloseBrace()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{"")
    End Sub
End Module")
    End Sub

    <Fact>
    Public Sub ErrorRecovery_MissingExpressionWithAlignment()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{,5}"")
    End Sub
End Module")
    End Sub

    <Fact>
    Public Sub ErrorRecovery_MissingExpressionWithFormatString()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{:C02}"")
    End Sub
End Module")
    End Sub

    <Fact>
    Public Sub ErrorRecovery_MissingExpressionWithAlignmentAndFormatString()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{,5:C02}"")
    End Sub
End Module")
    End Sub

    <Fact>
    Public Sub ErrorRecovery_MissingExpressionAndAlignment()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{,}"")
    End Sub
End Module")
    End Sub

    <Fact>
    Public Sub ErrorRecovery_MissingExpressionAndAlignmentAndFormatString()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{,:}"")
    End Sub
End Module")
    End Sub

    <Fact>
    Public Sub ErrorRecovery_MissingExpression()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{}"")
    End Sub
End Module")
    End Sub

    <Fact>
    Public Sub ErrorRecovery_NonExpressionKeyword()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{For}"")
    End Sub
End Module")
    End Sub

    <Fact>
    Public Sub ErrorRecovery_NonExpressionCharacter()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{`}"")
    End Sub
End Module")
    End Sub

    <Fact>
    Public Sub ErrorRecovery_IncompleteExpression()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{(1 +}"")
    End Sub
End Module")
    End Sub

    <Fact>
    Public Sub ErrorRecovery_MissingAlignment()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{1,}"")
    End Sub
End Module")
    End Sub

    <Fact>
    Public Sub ErrorRecovery_BadAlignment()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{1,&}"")
    End Sub
End Module")
    End Sub

    <Fact>
    Public Sub ErrorRecovery_MissingFormatString()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{1:}"")
    End Sub
End Module")
    End Sub

    <Fact>
    Public Sub ErrorRecovery_AlignmentWithMissingFormatString()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{1,5:}"")
    End Sub
End Module")
    End Sub

    <Fact>
    Public Sub ErrorRecovery_AlignmentAndFormatStringOutOfOrder()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{1:C02,-5}"")
    End Sub
End Module")
    End Sub

    <Fact>
    Public Sub ErrorRecovery_MissingOpenBrace()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""}"")
    End Sub
End Module")
    End Sub

    <Fact>
    Public Sub ErrorRecovery_DollarSignMissingDoubleQuote_NestedInIncompleteExpression()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($
    End Sub
End Module")
    End Sub

    <Fact>
    Public Sub ErrorRecovery_MissingClosingDoubleQuote_NestedInIncompleteExpression()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""
    End Sub
End Module")
    End Sub

    <Fact>
    Public Sub ErrorRecovery_MissingCloseBrace_NestedInIncompleteExpression()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{""
    End Sub
End Module")
    End Sub

    <Fact>
    Public Sub ErrorRecovery_MissingExpression_NestedInIncompleteExpression()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{}""
    End Sub
End Module")
    End Sub

    <Fact>
    Public Sub ErrorRecovery_NonExpressionKeyword_NestedInIncompleteExpression()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{For}""
    End Sub
End Module")
    End Sub

    <Fact>
    Public Sub ErrorRecovery_NonExpressionCharacter_NestedInIncompleteExpression()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{`}""
    End Sub
End Module")
    End Sub

    <Fact>
    Public Sub ErrorRecovery_IncompleteExpression_NestedInIncompleteExpression()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{(1 +}""
    End Sub
End Module")
    End Sub

    <Fact>
    Public Sub ErrorRecovery_MissingAlignment_NestedInIncompleteExpression()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{1,}""
    End Sub
End Module")
    End Sub

    <Fact>
    Public Sub ErrorRecovery_BadAlignment_NestedInIncompleteExpression()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{1,&}""
    End Sub
End Module")
    End Sub

    <Fact>
    Public Sub ErrorRecovery_MissingFormatString_NestedInIncompleteExpression()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{1:}""
    End Sub
End Module")
    End Sub

    <Fact>
    Public Sub ErrorRecovery_AlignmentWithMissingFormatString_NestedInIncompleteExpression()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{1,5:}""
    End Sub
End Module")
    End Sub

    <Fact>
    Public Sub ErrorRecovery_AlignmentAndFormatStringOutOfOrder_NestedInIncompleteExpression()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{1:C02,-5}""
    End Sub
End Module")
    End Sub

    <Fact>
    Public Sub ErrorRecovery_MissingOpenBrace_NestedInIncompleteExpression()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""}""
    End Sub
End Module")
    End Sub

    <Fact>
    Public Sub ErrorRecovery_NonExpressionKeyword_InUnclosedInterpolation()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{For"")
    End Sub
End Module")
    End Sub

    <Fact>
    Public Sub ErrorRecovery_NonExpressionCharacter_InUnclosedInterpolation()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{`"")
    End Sub
End Module")
    End Sub

    <Fact>
    Public Sub ErrorRecovery_IncompleteExpression_InUnclosedInterpolation()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{(1 +"")
    End Sub
End Module")
    End Sub

    <Fact>
    Public Sub ErrorRecovery_MissingAlignment_InUnclosedInterpolation()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{1,"")
    End Sub
End Module")
    End Sub

    <Fact>
    Public Sub ErrorRecovery_BadAlignment_InUnclosedInterpolation()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{1,&"")
    End Sub
End Module")
    End Sub

    <Fact>
    Public Sub ErrorRecovery_MissingFormatString_InUnclosedInterpolation()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{1:"")
    End Sub
End Module")
    End Sub

    <Fact>
    Public Sub ErrorRecovery_AlignmentWithMissingFormatString_InUnclosedInterpolation()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{1,5:"")
    End Sub
End Module")
    End Sub

    <Fact>
    Public Sub ErrorRecovery_AlignmentAndFormatStringOutOfOrder_InUnclosedInterpolation()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{1:C02,-5"")
    End Sub
End Module")
    End Sub

    <Fact>
    Public Sub ErrorRecovery_NonExpressionKeyword_InUnclosedInterpolation_NestedInIncompleteExpression()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{For
    End Sub
End Module")
    End Sub

    <Fact>
    Public Sub ErrorRecovery_NonExpressionCharacter_InUnclosedInterpolation_NestedInIncompleteExpression()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{`
    End Sub
End Module")
    End Sub

    <Fact>
    Public Sub ErrorRecovery_IncompleteExpression_InUnclosedInterpolation_NestedInIncompleteExpression()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{(1 +
    End Sub
End Module")
    End Sub

    <Fact>
    Public Sub ErrorRecovery_MissingAlignment_InUnclosedInterpolation_NestedInIncompleteExpression()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{1,
    End Sub
End Module")
    End Sub

    <Fact>
    Public Sub ErrorRecovery_BadAlignment_InUnclosedInterpolation_NestedInIncompleteExpression()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{1,&
    End Sub
End Module")
    End Sub

    <Fact>
    Public Sub ErrorRecovery_MissingFormatString_InUnclosedInterpolation_NestedInIncompleteExpression()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{1:
    End Sub
End Module")
    End Sub

    <Fact>
    Public Sub ErrorRecovery_AlignmentWithMissingFormatString_InUnclosedInterpolation_NestedInIncompleteExpression()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{1,5:
    End Sub
End Module")
    End Sub

    <Fact>
    Public Sub ErrorRecovery_AlignmentAndFormatStringOutOfOrder_InUnclosedInterpolation_NestedInIncompleteExpression()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{1:C02,-5
    End Sub
End Module")
    End Sub

    <Fact>
    Public Sub ErrorRecovery_IncompleteExpression_FollowedByAColon()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{CStr(:C02}"")
        Console.WriteLine($""{CStr(1:C02}"")
    End Sub
End Module")
    End Sub

    <Fact>
    Public Sub ErrorRecovery_IncompleteExpression_FollowedByATwoColons()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{CStr(::C02}"")
        Console.WriteLine($""{CStr(1::C02}"")
    End Sub
End Module")
    End Sub

    <Fact>
    Public Sub ErrorRecovery_ExtraCloseBraceFollowingInterpolationWithNoFormatClause()
        Parse(
"Module Program
    Sub Main()
        Console.WriteLine($""{1}}"")
    End Sub
End Module")
    End Sub

    <Fact, WorkItem(6341, "https://github.com/dotnet/roslyn/issues/6341")>
    Public Sub LineBreakInInterpolation_1()
        Parse(
"Module Program
    Sub Main()
        Dim x = $""{ " + vbCr + vbCr + "1 

}""
    End Sub
End Module"
        ).AssertTheseDiagnostics(
<expected>
BC30625: 'Module' statement must end with a matching 'End Module'.
Module Program
~~~~~~~~~~~~~~
BC30026: 'End Sub' expected.
    Sub Main()
    ~~~~~~~~~~
BC30201: Expression expected.
        Dim x = $"{ 
                    ~
BC30370: '}' expected.
        Dim x = $"{ 
                    ~
BC30801: Labels that are numbers must be followed by colons.
1 
~~
BC30648: String constants must end with a double quote.
}"
 ~~
</expected>)

    End Sub

    <Fact, WorkItem(6341, "https://github.com/dotnet/roslyn/issues/6341")>
    Public Sub LineBreakInInterpolation_2()
        Parse(
"Module Program
    Sub Main()
        Dim x = $""{ 1 " + vbCr + vbCr + " 

}""
    End Sub
End Module"
        ).AssertTheseDiagnostics(
<expected>
BC30625: 'Module' statement must end with a matching 'End Module'.
Module Program
~~~~~~~~~~~~~~
BC30026: 'End Sub' expected.
    Sub Main()
    ~~~~~~~~~~
BC30370: '}' expected.
        Dim x = $"{ 1 
                      ~
BC30648: String constants must end with a double quote.
}"
 ~~
</expected>)

    End Sub

End Class
