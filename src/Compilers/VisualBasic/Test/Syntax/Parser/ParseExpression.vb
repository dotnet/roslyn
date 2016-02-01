' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Globalization
Imports System.Text
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

<CLSCompliant(False)>
Public Class ParseExpressionTest
    Inherits BasicTestBase

    Private Function ParseExpressionAsRhs(text As XElement) As ExpressionSyntax
        Return ParseExpressionAsRhs(text.Value)
    End Function

    Private Function ParseExpressionAsRhs(text As String, Optional expectsErrors As Boolean = False) As ExpressionSyntax
        Dim modText =
    <Module>
        Module M1
            Dim x =<%= text %>
        End Module
    </Module>.Value

        Dim prog = SyntaxFactory.ParseCompilationUnit(modText)
        Assert.Equal(modText, prog.ToFullString)
        Assert.Equal(expectsErrors, prog.ContainsDiagnostics)

        Dim modTree = DirectCast(prog.Members(0), TypeBlockSyntax)
        Dim varDecl = DirectCast(modTree.Members(0), FieldDeclarationSyntax)

        Return varDecl.Declarators(0).Initializer.Value
    End Function

    Private Function ParseExpression(text As XElement) As ExpressionSyntax
        Return ParseExpression(text.Value)
    End Function

    Private Function ParseExpression(text As String, Optional expectsErrors As Boolean = False) As ExpressionSyntax
        Dim modText = text.Trim(" "c, CChar(vbTab), CChar(vbCr), CChar(vbLf))

        Dim expr = SyntaxFactory.ParseExpression(modText)
        Assert.Equal(modText, expr.ToFullString)
        Assert.Equal(expectsErrors, expr.ContainsDiagnostics)

        Return expr
    End Function

    <Fact>
    Public Sub ParseExpressionTest1()
        ParseExpression(<text>    3 + 0  </text>)
    End Sub

    <Fact>
    Public Sub ParseExpressionTest2()
        ParseExpression(<text>    30 *  0 </text>)
    End Sub
    <Fact>
    Public Sub ParseExpressionTest3()
        ParseExpression(<text>    3^0  </text>)
    End Sub
    <Fact>
    Public Sub ParseExpressionTest4()
        ParseExpression(<text>    30 > 0  </text>)
    End Sub

    <Fact>
    Public Sub ParseExpressionTest5()
        ParseExpression(<text>    30 >= 0  </text>)
    End Sub
    <Fact>
    Public Sub ParseExpressionTest6()
        ParseExpression(<text>    30 = 0  </text>)
    End Sub

    <Fact>
    Public Sub ParseExpressionTest7()
        ParseExpression("30 < 0")
    End Sub

    <Fact>
    Public Sub ParseExpressionTest8()
        ParseExpression("30 <= 0")
    End Sub

    <Fact>
    Public Sub ParseExpressionTest9()
        ParseExpression("AddressOf 3")
    End Sub

#Region "Literal Test"


    <Fact>
    Public Sub ParseIntegerLiteralTest()
        ParseExpression("&H1")
        ParseExpression("&O1")
    End Sub

    <Fact>
    Public Sub ParseCharLiteralTest()
        ParseExpression(<Text>"a"c </Text>)
    End Sub

    <Fact>
    Public Sub ParseDecLiteralTest()
        ParseExpression(<Text>12@</Text>)
        ParseExpression(<Text>12.9@</Text>)
        ParseExpression(<Text>12.90@</Text>)
        ParseExpression(<Text>.90@</Text>)
        ParseExpression(<Text>01.20@</Text>)
    End Sub

    <Fact>
    Public Sub ParseStringLiteralTest()
        ParseExpression(<Text>"a" </Text>)
        ParseExpression(<Text> "ab" </Text>)
        ParseExpression(<Text>"" </Text>)
    End Sub

    <Fact>
    Public Sub ParseFloatLiteralTest()
        ParseExpression(<Text>"1.2" </Text>)
        ParseExpression(<Text> ".3" </Text>)
        ParseExpression(<Text>"0.34" </Text>)

        ParseExpression(<Text>"2F" </Text>)
        ParseExpression(<Text> "3R" </Text>)
        ParseExpression(<Text>"4D" </Text>)

        ParseExpression(<Text>"1.2F" </Text>)
        ParseExpression(<Text> ".3R" </Text>)
        ParseExpression(<Text>"0.34D" </Text>)

        ParseExpression(<Text>"1.2E1" </Text>)
        ParseExpression(<Text> ".3E0" </Text>)
        ParseExpression(<Text>"0.34E2" </Text>)

        ParseExpression(<Text>"1.2E1F" </Text>)
        ParseExpression(<Text> ".3E0R" </Text>)
        ParseExpression(<Text>"0.34E2D" </Text>)
    End Sub

    <Fact>
    Public Sub ParseDateLiteralTest()

        ParseExpressionAsRhs(<Text># 8/23/1970 3:45:39AM #</Text>)
        ParseExpressionAsRhs(<Text># 8/23/1970 #</Text>)
        ParseExpressionAsRhs(<Text># 3:45:39AM # </Text>)

        ParseExpressionAsRhs(<Text># 3:45:39 #</Text>)
        ParseExpressionAsRhs(<Text># 1AM #</Text>)
        ParseExpressionAsRhs(<Text># 1:45:39PM # </Text>)

        ParseExpressionAsRhs(<Text># 8-23-1970 3:45AM #</Text>)

    End Sub

    <Fact>
    Public Sub ParseBooleanLiteralTest()
        ParseExpression("True")
        ParseExpression("False")
    End Sub

#End Region

    <Fact>
    Public Sub ParseIdentifier()
        ParseExpression("a")
        ParseExpression("B(Of X)")
        ParseExpression("B( Of y, [exit] )")
    End Sub

    <Fact>
    Public Sub ParseArrayInitializer()
        ParseExpression("{  2, 1}")
        ParseExpression("{ }")
        ParseExpression(<text>{1,3
}</text>)

    End Sub
    <Fact>
    Public Sub ParseNestedArrayInitializer()
        ParseExpression("{ {}, 2, 1}")
        ParseExpression(<Text>{{
                                  }
                                  }</Text>)
        ParseExpression(<text>{1,{{3}}
}</text>)

    End Sub

#Region "ParseExpression Error Test"
    <Fact>
    Public Sub BC36637ERR_NullableCharNotSupported()
        Dim exp = ParseExpression("1?", True)

        Assert.True(exp.ContainsDiagnostics)

        Dim unexp = From t In exp.GetTrailingTrivia() Where t.Kind = SyntaxKind.SkippedTokensTrivia

        'ERRID_NullableCharNotSupported = 36637
        Assert.Equal(1, unexp.Count)
        Assert.Equal("?", unexp(0).ToString())
        Assert.Equal(36637, unexp(0).Errors(0).Code)
    End Sub

    <Fact>
    Public Sub BC30944ERR_SyntaxInCastOp()
        Dim exp As CastExpressionSyntax = DirectCast(ParseExpression("TryCast(1 a)", True), CastExpressionSyntax)

        Assert.True(exp.ContainsDiagnostics)
        'ERRID_NullableCharNotSupported = 30944
        Assert.True(exp.CommaToken.IsMissing, "IsMissing Not True")
        Assert.True(exp.CommaToken.ContainsDiagnostics, "ContainsDiagnostics Not True")
        Assert.Equal(30944, exp.CommaToken.GetSyntaxErrorsNoTree(0).Code)
    End Sub

    <Fact>
    Public Sub BC30198ERR_ExpectedRparen_ErrorTest()
        Dim exp As GenericNameSyntax = DirectCast(ParseExpressionAsRhs("B(of a b)", True), GenericNameSyntax)

        Assert.True(exp.ContainsDiagnostics)
        Assert.Equal(1, exp.GetSyntaxErrorsNoTree.Count)
        'ERRID_ExpectedRparen = 30198
        Assert.Equal(30198, exp.GetSyntaxErrorsNoTree(0).Code)
        Assert.True(exp.TypeArgumentList.CloseParenToken.Span.IsEmpty)
    End Sub

#End Region

    <Fact>
    Public Sub ParseTypeOf()
        ParseExpression("TypeOf a is b")
        ParseExpression(<Text>TypeOf a is 
                                       b</Text>)
    End Sub

    <Fact>
    Public Sub ParseGetType()
        ParseExpression("gettype(a)")
        ParseExpression("gettype(a(of ))")
        ParseExpression("gettype(a(of b))")
        ParseExpression("gettype(a(of ,))")
        ParseExpression("gettype(a(of ,,))")
        ParseExpression(<text>gettype(a(of ,,)
                                        )</text>)
        'TODO -shiqic enable the following test when Qualified type name can be parsed
        'ParseExpressionTestHelper("gettype(a.b)")
    End Sub

    <Fact>
    Public Sub ParseCast()
        ParseExpression("CType( a, b )")
        ParseExpression("DirectCast( a, b )")
        ParseExpression("TryCast( a, b )")

        ParseExpression(<Text>TryCast( a, b 
                                        )</Text>)
    End Sub

    <Fact>
    Public Sub ParseQualified()
        ParseExpression("a.b")
        ParseExpression(" a.b ")
        ParseExpression(" a . b ")
        ParseExpression("a!b")
        ParseExpression("a.b.c.d")
        ParseExpression(<t>A.
b</t>)
    End Sub

    <Fact>
    Public Sub ParseSpecialBase()
        Dim expr = ParseExpression("MyBase.b")
        Assert.Equal(SyntaxKind.MyBaseExpression, expr.ChildNodesAndTokens()(0).Kind())
        expr = ParseExpression("MyClass.b")
        Assert.Equal(SyntaxKind.MyClassExpression, expr.ChildNodesAndTokens()(0).Kind())
        expr = ParseExpression("Global.b")
        Assert.Equal(SyntaxKind.GlobalName, expr.ChildNodesAndTokens()(0).Kind())

        expr = ParseExpression("Me")
        Assert.Equal(SyntaxKind.MeExpression, expr.Kind)
        expr = ParseExpression("Me.b")
        Assert.Equal(SyntaxKind.MeExpression, expr.ChildNodesAndTokens()(0).Kind())
    End Sub

    <Fact>
    Public Sub ParseParenthesized()
        Dim expr = ParseExpression("(A)")
        Assert.Equal(SyntaxKind.ParenthesizedExpression, expr.Kind)
        expr = ParseExpression("((A)).B")
        Assert.Equal(SyntaxKind.ParenthesizedExpression, expr.ChildNodesAndTokens()(0).Kind())
        Assert.Equal(SyntaxKind.ParenthesizedExpression, expr.ChildNodesAndTokens()(0).ChildNodesAndTokens()(1).Kind())

        expr = ParseExpression(<![CDATA[
(
(
42
)
).
ToString]]>.Value)

        Assert.Equal(SyntaxKind.ParenthesizedExpression, expr.ChildNodesAndTokens()(0).Kind())
        Assert.Equal(SyntaxKind.ParenthesizedExpression, expr.ChildNodesAndTokens()(0).ChildNodesAndTokens()(1).Kind())
    End Sub

    <Fact>
    Public Sub ParseIIF()
        Dim expr = ParseExpression("if(true,A,B)")
        Assert.Equal(SyntaxKind.TernaryConditionalExpression, expr.Kind)
        expr = ParseExpression("if ( A , B )")
        Assert.Equal(SyntaxKind.BinaryConditionalExpression, expr.Kind)
        expr = ParseExpression("if    (" & vbCrLf & "A,B)")
        Assert.Equal(SyntaxKind.BinaryConditionalExpression, expr.Kind)
    End Sub

    <Fact>
    Public Sub ParseNew()
        Dim expr = ParseExpression("New Boo(1,2)")
        Assert.Equal(SyntaxKind.ObjectCreationExpression, expr.Kind)

        expr = ParseExpression("New Moo(1){}")
        Assert.Equal(SyntaxKind.ArrayCreationExpression, expr.Kind)

        expr = ParseExpression("New Moo()(){{1},{2}}")
        Assert.Equal(SyntaxKind.ArrayCreationExpression, expr.Kind)

        expr = ParseExpression("New Moo(1)(){}")
        Assert.Equal(SyntaxKind.ArrayCreationExpression, expr.Kind)

        expr = ParseExpression("New Moo() With{.x= 42}")
        Assert.Equal(SyntaxKind.ObjectCreationExpression, expr.Kind)

        expr = ParseExpression("New With{key .x= 42}")
        Assert.Equal(SyntaxKind.AnonymousObjectCreationExpression, expr.Kind)

        expr = ParseExpression("New Moo() From{1,2,3}")
        Assert.Equal(SyntaxKind.ObjectCreationExpression, expr.Kind)
    End Sub

    <Fact>
    Public Sub ParseBuiltInCasts()
        Dim expr = ParseExpression("CObj(123)")
        Assert.Equal(SyntaxKind.PredefinedCastExpression, expr.Kind)
        Assert.Equal(SyntaxKind.CObjKeyword, DirectCast(expr, PredefinedCastExpressionSyntax).Keyword.Kind)

        expr = ParseExpression("CStr(aa)")
        Assert.Equal(SyntaxKind.PredefinedCastExpression, expr.Kind)
        Assert.Equal(SyntaxKind.CStrKeyword, DirectCast(expr, PredefinedCastExpressionSyntax).Keyword.Kind)

        expr = ParseExpression("CUint(aa)")
        Assert.Equal(SyntaxKind.PredefinedCastExpression, expr.Kind)
        Assert.Equal(SyntaxKind.CUIntKeyword, DirectCast(expr, PredefinedCastExpressionSyntax).Keyword.Kind)

    End Sub

    <Fact>
    Public Sub ParseBuiltInTyped()
        Dim expr = ParseExpression("Integer.MaxValue")
        Assert.Equal(SyntaxKind.SimpleMemberAccessExpression, expr.Kind)
        Assert.Equal(SyntaxKind.PredefinedType, DirectCast(expr, MemberAccessExpressionSyntax).Expression.Kind)

        expr = ParseExpression("UShort.ToString()")
        Assert.Equal(SyntaxKind.InvocationExpression, expr.Kind)
        Assert.Equal(SyntaxKind.SimpleMemberAccessExpression, DirectCast(expr, InvocationExpressionSyntax).Expression.Kind)
        Assert.Equal(SyntaxKind.PredefinedType, DirectCast(DirectCast(expr, InvocationExpressionSyntax).Expression, MemberAccessExpressionSyntax).Expression.Kind)
    End Sub

    <Fact>
    Public Sub Invocation()
        Dim expr = ParseExpression("Blah()")
        Assert.Equal(SyntaxKind.InvocationExpression, expr.Kind)

        expr = ParseExpression("Boo(1,2,3)")
        Assert.Equal(SyntaxKind.InvocationExpression, expr.Kind)

        expr = ParseExpression("Boo(1,,3)")
        Assert.Equal(SyntaxKind.InvocationExpression, expr.Kind)

        expr = ParseExpression("Boo(1,2, x:=3)")
        Assert.Equal(SyntaxKind.InvocationExpression, expr.Kind)

        expr = ParseExpression("Boo(1,2, x:=3)(ha)")
        Assert.Equal(SyntaxKind.InvocationExpression, expr.Kind)
    End Sub


    <Fact>
    Public Sub FromQueryClause()
        Dim expr = ParseExpression("From x in y")
        Assert.Equal(SyntaxKind.FromClause, GetOperator(expr, 0).Kind)

        expr = ParseExpression("From x as integer in Blah")
        Assert.Equal(SyntaxKind.FromClause, GetOperator(expr, 0).Kind)

        expr = ParseExpression("From x in y, z in a")
        Assert.Equal(SyntaxKind.FromClause, GetOperator(expr, 0).Kind)

        Dim x =
            From
k As Char
In
"qqq",
z
In
"qqqq"

        expr = ParseExpression(
<Q>
            From
k As Char
In
"qqq",
z
In
"qqqq"</Q>)
        Assert.Equal(SyntaxKind.FromClause, GetOperator(expr, 0).Kind)
    End Sub

    <Fact>
    Public Sub Regression001()
        ' Bug 863001
        Dim expr = ParseExpression("&H55 << 2")
        Assert.Equal(SyntaxKind.LeftShiftExpression, expr.Kind)

        ' Bug 863525
        expr = ParseExpression("ÛÊÛÄÁÍäá")
        Assert.Equal(SyntaxKind.IdentifierName, expr.Kind)

        expr = ParseExpression(ChrW(&HDB) & ChrW(&HCA) & ChrW(&HDB) & ChrW(&HC4) & ChrW(&HC1) & ChrW(&HCD) & ChrW(&HE4) & ChrW(&HE1))
        Assert.Equal(SyntaxKind.IdentifierName, expr.Kind)

    End Sub

    <Fact>
    Public Sub CrossJoin()
        Dim expr = ParseExpression("From x in y From y in z")
        Assert.Equal(SyntaxKind.FromClause, GetOperator(expr, 1).Kind)

        expr = ParseExpression("From x in y Let a = 2")
        Assert.Equal(SyntaxKind.LetClause, GetOperator(expr, 1).Kind)

        expr = ParseExpression(
<Q>
From x as integer in Blah
From x as integer in Blah
</Q>)
        Assert.Equal(SyntaxKind.FromClause, GetOperator(expr, 1).Kind)

        expr = ParseExpression(
<Q>
From x as integer in Blah
From x as integer in Blah, x in Boo
</Q>)
        Assert.Equal(SyntaxKind.FromClause, GetOperator(expr, 1).Kind)

        expr = ParseExpression(
<Q>
From x as integer in Blah
From x as integer in Blah, x in Boo
Let y = 1
Let yy as String = "qqq"
</Q>)
        Assert.Equal(SyntaxKind.LetClause, GetOperator(expr, 2).Kind)

        expr = ParseExpression(
<Q>
From x as integer in Blah
From x as integer in Blah, x in Boo
Let y = 1, hh as integer = 3 Let yy as String = "qqq"
From a in B
</Q>)
        Assert.Equal(SyntaxKind.FromClause, GetOperator(expr, 4).Kind)

    End Sub

    <Fact>
    Public Sub Take()
        Dim expr = ParseExpression("From x in y Take 2")
        Assert.Equal(SyntaxKind.TakeClause, GetOperator(expr, 1).Kind)

        expr = ParseExpression("From x in y Take While true")
        Assert.Equal(SyntaxKind.TakeWhileClause, GetOperator(expr, 1).Kind)

        expr = ParseExpression(
<Q>
        From x as integer in Blah
        Take
        2
        </Q>)
        Assert.Equal(SyntaxKind.TakeClause, GetOperator(expr, 1).Kind)

        expr = ParseExpression(
<Q>
        From x as integer in Blah
        Take While
        true
        From x as integer in Blah, x in Boo
        </Q>)
        Assert.Equal(SyntaxKind.FromClause, GetOperator(expr, 2).Kind)

    End Sub

    <Fact>
    Public Sub Skip()
        Dim expr = ParseExpression("From x in y Skip 2")
        Assert.Equal(SyntaxKind.SkipClause, GetOperator(expr, 1).Kind)

        expr = ParseExpression("From x in y Skip While true")
        Assert.Equal(SyntaxKind.SkipWhileClause, GetOperator(expr, 1).Kind)

        expr = ParseExpression(
<Q>
        From x as integer in Blah
        Skip
        2
        Skip While
        true
        </Q>)
        Assert.Equal(SyntaxKind.SkipWhileClause, GetOperator(expr, 2).Kind)

        expr = ParseExpression(
<Q>
        From x as integer in Blah
        Skip While
        true
        Skip 2
        </Q>)
        Assert.Equal(SyntaxKind.SkipClause, GetOperator(expr, 2).Kind)

    End Sub

    <Fact>
    Public Sub Distinct()
        Dim expr = ParseExpression("From x in y Distinct")
        Assert.Equal(SyntaxKind.DistinctClause, GetOperator(expr, 1).Kind)

        expr = ParseExpression("From x in y Distinct Skip While true")
        Assert.Equal(SyntaxKind.SkipWhileClause, GetOperator(expr, 2).Kind)

        expr = ParseExpression(
<Q>
        From x as integer in Blah
        Distinct
        Distinct
        </Q>)
        Assert.Equal(SyntaxKind.DistinctClause, GetOperator(expr, 2).Kind)

        expr = ParseExpression(
<Q>
        From x as integer in Blah
        Distinct
        Skip 2
        </Q>)
        Assert.Equal(SyntaxKind.SkipClause, GetOperator(expr, 2).Kind)

    End Sub

    <Fact>
    Public Sub Aggregate()
        'Dim qq = Aggregate x In "qq" Into s = Count()
        Dim expr = ParseExpression("Aggregate x In ""qq"" Into s = Count")
        Assert.Equal(SyntaxKind.AggregateClause, GetOperator(expr, 0).Kind)

        expr = ParseExpression("Aggregate x in y Into Sum ( 10 ), Any()")
        Assert.Equal(SyntaxKind.AggregateClause, GetOperator(expr, 0).Kind)

        expr = ParseExpression("Aggregate x In ""qqq"" Into Sum(10), Any(), x = Count")
        Assert.Equal(SyntaxKind.AggregateClause, GetOperator(expr, 0).Kind)

        expr = ParseExpression(
<Q>
        From x as integer in Blah
        Aggregate x In "qqq" Into Sum(10), Any(), x = Count
        Skip 2
        </Q>)
        Assert.Equal(SyntaxKind.SkipClause, GetOperator(expr, 2).Kind)

        expr = ParseExpression(
<Q>
From x as integer in Blah
Aggregate x 
    In 
    "qqq" 
    Into 
    Sum(10), 
    Any(), 
    x = Count
Skip 2
        </Q>)
        Assert.Equal(SyntaxKind.SkipClause, GetOperator(expr, 2).Kind)

    End Sub

    <Fact>
    Public Sub OrderBy()
        Dim expr = ParseExpression("From x in y Order By x")
        Assert.Equal(SyntaxKind.OrderByClause, GetOperator(expr, 1).Kind)

        expr = ParseExpression("From x in y Order By x, y")
        Assert.Equal(SyntaxKind.OrderByClause, GetOperator(expr, 1).Kind)

        expr = ParseExpression("From x in y Order By x Ascending, y Descending")
        Assert.Equal(SyntaxKind.OrderByClause, GetOperator(expr, 1).Kind)

        expr = ParseExpression(
<Q>
From x as integer in Blah
Order By
x 
Ascending,
y Descending
Skip 2
        </Q>)
        Assert.Equal(SyntaxKind.SkipClause, GetOperator(expr, 2).Kind)

    End Sub

    Private Function GetOperator(expr As ExpressionSyntax, num As Integer) As QueryClauseSyntax
        Return DirectCast(expr, QueryExpressionSyntax).Clauses(num)
    End Function

    <Fact>
    Public Sub [Select]()
        Dim expr = ParseExpression("From x In ""qq"" Select s = Count")
        Assert.Equal(SyntaxKind.SelectClause, GetOperator(expr, 1).Kind)

        expr = ParseExpression("From x in y Select Sum ( 10 ), Any()")
        Assert.Equal(SyntaxKind.SelectClause, GetOperator(expr, 1).Kind)

        expr = ParseExpression("From x In ""qqq"" Select Sum(10), Any(), x = Count")
        Assert.Equal(SyntaxKind.SelectClause, GetOperator(expr, 1).Kind)

        expr = ParseExpression(
<Q>
                From x as integer in Blah
                From x In "qqq" Select Sum(10), Any(), x = Count
                Skip 2
                </Q>)
        Assert.Equal(SyntaxKind.SkipClause, GetOperator(expr, 3).Kind)

        expr = ParseExpression(
<Q>
        From x as integer in Blah
        From x 
            In 
            "qqq" 
            Select 
            Sum(10), 
            Any(), 
            x = Count
        Skip 2
                </Q>)
        Assert.Equal(SyntaxKind.SkipClause, GetOperator(expr, 3).Kind)

    End Sub

    <Fact>
    Public Sub GroupBy()
        Dim expr = ParseExpression("From x In ""qq"" Group By x Into Group")
        Assert.Equal(SyntaxKind.GroupByClause, GetOperator(expr, 1).Kind)

        expr = ParseExpression("From x In ""qq"" Let y = 2 Group x, y By x Into Group")
        Assert.Equal(SyntaxKind.GroupByClause, GetOperator(expr, 2).Kind)

        expr = ParseExpression("From x In ""qq"" Group x By x Into a = Any(), Group, Count")
        Assert.Equal(SyntaxKind.GroupByClause, GetOperator(expr, 1).Kind)

        expr = ParseExpression(<Q>
        From x As Char In "Blah"
                Group x, y
                By
                x,
                y
                Into
                Sum(10),
                Any(),
                e = Count()
                Skip 2
                </Q>)

        Assert.Equal(SyntaxKind.SkipClause, GetOperator(expr, 2).Kind)

        expr = ParseExpression(<Q>
        From x As Char In "Blah"
                Group
                By
                x,
                y
                Into
                Sum(10),
                Any(),
                e = Count
        </Q>)

        Assert.Equal(SyntaxKind.GroupByClause, GetOperator(expr, 1).Kind)

    End Sub

    <Fact>
    Public Sub Join()

        Dim expr = ParseExpression("From x In ""qq"" Join y In ""www"" On x Equals y")
        Assert.Equal(SyntaxKind.SimpleJoinClause, GetOperator(expr, 1).Kind)

        expr = ParseExpression("From x In ""qq"" Join y In ""www"" On x Equals y And x Equals y")
        Assert.Equal(SyntaxKind.SimpleJoinClause, GetOperator(expr, 1).Kind)

        expr = ParseExpression("From x In ""qq"" Join y In ""www"" Join y in Blah On x Equals y And x Equals y On 1 Equals 2 And 3 Equals 4")
        Assert.Equal(SyntaxKind.SimpleJoinClause, GetOperator(expr, 1).Kind)

        expr = ParseExpression(<Q>
                From x In "qq"
                Join
                   y
                   In
                   "www"
                   Join
                   z
                   In
                   {1, 2}
                   On
                   y Equals
                   z.
                   ToString And
                   AscW(y) Equals
                   z
                   On
                   x Equals
                   y And AscW(x) Equals
                   z
                Skip 2
                </Q>)

        Assert.Equal(SyntaxKind.SkipClause, GetOperator(expr, 2).Kind)

        expr = ParseExpression(<Q>
                From x In "qq"
                Join
                   y
                   In
                   "www"
                   Join
                   z
                   In
                   {1, 2}
                   On
                   y Equals
                   z.
                   ToString And
                   AscW(y) Equals
                   z
                   On
                   x Equals
                   y And AscW(x) Equals
                   z
                </Q>)

        Assert.Equal(SyntaxKind.SimpleJoinClause, GetOperator(expr, 1).Kind)
    End Sub

    <WorkItem(542686, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542686")>
    <Fact>
    Public Sub RangeVariableNamedFrom()
        Dim expr = ParseExpression("From From In ""qq"" Join y In ""www"" On From Equals y")
        Assert.Equal(SyntaxKind.SimpleJoinClause, GetOperator(expr, 1).Kind)
    End Sub

    <Fact>
    Public Sub GroupJoin()
        Dim expr = ParseExpression("From x In ""qq"" Group Join y In ""www"" On x Equals y Into Count()")
        Assert.Equal(SyntaxKind.GroupJoinClause, GetOperator(expr, 1).Kind)

        expr = ParseExpression("From x In ""qq"" Group Join y In ""www"" On x Equals y Into Group, Count")
        Assert.Equal(SyntaxKind.GroupJoinClause, GetOperator(expr, 1).Kind)

        expr = ParseExpression("From x In {1, 2} Group Join y In {1, 2} On x Equals y And x + 1 Equals y + 1 Into g = Group, m = Max(x)")
        Assert.Equal(SyntaxKind.GroupJoinClause, GetOperator(expr, 1).Kind)

        expr = ParseExpression(<Q>
                From x In {1, 2}
                  Group Join
                  y
                  In
                  {1, 2}
                  On
                  x Equals
                  y And
                  x + 1 Equals
                  y + 1
                  Into
                  g =
                  Group,
                  m =
                  Max(x)
                Skip 2
                </Q>)

        Assert.Equal(SyntaxKind.SkipClause, GetOperator(expr, 2).Kind)

        expr = ParseExpression(<Q>
                From x In {1, 2}
                  Join z In {1, 2}
                  Group Join
                  y
                  In
                  {1, 2}
                  On
                  z Equals
                  y And
                  z + 1 Equals
                  y + 1
                  Into
                  g =
                  Group,
                  m =
                  Max(z)
                  On
                  x Equals
                  z And
                  g.Count Equals
                  x
                </Q>)

        Assert.Equal(SyntaxKind.SimpleJoinClause, GetOperator(expr, 1).Kind)
    End Sub

    <Fact>
    Public Sub Bug866481()
        Dim expr = ParseExpression("1 \ 3 'constant with init expression")
        Assert.Equal(SyntaxKind.IntegerDivideExpression, expr.Kind)
    End Sub

    <Fact>
    Public Sub Bug867026()
        Dim expr = ParseExpression("GetType(Integer) IsNot GetType(Short)")
        Assert.Equal(SyntaxKind.IsNotExpression, expr.Kind)
    End Sub

    <Fact>
    Public Sub Bug868395()
        Dim expr = ParseExpression("SByte.MinValue")
        Assert.Equal(SyntaxKind.SimpleMemberAccessExpression, expr.Kind)
    End Sub

    <Fact>
    Public Sub Bug869112()
        Dim code = <![CDATA[
            Enum enum1
                e1
                e2
                e3
                e4
            End Enum
            Class myattr9
                Inherits Attribute
         
                Sub New(ByVal o() As enum1)
                End Sub
         
                Property prop() As enum1()
         
                Public o() As enum1
                Private p() As enum1
            End Class
            <myattr9(New enum1(0 To 3) {enum1.e1, enum1.e3, enum1.e2, enum1.e4}, prop:=New enum1(0 To 3) {enum1.e1, enum1.e3, enum1.e2, enum1.e4})> Class Scen23
            End Class
        ]]>

        Dim tree = SyntaxFactory.ParseCompilationUnit(code.Value)
        Assert.Equal(code.Value, tree.ToFullString)
        Dim errors As String = ""
        For Each e In tree.GetSyntaxErrorsNoTree()
            Dim span = e.Location.SourceSpan
            errors &= e.Code & " : " & e.GetMessage(CultureInfo.GetCultureInfo("en")) & " (" & span.Start & ", " & span.End & ")" & Environment.NewLine
        Next
        Assert.False(tree.ContainsDiagnostics, errors)

    End Sub

    <Fact>
    Public Sub Bug874435()
        Dim expr = ParseExpression("<ns:e> a </ns:e>")
        Dim el = DirectCast(expr, XmlElementSyntax)

        Assert.Equal(1, el.Content.Count)
        Assert.Equal(" a ", el.Content.First.ToString())
        Assert.Equal(" a ", el.Content.First.ChildNodesAndTokens().First.ToFullString())
    End Sub

    <Fact>
    Public Sub XmlTextList()
        Dim expr = ParseExpression("<ns:e> a &lt; b </ns:e>")
        Dim el = DirectCast(expr, XmlElementSyntax)

        Assert.Equal(1, el.Content.Count)
        Assert.Equal(" a &lt; b ", el.Content.First.ToString)
        Assert.Equal(" a ", el.Content.First.ChildNodesAndTokens().First.ToFullString())
        Assert.Equal("<", el.Content.First.ChildNodesAndTokens(1).AsToken.ValueText)
        Assert.Equal(" b ", el.Content.First.ChildNodesAndTokens().Last.ToFullString())
    End Sub

    <Fact>
    Public Sub TestSingleLineLambdaFunction()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim a = function() 1
                End Sub
            End Module
        ]]>)
    End Sub

    <Fact>
    Public Sub TestSingleLineLambdaSub()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim a = sub() console.WriteLine("hello world")
                End Sub
            End Module
        ]]>)
    End Sub

    <Fact>
    Public Sub BC30088ERR_EndSelectNoSelect_TestSingleLineLambdaFunctionWithColon()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()

                    Dim a =  sub () select case 0 : case 0 : end select

                End Sub
            End Module
        ]]>,
        Diagnostic(ERRID.ERR_SubRequiresSingleStatement, "sub () select case 0 : case 0 : end select"))
    End Sub


    <Fact>
    Public Sub TestLambdaInModule1()
        ParseAndVerify(<![CDATA[
           Module Module1

                Dim y = Sub ()
                        Console.WriteLine("hello world")
                    End Sub

           End Module
        ]]>)
    End Sub

    <Fact>
    Public Sub TestEmptyFunctionLambdaNewLineTriviaAdjustment1()
        ' Ensure that new line trivia is attached correctly
        Dim tree = ParseAndVerify(<![CDATA[
           Module Module1

                Dim y = Function() :

           End Module
        ]]>, Diagnostic(ERRID.ERR_ExpectedExpression, ""))
        Dim root = tree.GetRoot()
        Dim module1 = DirectCast(root.ChildNodes(0), ModuleBlockSyntax)
        Dim stmt = module1.Members(0)
        Dim lastToken = stmt.GetLastToken(includeZeroWidth:=True)
        ' Colon trivia should be attached to missing identifier token
        Assert.True(lastToken.IsMissing)
        Assert.Equal(SyntaxKind.IdentifierToken, lastToken.Kind)
        Assert.Equal(lastToken.TrailingTrivia(0).Kind, SyntaxKind.ColonTrivia)
    End Sub

    <Fact>
    Public Sub TestEmptyFunctionLambdaNewLineTriviaAdjustment2()
        ' Ensure that new line trivia is attached correctly
        Dim tree = ParseAndVerify(<![CDATA[
           Module Module1

                Dim y = Function() 


        ]]>,
        Diagnostic(ERRID.ERR_ExpectedEndModule, "Module Module1"),
        Diagnostic(ERRID.ERR_MultilineLambdaMissingFunction, "Function()"))

        Dim root = tree.GetRoot()
        Dim module1 = DirectCast(root.ChildNodes(0), ModuleBlockSyntax)
        Dim stmt = module1.Members(0)
        Dim lastToken = stmt.GetLastToken()

        ' new line trivia should be attached to closing paren
        Assert.False(lastToken.IsMissing)
        Assert.Equal(SyntaxKind.CloseParenToken, lastToken.Kind)
        Assert.Equal(lastToken.TrailingTrivia(0).Kind, SyntaxKind.WhitespaceTrivia)
    End Sub

    <Fact>
    Public Sub TestLambdaStatementFollowedByColonAndLabel()
        Dim tree = ParseAndVerify(<![CDATA[
        Module Program
              Sub Main()
                  Dim x = Sub()
                              GoTo label1 :
                              label1:
                          End Sub
              End Sub
        End Module
        ]]>)

        Dim root = tree.GetRoot()
        Dim module1 = DirectCast(root.ChildNodes(0), ModuleBlockSyntax)
        Dim main = DirectCast(module1.Members(0), MethodBlockSyntax)
        Dim localDecl = main.Statements(0)
        Dim subKeyword = localDecl.GetLastToken()
        Dim lambda = DirectCast(subKeyword.Parent.Parent, MultiLineLambdaExpressionSyntax)
        Dim gotoStmt = lambda.Statements(0)
        Dim labelStmt = lambda.Statements(1)
        Assert.Equal(SyntaxKind.GoToStatement, gotoStmt.Kind)
        Assert.Equal(SyntaxKind.LabelStatement, labelStmt.Kind)
    End Sub

    <Fact>
    Public Sub TestLambdaWithEmptySingleLineIf()
        Dim tree = ParseAndVerify(<![CDATA[
        Module Program
              Sub Main()
                  Dim x = Sub()
                              if true then else
                          End Sub
              End Sub
        End Module
        ]]>)
    End Sub

    <WorkItem(537571, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537571")>
    <Fact>
    Public Sub TestLambdaInModule2()
        ' Test that a declaration with an attribute terminates the lambda.
        ParseAndVerify(<![CDATA[
           Module Module1
            
            Dim method4ParamTypes = m4.Parameters.Select(Function(p) 
                return p
            <Fact>
        Public Sub Fields() 

           End Module
        ]]>,
         <errors>
             <error id="36674"/>
             <error id="30198"/>
             <error id="30026"/>
         </errors>)
    End Sub


    <Fact>
    Public Sub TestMultiLineLambdaSub()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim a = sub()
                        console.WriteLine("hello world")
                    end sub                  
                End Sub
            End Module
        ]]>)
    End Sub

    <Fact>
    Public Sub TestMultiLineLambdaSubNoEndSub1()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    do 
                        Dim a = sub()
                            console.WriteLine("hello world")
                    loop                 
                End Sub
            End Module
        ]]>, <errors>
                 <error id="36673"/>
             </errors>)
    End Sub

    <Fact>
    Public Sub TestMultiLineLambdaSubNoEndSub2()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    do 
                        Dim a = sub()
                            if true then
                                    console.WriteLine("hello world")
                    loop                 
                End Sub
            End Module
        ]]>, <errors>
                 <error id="36673"/>
                 <error id="30081"/>
             </errors>)
    End Sub


    <Fact>
    Public Sub TestMultiLineLambdaFunction()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim a = function(i as integer, j as integer) as integer
                        dim x = i + j
                        return x
                    end function                
                End Sub
            End Module
        ]]>)
    End Sub

    <Fact>
    Public Sub TestMultiLineLambdaFunctionNoEndFunction()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim a = function(i as integer, j as integer) as integer
                        dim x = i + j
                        return x
                                
                End Sub
            End Module
        ]]>,
        <errors>
            <error id="36674"/>
        </errors>)
    End Sub

    <Fact>
    Public Sub BC36677ERR_AttributeOnLambdaReturnType_TestMultiLineLambdaFunctionWithAttribute()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim a = function(i as integer, j as integer) as <clscompliant(true)> integer
                        dim x = i + j
                        return x
                    end function
                                
                End Sub
            End Module
        ]]>,
        <errors>
            <error id="36677"/>
        </errors>)
    End Sub


    <Fact>
    Public Sub TestMultiLineLambdaFunctionClosedWithEndModule()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim a = function(i as integer, j as integer) as integer
                        dim x = i + j
                        return x
                End Module
        ]]>,
        <errors>
            <error id="36674"/>
            <error id="30026"/>
        </errors>)
    End Sub

    <Fact>
    Public Sub TestMultiLineLambdaFunctionClosedWithFunctionDeclaration()
        ParseAndVerify(<![CDATA[
                Module Module1
                    Sub Main()

                        Dim a = function(i as integer, j as integer)
                                    Return i + j
                           
                function f
                end function

                End Module
        ]]>,
        <errors>
            <error id="36674"/>
            <error id="30026"/>
            <error id="30289"/>
        </errors>)
    End Sub

    <Fact>
    Public Sub TestMultiLineLambdaFunctionWithOutBody()
        ParseAndVerify(<![CDATA[
                Module Module1
                    Sub Main()
                        Dim a = function(i as integer, j as integer)
                End Module
        ]]>, <errors>
                 <error id="36674"/>
                 <error id="30026"/>
             </errors>)
    End Sub


    <Fact>
    Public Sub TestMultiLineLambdaFunctionWithOutBody1()
        ParseAndVerify(<![CDATA[
            Module m1
                Sub Main()
                    Dim s6 = Function (x) 
                End Sub
            End Module
        ]]>, <errors>
                 <error id="36674"/>
             </errors>)
    End Sub

    <Fact>
    Public Sub TestMultiLineLambdaSubWithOutStatement()
        ParseAndVerify(<![CDATA[
                Module Module1
                    Sub Main()
                        Dim a = sub(i as integer, j as integer)
                End Module
        ]]>, <errors>
                 <error id="36673"/>
                 <error id="30026"/>
             </errors>)
    End Sub

    <Fact>
    Public Sub TestMultiLineLambdaFunctionWithEmptyStatementBody()
        ParseAndVerify(<![CDATA[
                Module Module1
                    Sub Main()
                        Dim a = function(i as integer, j as integer)

                    End Sub
                End Module
        ]]>,
        <errors>
            <error id="36674"/>
        </errors>)
    End Sub

    <WorkItem(880474, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseBadRelationalOperators()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim x = 1 >< 2
                    dim x = 1 => 2
                    dim x = 1 =< 2
                End Sub
            End Module
        ]]>,
        <errors>
            <error id="30239"/>
            <error id="30239"/>
            <error id="30239"/>
        </errors>)
    End Sub

    <WorkItem(794247, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseBadlyTerminatedLambdaScenario1()
        ParseAndVerify(<![CDATA[
            module A

            dim x = function() as integer
                with new object()
                    return nothing
                end sub

            end module
        ]]>,
        <errors>
            <error id="30429"/>
            <error id="30085"/>
            <error id="36674"/>
        </errors>)
    End Sub


    <Fact>
    Public Sub ParseBadlyTerminatedLambdaScenario2()
        ParseAndVerify(<![CDATA[
            module BBB

            dim x = Sub()
            with new object()
            end fu

            end module
        ]]>, <errors>
                 <error id="36673"/>
                 <error id="30085"/>
                 <error id="30678"/>
             </errors>
        )
    End Sub

    <Fact>
    Public Sub ParseBadlyTerminatedLambdaScenario3()
        ParseAndVerify(<![CDATA[
            module BBB

                Sub Foo
                    if True
                        dim x=Sub()

                    End if
                end Sub

            end module
        ]]>,
        <errors>
            <error id="36673"/>
        </errors>)
    End Sub

    <Fact>
    Public Sub BC30085ERR_ExpectedEndWith_ParseBadlyTerminatedLambdaScenario4()
        ParseAndVerify(<![CDATA[
            module BBB

                Sub Foo
                    if True
                        dim x=Sub()
                            With new Object()

                    End if
                end Sub

            end module
        ]]>,
        <errors>
            <error id="30085"/>
            <error id="36673"/>
        </errors>)
    End Sub

    <Fact>
    Public Sub ParseBadlyTerminatedLambdaScenario5()
        ParseAndVerify(<![CDATA[
            module BBB

                Sub Foo
                        dim x=Function()
                            With new Object()
                end Sub

            end module
        ]]>,
        <errors>
            <error id="30085"/>
            <error id="36674"/>
        </errors>)
    End Sub

    <Fact>
    Public Sub ParseLambdaScenario6()
        ParseAndVerify(<![CDATA[
            module BBB

                Sub Foo
                        dim x=Sub() if true then while true : Dim z=Function()

                                                              End Function : end while
                        end sub
            end module
        ]]>)
    End Sub

    <Fact>
    Public Sub ParseBadlyTerminateLambdaScenario6MissingEndSub()
        ParseAndVerify(<![CDATA[
            module BBB

                Sub Foo
                        dim x=Sub() if true then while true : Dim z=Function()

                                                              End Function : end while
            end module
        ]]>,
        <errors>
            <error id="30026"/>
        </errors>)
    End Sub

    <Fact>
    Public Sub BC30090ERR_EndWhileNoWhile_ParseBadlyTerminateLambdaScenario6ExtraEndWhile()
        ParseAndVerify(<![CDATA[
            module BBB

                Sub Foo
                        dim x=Sub() if true then while true : Dim z=Function()
                                                                end while
                                                              End Function : end while
            end module
        ]]>,
        <errors>
            <error id="30026"/>
            <error id="30082"/>
            <error id="36674"/>
            <error id="30090"/>
            <error id="30430"/>
            <error id="30090"/>
        </errors>)
    End Sub

    <Fact>
    Public Sub ParseBadlyTerminateLambdaScenario7()
        ParseAndVerify(<![CDATA[
            module BBB
                        dim x=Sub() call Sub()
                                       synclock x
            end module
        ]]>,
        <errors>
            <error id="30675"/>
            <error id="36673 "/>
        </errors>)
    End Sub

    <Fact>
    Public Sub ParseBadlyTerminateLambdaScenario8()
        ParseAndVerify(<![CDATA[
            module BBB
                friend e = Foo(Function()
                                if true then
                                    dim x=Sub() Call Sub()
                                                    end if
                                                end sub
                                end function)
            end module
        ]]>,
        <errors>
            <error id="30429"/>
            <error id="36673"/>
        </errors>)
    End Sub

    <Fact>
    Public Sub ParseBadlyTerminateLambdaScenario9()
        ParseAndVerify(<![CDATA[
            module BBB
                friend e = Function()
                                if true then
                                    Dim x=Sub() End : End if
                                end function
            end module
        ]]>,
        <errors>
            <error id="36918"/>
        </errors>)
    End Sub

    <Fact>
    Public Sub ParseBadlyTerminateLambdaScenario10()
        ParseAndVerify(<![CDATA[
            module BBB
                Sub Foo()
                  Dim x  = Function()
                End Sub
            end module
        ]]>,
            <errors>
                <error id="36674"/>
            </errors>)
    End Sub

    <WorkItem(890852, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseBadlyTerminateLambdaScenario11()
        ParseAndVerify(<![CDATA[
            Module foo    
                sub main     
                    Dim x = function         
                End sub    
            End Module
        ]]>,
        <errors>
            <error id="30198"/>
            <error id="30199"/>
            <error id="36674"/>
        </errors>)
    End Sub

    <Fact>
    Public Sub ParseSingleLineLambdaWithSingleLineIf()
        ParseAndVerify(<![CDATA[
            module m1
                Sub Foo
                        dim x=Sub() if true then console.writeline
                end sub
            end module
        ]]>)
    End Sub

    <Fact>
    Public Sub ParseSingleLineLambdaWithMultipleStatements()
        ParseAndVerify(<![CDATA[
            module m1
                Sub Foo
                        dim x=Sub() console.writeline : console.writeline : end sub
                end sub
            end module
        ]]>,
        <errors>
            <error id="30429"/>
            <error id="36918"/>
        </errors>)
    End Sub

    <Fact>
    Public Sub ParseSingleLineLambdaEndingWithColon()
        ParseAndVerify(<![CDATA[
            module m1
                Sub Foo
                        dim x=Sub() console.writeline : console() :
                end sub
            end module
        ]]>,
        <errors>
            <error id="36918"/>
        </errors>)
    End Sub

    <WorkItem(887486, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseTryCastErrorExpectedRparen()
        ParseAndVerify(<![CDATA[
                Module Module1
                  Sub Foo()
                   Dim o As Object
                   TryCast(o,Nothing)
                  End Sub
                End Module
            ]]>,
            <errors>
                <error id="30198"/>
                <error id="30180"/>
            </errors>)
    End Sub

    <WorkItem(888998, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseMoreErrorsMultilineLambdaMissingFunctionAndSyntax()
        ParseAndVerify(<![CDATA[
                      #const COMPERRORTEST = true
                      Class Class1
                       #If COMPERRORTEST Then
                        Dim x7 = Function()
                                   Return =
                                 End Function
                       #End If
                      End Class
            ]]>,
            <errors>
                <error id="30201"/>
                <error id="30201"/>
            </errors>)

    End Sub

    ''' Lambda with a label
    <Fact>
    Public Sub ParseEmptyLambdaWithLabelSyntax()
        ParseAndVerify(<![CDATA[
                      Class Class1
                        Dim x7 = Sub()
                                l1:
                                 End Sub
                      End Class
            ]]>)

    End Sub

    <WorkItem(887861, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseMoreErrorExpectedExpression30241()
        ParseAndVerify(<![CDATA[
                      <myattr2(1, "abc", prop:=42,true)> Class Scen15
                      End Class
            ]]>,
            <errors>
                <error id="30241"/>
            </errors>)
    End Sub

    <WorkItem(887741, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseMoreErrorExpectedExpression()
        ParseAndVerify(<![CDATA[
                     Module Module1
                        Sub Foo()
                         Dim c = <></>
                        End Sub
                     End Module

            ]]>,
            <errors>
                <error id="31146"/>
            </errors>)
    End Sub

    <WorkItem(527019, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527019")>
    <Fact>
    Public Sub ParseErrorMismatchExpectedEOSVSBadCCExpression()
        ParseAndVerify(<![CDATA[
                      Class Class1
                     'COMPILEERROR: BC31427, "global"
                      #if global.ns1 then
                      #End If
                      End Class
            ]]>,
            <errors>
                <error id="31427"/>
            </errors>)
    End Sub

    <WorkItem(897351, "DevDiv/Personal")>
    <Fact>
    Public Sub MissingExpressionShouldNotEatTokenOnNextLine()
        ParseAndVerify(
            "Class C" & vbCrLf &
            "Sub main()" & vbCrLf &
            "dim x =  " & vbCrLf &
            "end sub" & vbCrLf &
            "Sub bar" & vbCrLf &
            "End sub" & vbCrLf &
            "End Class",
        <errors>
            <error id="30201"/>
        </errors>).
        VerifyOccurrenceCount(SyntaxKind.EndSubStatement, 2)
    End Sub

    <WorkItem(929944, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseQueryWithSelectClause()
        ParseAndVerify(<![CDATA[
class C1
    Dim scen2 = From c In "" Select s
End Class
            ]]>).VerifyNoMissingChildren()
    End Sub

    <WorkItem(929944, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseQueryWithSelectAndLetClause()
        ParseAndVerify(<![CDATA[
class C1
    Dim scen2 = From c In "" Let x Select s
    Dim scen3 = From c In "" Let = Select s
 End Class
            ]]>,
            <errors>
                <error id="32020"/>
                <error id="30203"/>
                <error id="30201"/>
            </errors>)
    End Sub

    <WorkItem(929945, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseMethodInvocationWithMissingParens()
        ParseAndVerify(<![CDATA[
class C1
sub foo
foo
end sub
End Class

class c2
sub foo
foo 'comment
end sub
end class

class c3
Dim x2 = Sub()
Dim x21 = Sub(y) y > _
1 End Sub
End Sub
end class
            ]]>, <errors>
                     <error id="30205"/>
                 </errors>).VerifyNoMissingChildren()
    End Sub

    <WorkItem(537225, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537225")>
    <Fact>
    Public Sub TestNewWithAnonymousExpression()
        ParseAndVerify(<![CDATA[
           Friend Module RegressDDB17220
    Sub RegressDDB17220()

        Dim y = New With {Foo()}

    End Sub

    Function foo() As String
        Return Nothing
    End Function
End Module
        ]]>)
    End Sub


    <WorkItem(538492, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538492")>
    <Fact>
    Public Sub TestMultipleConditionValid002()
        ParseAndVerify(<![CDATA[
Friend Module MultipleConditionValid002mod
    Sub MultipleConditionValid002()
        Dim col1l = New List(Of Int16) From {1, 2, 3}
        Dim col1la = New List(Of Int16) From {1, 2, 3}
        Dim col1r = New List(Of Int16) From {1, 2, 3}
        Dim col1ra = New List(Of Int16) From {1, 2, 3}
        Dim q2 = From i In col1l, j In col1la, ii In col1l, jj In col1la Join k In col1r _
        Join l In col1ra On k Equals l Join kk In col1r On kk Equals k Join ll In col1ra On l Equals ll _
        On i * j Equals l * k And ll + kk Equals ii + jj Select i
    End Sub
End Module
        ]]>)
    End Sub

    <Fact>
    Public Sub NullableTypeInQueries()
        Dim code = <![CDATA[
                Namespace X
                    Friend Module Y
                        Sub A()
                                Dim col2() As Integer = New Integer() {1, 2, 3, 4}
                                Dim q = From a? As Integer In col2
                                Dim r = From a In col2 Join b? As Integer In col2 On a Equals b 
                                Dim w = From a In col2 Let b? as Integer = a * a Select b 
                        End Sub
                    End Module
                End Namespace
            ]]>.Value
        ParseAndVerify(code)
    End Sub

    <Fact>
    Public Sub Bug4120()
        Dim code = <![CDATA[
Friend Module RegressDDB78254mod
    Sub RegressDDB78254()
        Dim col As New Collections.ArrayList()
        col.Add(1)
        col.Add(2)
        Dim w3 = From i? As Integer In col
    End Sub
End Module
            ]]>.Value
        ParseAndVerify(code)
    End Sub

    <WorkItem(537003, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537003")>
    <Fact>
    Public Sub ParseFromFollowedById()
        Dim code = <![CDATA[
Friend Module M
    Sub S()
        Dim i = From x
    End Sub
End Module
            ]]>.Value
        ParseAndVerify(code,
                       <errors>
                           <error id="36607"/>
                       </errors>)

        Dim expr = ParseExpression("From x", expectsErrors:=True)
        Assert.Equal(SyntaxKind.QueryExpression, expr.Kind)
        Assert.Equal(SyntaxKind.FromClause, expr.ChildNodesAndTokens()(0).Kind())
    End Sub

    <WorkItem(537003, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537003")>
    <Fact>
    Public Sub ParseFromFollowedByAndAlso()
        Dim code = <![CDATA[
Module Module1
    Sub Main()
        Dim from As Boolean
        Dim x = from AndAlso from
        Dim y = from class in "abc"
    End Sub
End Module
            ]]>.Value
        ParseAndVerify(code,
                      <errors>
                          <error id="30183" message="Keyword is not valid as an identifier." start="114" end="119"/>
                      </errors>)
    End Sub

    <WorkItem(527765, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527765")>
    <Fact>
    Public Sub NumericLiteralLongMinValue()
        Dim code = <![CDATA[
Imports System
Imports Microsoft.VisualBasic

Module Program

    Sub Main()
        ' Long Min
        LongMaxMinMinus1(9223372036854775807, -9223372036854775807L) ' Bug#5033 is by design because VB do NOT parse negative num (only neg expr)

        DoubleMaxMin(1.79769313486231E+308, -1.79769313486231E+308)
    End Sub

    Sub LongMaxMinMinus1(x As Long, byref y as Long)
    End Sub

    Sub DoubleMaxMin(x As Double, y As Double)
    End Sub
End Module
            ]]>.Value
        ParseAndVerify(code)
    End Sub

    <WorkItem(541324, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541324")>
    <Fact>
    Public Sub ParsingIntoClauseShouldNotCreateMissingTokens()
        Dim code = <![CDATA[
Imports System
Module Module1
    Sub Main()
        Dim x(9) As Integer
        x(0) = 1 : x(1) = 2 : x(2) = 3 : x(3) = 4 : x(4) = 5 : x(5) = 6 : x(6) = 7 : x(7) = 8 : x(8) = 9 : x(9) = 10
        Dim q1 = Aggregate i In x Into Sum(i)
    End Sub
End Module
            ]]>.Value
        ParseAndVerify(code)
    End Sub

    <Fact>
    Public Sub ParsingIntoClause()
        Dim code = <![CDATA[
Imports System
Module Module1
    Sub Main()
        Dim x(9) As Integer
        x(0) = 1 : x(1) = 2 : x(2) = 3 : x(3) = 4 : x(4) = 5 : x(5) = 6 : x(6) = 7 : x(7) = 8 : x(8) = 9 : x(9) = 10
        Dim q1 = Aggregate i In x Into foo = Sum(i)
    End Sub
End Module
            ]]>.Value
        ParseAndVerify(code)
    End Sub

    <WorkItem(542057, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542057")>
    <Fact>
    Public Sub ParsingImplicitLineContinuationAfterDistinct()
        Dim code = <![CDATA[
Imports System.Linq
Module M
    Sub Main
        Dim y = From z In "ABC" Distinct
        .ToArray()
    End Sub
End Module
            ]]>.Value
        ParseAndVerify(code)
    End Sub


    <Fact>
    Public Sub InvalidTypeName()
        Dim code = <String>*ERROR*</String>.Value
        Dim name = SyntaxFactory.ParseTypeName(code)

        Assert.True(name.ContainsDiagnostics)
    End Sub

    <WorkItem(542686, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542686")>
    <Fact>
    Public Sub FromAsIdentifierInRangeQuery_1()
        Dim code = <![CDATA[
Module Program
    Sub Main()
        Dim q = From From In "qq" Join y In "www" On From Equals y
        Dim r = From From In "qq" Join y In "www" On 
                                                  From Equals 
                                                  y
    End Sub
End Module
            ]]>.Value
        ParseAndVerify(code)
    End Sub

    <WorkItem(542686, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542686")>
    <Fact>
    Public Sub FromAsIdentifierInRangeQuery_2()
        Dim code = <![CDATA[
Module Program
    Sub Main()
        Dim t = From g
        Dim s = from Equals
        Dim f = from Class
    End Sub
End Module
            ]]>.Value
        ParseAndVerify(code, Diagnostic(ERRID.ERR_ExpectedIn, ""),
                             Diagnostic(ERRID.ERR_ExpectedEOS, "Equals"),
                             Diagnostic(ERRID.ERR_ExpectedEOS, "Class"))
    End Sub

    <WorkItem(542715, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542715")>
    <Fact()>
    Public Sub ParseExpressionRangeVariableDeclarationList_1()

        ParseAndVerify(<![CDATA[
Imports System.Linq
 
Module Program
    Sub Main()
        Dim y = Aggregate x In New Integer() {}
                Into Count, Sum.All
    End Sub
End Module
]]>, Diagnostic(ERRID.ERR_ExpectedEndOfExpression, "Sum"))
    End Sub


    <WorkItem(544274, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544274")>
    <Fact()>
    Public Sub ParseMemberAccessExpressionWithDotDot()

        ParseAndVerify(<![CDATA[
Imports System.Linq

Module Program
    Sub Main()
         Dim i = Integer..MaxValue
    End Sub
End Module
]]>, Diagnostic(ERRID.ERR_ExpectedIdentifier, ""))
    End Sub

    <WorkItem(545515, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545515")>
    <Fact>
    Public Sub FromAsNewInQuery()
        Dim code = <![CDATA[
Module M
    Sub Main()
        Dim q = From x As New Char In ""
    End Sub
End Module
            ]]>.Value
        ParseAndVerify(code, Diagnostic(ERRID.ERR_UnrecognizedTypeKeyword, ""))
    End Sub

    <WorkItem(545584, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545584")>
    <Fact>
    Public Sub ParseNullableAfterOpenGeneric()
        Dim code = <![CDATA[
Class C
   Dim x1 = GetType(List(Of )?)
   Dim x2 = GetType(List(Of ).?)
   Dim x3 = GetType(List(Of ).x.y?)
End Class

            ]]>.Value
        ParseAndVerify(code, Diagnostic(ERRID.ERR_UnrecognizedType, ")"),
    Diagnostic(ERRID.ERR_UnrecognizedType, ")"),
    Diagnostic(ERRID.ERR_ExpectedIdentifier, ""),
    Diagnostic(ERRID.ERR_UnrecognizedType, ")"))
    End Sub

    <WorkItem(545166, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545166")>
    <Fact>
    Public Sub ParseNullableWithQuestionMark()
        Dim code = <![CDATA[
Imports System
 
Module M
    Sub Main()
        Dim x = (Integer?).op_Implicit(1) 

        Dim y = (System.Nullable(Of Integer)).op_Implicit(1) 
    End Sub
End Module
            ]]>.Value
        ParseAndVerify(code)
    End Sub

    <WorkItem(546514, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546514")>
    <Fact>
    Public Sub XmlMemberAccessWithInvalidWhitespace()
        Dim tree = ParseAndVerify(<![CDATA[
Imports <xmlns:db="http://example.org/database">
Module Test
  Sub Main()
    Dim x = <db:customer><db:Name>Bob</db:Name></db:customer>
    Console.WriteLine(x.< db : Name >.Value)
  End Sub
End Module

        ]]>,
    Diagnostic(ERRID.ERR_IllegalXmlWhiteSpace, " "),
    Diagnostic(ERRID.ERR_IllegalXmlWhiteSpace, " "),
    Diagnostic(ERRID.ERR_IllegalXmlWhiteSpace, " "),
    Diagnostic(ERRID.ERR_ExpectedXmlName, "Name"),
    Diagnostic(ERRID.ERR_IllegalXmlWhiteSpace, " "))

        VerifySyntaxKinds(tree.GetRoot().DescendantNodes.OfType(Of XmlBracketedNameSyntax).First,
                            SyntaxKind.XmlBracketedName,
                                SyntaxKind.LessThanToken,
                                SyntaxKind.XmlName,
                                    SyntaxKind.XmlPrefix,
                                        SyntaxKind.XmlNameToken,
                                        SyntaxKind.ColonToken,
                                    SyntaxKind.XmlNameToken,
                                SyntaxKind.GreaterThanToken)

    End Sub

    <WorkItem(546378, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546378")>
    <Fact>
    Public Sub SpuriousLineContinuationAtBeginningOfStatement()
        Dim tree = ParseAndVerify(<![CDATA[
Class C
    _
    Friend Shared Function SendInput() As Integer
        Return 0
    End Function
End Class
Module Module1
    Sub Main
    End Sub
End Module

        ]]>)

    End Sub

    <WorkItem(531567, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531567")>
    <Fact>
    Public Sub ColonAfterQuery()
        Dim tree = ParseAndVerify(<![CDATA[
Friend Module Program
    Sub Main()
        Dim q = From y In {1, 2, 3} Select y
:
    End Sub
End Module

        ]]>)

    End Sub

    <WorkItem(546515, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546515")>
    <Fact>
    Public Sub ParseBadAsClauseInJoinQuery()
        Dim code = <![CDATA[
Module M
    Sub S()
        q = From a In aa Group Join b As $ In bb On a Equals b
    End Sub
End Module
        ]]>.Value

        ParseAndVerify(code,
                       Diagnostic(ERRID.ERR_UnrecognizedType, ""),
                       Diagnostic(ERRID.ERR_IllegalChar, "$"),
                       Diagnostic(ERRID.ERR_ExpectedInto, ""))
    End Sub

    <WorkItem(546515, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546515")>
    <Fact>
    Public Sub ParseBadSourceInJoinQuery()
        Dim code = <![CDATA[
Module M
    Sub S()
        q = From a In aa Group Join b As string In $ On a Equals b
    End Sub
End Module
        ]]>.Value

        ParseAndVerify(code,
                       Diagnostic(ERRID.ERR_ExpectedExpression, ""),
                       Diagnostic(ERRID.ERR_ExpectedOn, ""),
                       Diagnostic(ERRID.ERR_ExpectedInto, ""),
                       Diagnostic(ERRID.ERR_IllegalChar, "$"))
    End Sub

    <WorkItem(546710, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546710")>
    <Fact()>
    Public Sub Bug16626()
        Dim source = <![CDATA[
Module M
    Sub M()
        Dim q = From c In ""
                Let a = Sub() If True The, b = 2 Select a
    End Sub
End Module
]]>.Value
        Dim text = SourceText.From(source)
        Dim tree = VisualBasicSyntaxTree.ParseText(text)
        Dim nodes = tree.GetRoot().DescendantNodes().ToArray()
        Dim varNameEquals = nodes.First(Function(n) n.Kind = SyntaxKind.VariableNameEquals)
        Assert.Equal(varNameEquals.ToFullString(), "a = ")
    End Sub

    <Fact()>
    Public Sub Bug16626_2()
        Dim source = <![CDATA[
Module M
    Sub M()
        Dim q = From c In ""
                Let a As Class C, b = 2 Select a
    End Sub
End Module
]]>.Value
        Dim text = SourceText.From(source)
        Dim tree = VisualBasicSyntaxTree.ParseText(text)
        Dim nodes = tree.GetRoot().DescendantNodes().ToArray()
        Dim varNameEquals = nodes.First(Function(n) n.Kind = SyntaxKind.VariableNameEquals)
        Assert.Equal(varNameEquals.ToFullString(), "a As Class C")
    End Sub

    <Fact()>
    Public Sub Bug16626_3()
        Dim source = <![CDATA[
Module M
    Sub M()
        Dim q = From a In
            Sub() If True The, b In a
    End Sub
End Module
]]>.Value
        Dim text = SourceText.From(source)
        Dim tree = VisualBasicSyntaxTree.ParseText(text)
        Dim nodes = tree.GetRoot().DescendantNodes().ToArray()
        Dim collectionRangeVar = DirectCast(nodes.First(Function(n) n.Kind = SyntaxKind.CollectionRangeVariable), CollectionRangeVariableSyntax)
        Dim varName = collectionRangeVar.Identifier
        Assert.Equal(varName.ToFullString(), "a ")
    End Sub

    <Fact()>
    Public Sub Bug16626_4()
        Dim source = <![CDATA[
Module M
    Sub M()
        Dim q = From a As Object In
            Sub() If True The, b In a
    End Sub
End Module
]]>.Value
        Dim text = SourceText.From(source)
        Dim tree = VisualBasicSyntaxTree.ParseText(text)
        Dim nodes = tree.GetRoot().DescendantNodes().ToArray()
        Dim collectionRangeVar = DirectCast(nodes.First(Function(n) n.Kind = SyntaxKind.CollectionRangeVariable), CollectionRangeVariableSyntax)
        Dim asClause = collectionRangeVar.AsClause
        Assert.Equal(asClause.ToFullString(), "As Object ")
    End Sub

    <WorkItem(601108, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/601108")>
    <Fact>
    Public Sub DistinctAfterOctal()
        Dim tree = ParseAndVerify(<![CDATA[

Module M
    Dim q = From x In "" Select &O7Distinct
End Module

        ]]>)
    End Sub

    <Fact>
    Public Sub MissingQueryKeywords()
        ParseAndVerify(<![CDATA[
Module M
    Dim o = From c in ""
        Order By
        c
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim o = From c in ""
        Order 
        c
End Module
]]>,
            <errors>
                <error id="36605" message="'By' expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim o = From c in ""
        Group By c Into
        F
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim o = From c in ""
        Group c Into
        F
End Module
]]>,
            <errors>
                <error id="36605" message="'By' expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim o = From c in ""
        Group By c
        F
End Module
]]>,
            <errors>
                <error id="36615" message="'Into' expected."/>
                <error id="30188" message="Declaration expected."/>
            </errors>)
    End Sub

    <Fact>
    Public Sub QueryLineBreaks()
        ParseAndVerify(<![CDATA[
Module M
    Dim o = Aggregate c In ""
        Into F
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim o = Aggregate c In ""
        Where F
        Into G
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim o = From x
        In
        ""
        Join
        y
        In
        ""
        On
        x Equals y
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim o = From x
        In
        ""
        Join
        y As Object
        In
        ""
        Join
        z
        In
        ""
        On
        y Equals z
        On
        x Equals y
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim o = From x
        In
        ""
        Group Join
        y
        In
        ""
        On
        x Equals y
        Into
        G
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim o = From x
        In
        ""
        Group Join
        y As Object
        In
        ""
        Group Join
        z
        In
        ""
        On
        y Equals z
        Into
        F
        On
        x Equals y
        Into
        G
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim o = From x
        In
        ""
        Group
        Join
End Module
]]>,
            <errors>
                <error id="36605" message="'By' expected."/>
                <error id="36615" message="'Into' expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim o = From x
        In
        ""
        Group Join
        y
        In
        ""
        Group
        Join
End Module
]]>,
            <errors>
                <error id="36631" message="'Join' expected."/>
                <error id="36607" message="'In' expected."/>
                <error id="36618" message="'On' expected."/>
                <error id="36615" message="'Into' expected."/>
                <error id="36618" message="'On' expected."/>
                <error id="36615" message="'Into' expected."/>
            </errors>)
    End Sub

    <Fact>
    Public Sub LineBreaks()
        ParseAndVerify(<![CDATA[
Module M
    Dim x = CByte(
            1
        )
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = CByte(
        )
End Module
]]>,
            <errors>
                <error id="30201"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = CByte 1
        )
End Module
]]>,
            <errors>
                <error id="30199"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = CType(
            Nothing,
            String
        )
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = CType Nothing,
            String
        )
End Module
]]>,
            <errors>
                <error id="30199"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = CType(
            Nothing,
        )
End Module
]]>,
            <errors>
                <error id="30182"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = New C(
            Of
            Object
        )
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = New C(
            Of
        )
End Module
]]>,
            <errors>
                <error id="30182"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x As Object(
        )
    Dim y As Object(
            ,
        )
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x As Object(
            1
        )
End Module
]]>,
            <errors>
                <error id="30638"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x(
        1
        ) As Object
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M(x As String)
        Mid$(
            x,
            1,
            2
            ) = ""
    End Sub
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Mid$(
            ) = ""
    End Sub
End Module
]]>,
            <errors>
                <error id="30201"/>
                <error id="30196"/>
                <error id="30201"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        For Each c
            In
            ""
        Next
    End Sub
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        For Each
            In
            ""
        Next
    End Sub
End Module
]]>,
            <errors>
                <error id="30201"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        For Each c
            In
        Next
    End Sub
End Module
]]>,
            <errors>
                <error id="30201"/>
            </errors>)
    End Sub

    ''' <summary>
    ''' Allow missing optional expression at end of statement
    ''' within single-line expression/statement.
    ''' </summary>
    <WorkItem(642273, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/642273")>
    <Fact()>
    Public Sub MissingOptionalExpressionEndingSingleLineStatement()
        ParseAndVerify(<![CDATA[
Module M
    Dim x = (Sub() If True Then For i = 1 To 5 : Next)
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If True Then For i = 1 To 5 : Next Else
    End Sub
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If True Then For [Else] = 1 To 5 : Next [Else] Else
    End Sub
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = (Sub() If True Then Else Do : Loop)
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = (Sub() If True Then Else Do : Loop
End Module
]]>,
            <errors>
                <error id="30198" message="')' expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If True Then Do : Loop Else
    End Sub
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M([Else] As Boolean)
        If [Else] Then Do : Loop While [Else] Else
    End Sub
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If True Then Do : Return Else
    End Sub
End Module
]]>,
            <errors>
                <error id="30083"/>
                <error id="30205"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = (Sub(o) Throw)(Nothing)
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Try
        Catch
            Throw) 
        End Try
    End Sub
End Module
]]>,
            <errors>
                <error id="30205" message="End of statement expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = (Sub() Throw
End Module
]]>,
            <errors>
                <error id="30198" message="')' expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If True Then Throw Else
    End Sub
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = (Sub() Throw New)
End Module
]]>,
            <errors>
                <error id="30182"/>
                <error id="30198" message="')' expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim z = (Sub() Throw TypeOf)
End Module
]]>,
            <errors>
                <error id="30201" message="Expression expected."/>
                <error id="30224"/>
                <error id="30182"/>
                <error id="30198" message="')' expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim z = (Sub() Throw From c in "" Select c)
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim z = (Sub() Throw From c in "" Select)
End Module
]]>,
            <errors>
                <error id="30201" message="Expression expected."/>
                <error id="30198" message="')' expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim z = (Sub() Throw From c in "" Order By)
End Module
]]>,
            <errors>
                <error id="30201" message="Expression expected."/>
                <error id="30198" message="')' expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim z = (Sub() If True Then x =)
End Module
]]>,
            <errors>
                <error id="30201" message="Expression expected."/>
                <error id="30198" message="')' expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = (Async Sub() Throw Await)
End Module
]]>,
            <errors>
                <error id="30201" message="Expression expected."/>
                <error id="30198" message="')' expected."/>
            </errors>)
    End Sub

    <ClrOnlyFact>
    <WorkItem(1085618, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1085618")>
    Public Sub TooDeepLambdaDeclarations()
        Dim depth = 5000
        Dim builder As New StringBuilder()
        builder.AppendLine(<![CDATA[

Module Module1

     Sub Test()
         Dim c = New Customer With { _]]>.Value)

        For i = 0 To depth
            Dim line = String.Format("Dim x{0} = Sub()", i)
            builder.AppendLine(line)
        Next

        For i = 0 To depth
            builder.AppendLine("End Sub")
        Next

        builder.AppendLine(<![CDATA[}
    End Sub
End Module]]>.Value)

        Dim tree = Parse(builder.ToString())
        Dim diagnostic = tree.GetDiagnostics().Single()
        Assert.Equal(CInt(ERRID.ERR_TooLongOrComplexExpression), diagnostic.Code)
    End Sub

    <ClrOnlyFact>
    Public Sub TooDeepObjectInitializers()
        Dim depth = 5000
        Dim builder As New StringBuilder()
        builder.AppendLine(<![CDATA[

Module Module1
     Class Customer
         Public C As Customer
         Public U As Integer
     End Class

     Sub Test()
         Dim c = New Customer With { _]]>.Value)

        For i = 0 To depth
            builder.AppendLine(".C = New Customer With { _")
        Next

        builder.AppendLine(".C = Nothing, U = 0 }, _")

        For i = 0 To depth - 1
            builder.AppendLine(".U = 0}, _")
        Next

        builder.AppendLine(<![CDATA[}
    End Sub
End Module]]>.Value)

        Dim tree = Parse(builder.ToString())
        Dim diagnostic = tree.GetDiagnostics().Single()
        Assert.Equal(CInt(ERRID.ERR_TooLongOrComplexExpression), diagnostic.Code)
    End Sub

    <ClrOnlyFact>
    Public Sub TooDeepLambdaDeclarationsAsExpression()
        Dim depth = 5000
        Dim builder As New StringBuilder()
        builder.AppendLine("Sub()")
        For i = 0 To depth
            Dim line = String.Format("Dim x{0} = Sub()", i)
            builder.AppendLine(line)
        Next

        For i = 0 To depth
            builder.AppendLine("End Sub")
        Next

        builder.AppendLine("End Sub")

        Dim expr = SyntaxFactory.ParseExpression(builder.ToString())
        Dim diagnostic = expr.GetDiagnostics().Single()
        Assert.Equal(CInt(ERRID.ERR_TooLongOrComplexExpression), diagnostic.Code)
    End Sub

    <ClrOnlyFact>
    Public Sub TooDeepLambdaDeclarationsAsStatement()
        Dim depth = 5000
        Dim builder As New StringBuilder()
        builder.AppendLine("Dim c = Sub()")
        For i = 0 To depth
            Dim line = String.Format("Dim x{0} = Sub()", i)
            builder.AppendLine(line)
        Next

        For i = 0 To depth
            builder.AppendLine("End Sub")
        Next

        builder.AppendLine("End Sub")

        Dim stmt = SyntaxFactory.ParseExecutableStatement(builder.ToString())
        Dim diagnostic = stmt.GetDiagnostics().Single()
        Assert.Equal(CInt(ERRID.ERR_TooLongOrComplexExpression), diagnostic.Code)
    End Sub

End Class
