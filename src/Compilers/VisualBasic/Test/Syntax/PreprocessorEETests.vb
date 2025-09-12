' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
Imports Roslyn.Test.Utilities

Public Class PreprocessorEETests

    Private Function ParseExpression(text As XElement) As ExpressionSyntax
        Return ParseExpression(text.Value)
    End Function

    Private Function ParseExpressionAsRhs(text As String, Optional expectsErrors As Boolean = False) As ExpressionSyntax
        Dim modText =
<Module>
    Module M1
        Dim x =<%= text %>
    End Module
</Module>.Value

        Dim prog = DirectCast(VisualBasic.SyntaxFactory.ParseCompilationUnit(modText).Green, CompilationUnitSyntax)
        Dim modTree = DirectCast(prog.Members(0), TypeBlockSyntax)
        Dim varDecl = DirectCast(modTree.Members(0), FieldDeclarationSyntax)

        Return DirectCast(varDecl.Declarators(0), VariableDeclaratorSyntax).Initializer.Value
    End Function

    Private Function ParseExpression(text As String, Optional expectsErrors As Boolean = False) As ExpressionSyntax
        Dim expr = VisualBasic.SyntaxFactory.ParseExpression(text)
        Return DirectCast(expr.Green, ExpressionSyntax)
    End Function

    <Fact>
    Public Sub CCExpressionsSimpleBoolLiterals()
#Const x = True Or True
        Dim tree = ParseExpression("True Or True")
        Dim res = ExpressionEvaluator.EvaluateExpression(tree)
        Assert.Equal(True, res.ValueAsObject)

#Const x = Not True
        tree = ParseExpression("Not True")
        res = ExpressionEvaluator.EvaluateExpression(tree)
        Assert.Equal(False, res.ValueAsObject)

#Const x = True Xor False
        tree = ParseExpression("True Xor False")
        res = ExpressionEvaluator.EvaluateExpression(tree)
        Assert.Equal(True, res.ValueAsObject)

#Const x = True + False
        tree = ParseExpression("True + False")
        res = ExpressionEvaluator.EvaluateExpression(tree)
        Assert.Equal(-1S, res.ValueAsObject)

#Const x = True * False
        tree = ParseExpression("True * False")
        res = ExpressionEvaluator.EvaluateExpression(tree)
        Assert.Equal(0S, res.ValueAsObject)

#Const x = True ^ False
        tree = ParseExpression("True ^ False")
        res = ExpressionEvaluator.EvaluateExpression(tree)
        Assert.Equal(1.0, res.ValueAsObject)

#Const x = True >> False
        tree = ParseExpression("True >> False")
        res = ExpressionEvaluator.EvaluateExpression(tree)
        Assert.Equal(-1S, res.ValueAsObject)

    End Sub

    <Fact>
    Public Sub CCExpressionsSimpleDateLiterals()
#Const x = #4/10/2012#
        Dim tree = ParseExpressionAsRhs("#4/10/2012#")
        Dim res = ExpressionEvaluator.EvaluateExpression(tree)
        Assert.Equal(#4/10/2012#, res.ValueAsObject)

    End Sub

    <Fact>
    Public Sub CCExpressionsSimpleTernaryExp()
#Const x = If(True, 42, 43)
        Dim tree = ParseExpression(" If(True, 42, 43)")
        Dim res = ExpressionEvaluator.EvaluateExpression(tree)
        Assert.Equal(42, res.ValueAsObject)

    End Sub

    <Fact>
    Public Sub CCExpressionsSimpleIntegralLiterals()

#Const x = 1
        Dim tree = ParseExpression("1 + 1")
        Dim res = ExpressionEvaluator.EvaluateExpression(tree)
        Assert.Equal(2, res.ValueAsObject)

#Const x = 1US + 1S
        tree = ParseExpression("1US - 1S")
        res = ExpressionEvaluator.EvaluateExpression(tree)
        Assert.Equal(0, res.ValueAsObject)

#Const x = 1UL + 2UL
        tree = ParseExpression("1UL + 2UL")
        res = ExpressionEvaluator.EvaluateExpression(tree)
        Assert.Equal(3UL, res.ValueAsObject)

#Const x = 5US + 5S
        tree = ParseExpression("5US + 5S")
        res = ExpressionEvaluator.EvaluateExpression(tree)
        Assert.Equal(10, res.ValueAsObject)

#Const x = -5US + 5S
        tree = ParseExpression("-5US + 5S")
        res = ExpressionEvaluator.EvaluateExpression(tree)
        Assert.Equal(0, res.ValueAsObject)

#Const x = -5US / 5S
        tree = ParseExpression("-5US / 5S")
        res = ExpressionEvaluator.EvaluateExpression(tree)
        Assert.Equal(-1.0, res.ValueAsObject)

#Const x = -5US \ 5S
        tree = ParseExpression("-5US \ 5S")
        res = ExpressionEvaluator.EvaluateExpression(tree)
        Assert.Equal(-1, res.ValueAsObject)

#Const x = 1L + 1UL
        tree = ParseExpression("1L + 1UL")
        res = ExpressionEvaluator.EvaluateExpression(tree)
        Assert.Equal(2D, res.ValueAsObject)

#Const x = 1 ^ 2
        tree = ParseExpression("1 ^ 2")
        res = ExpressionEvaluator.EvaluateExpression(tree)
        Assert.Equal(1.0, res.ValueAsObject)

#Const x = 16 >> 2US
        tree = ParseExpression("16 >> 2US")
        res = ExpressionEvaluator.EvaluateExpression(tree)
        Assert.Equal(4, res.ValueAsObject)

    End Sub

    <Fact>
    Public Sub CCExpressionsSimpleRelational()

#Const x = 2 > 1
        Dim tree = ParseExpression(" 2 > 1")
        Dim res = ExpressionEvaluator.EvaluateExpression(tree)
        Assert.Equal(True, res.ValueAsObject)

#Const x = (-1 = 3 > 1)
        tree = ParseExpression("(-1 = 3 > 1)")
        res = ExpressionEvaluator.EvaluateExpression(tree)
        Assert.Equal(False, res.ValueAsObject)
    End Sub

    <Fact>
    Public Sub CCExpressionsSimpleIsTrue()

        Dim tree = ParseExpression("""qqqq""")
        Dim res = ExpressionEvaluator.EvaluateExpression(tree)
        Assert.Equal("qqqq", res.ValueAsObject)

        Dim cond = ExpressionEvaluator.EvaluateCondition(tree)
        Assert.Equal(True, cond.IsBad)
        Assert.Equal(CInt(ERRID.ERR_RequiredConstConversion2), DirectCast(cond, BadCConst).ErrorId)
        Assert.Equal(False, cond.IsBooleanTrue)

        tree = ParseExpression("True >> False = -1")
        cond = ExpressionEvaluator.EvaluateCondition(tree)
        Assert.Equal(True, cond.IsBooleanTrue)

        tree = ParseExpression("If(True > False, 42, True >> False) < 0")
        cond = ExpressionEvaluator.EvaluateCondition(tree)
        Assert.Equal(True, cond.IsBooleanTrue)

    End Sub

    <Fact>
    Public Sub CCExpressionsSimpleNames()

        Dim tree = ParseExpression("""qqqq""")
        Dim res = ExpressionEvaluator.EvaluateExpression(tree)
        Assert.Equal("qqqq", res.ValueAsObject)

        Dim table = ImmutableDictionary.Create(Of String, CConst)(StringComparer.InvariantCultureIgnoreCase).Add("X", res)

        tree = ParseExpression("X")
        res = ExpressionEvaluator.EvaluateExpression(tree, table)
        Assert.Equal("qqqq", res.ValueAsObject)

        table = table.Add("Y", CConst.Create("W"c))

        tree = ParseExpression("X + Y")
        res = ExpressionEvaluator.EvaluateExpression(tree, table)
        Assert.Equal("qqqqW", res.ValueAsObject)

        ' CC allows type chars as long as they match constant type.
        tree = ParseExpression("X$ + Y")
        res = ExpressionEvaluator.EvaluateExpression(tree, table)
        Assert.Equal("qqqqW", res.ValueAsObject)

        ' y should hide Y
        table = table.Remove("y")
        table = table.Add("y", CConst.Create("QWE"))

        tree = ParseExpression("X & Y")
        res = ExpressionEvaluator.EvaluateExpression(tree, table)
        Assert.Equal("qqqqQWE", res.ValueAsObject)

        ' bracketed
        tree = ParseExpression("X & [Y]")
        res = ExpressionEvaluator.EvaluateExpression(tree, table)
        Assert.Equal("qqqqQWE", res.ValueAsObject)

        ' error
        tree = ParseExpression("X + 1")
        res = ExpressionEvaluator.EvaluateExpression(tree, table)
        Assert.Equal(True, res.IsBad)
        Assert.Equal(CInt(ERRID.ERR_RequiredConstConversion2), DirectCast(res, BadCConst).ErrorId)

        table = table.SetItem("X", res)

        tree = ParseExpression("X")
        res = ExpressionEvaluator.EvaluateExpression(tree, table)
        Assert.Equal(True, res.IsBad)
        Assert.Equal(0, DirectCast(res, BadCConst).ErrorId)

    End Sub

    <WorkItem(882921, "DevDiv/Personal")>
    <Fact>
    Public Sub CCCastExpression()
        Dim tree = ParseExpression("CBool(WIN32)")
        Dim res = ExpressionEvaluator.EvaluateExpression(tree)
        Assert.Equal(False, res.ValueAsObject)
    End Sub

    <WorkItem(888301, "DevDiv/Personal")>
    <Fact>
    Public Sub CCNotUndefinedConst()
        Dim tree = ParseExpression("Not OE_WIN9X")
        Dim res = ExpressionEvaluator.EvaluateExpression(tree)

#If Not OE_WIN9X = -1 Then
        Assert.Equal(-1, res.ValueAsObject)
#Else
        Assert.Equal(False, res.ValueAsObject)
#End If

    End Sub

    <WorkItem(888303, "DevDiv/Personal")>
    <Fact>
    Public Sub CCDateGreaterThanNow()
        Dim tree = ParseExpressionAsRhs("#7/1/2003# > Now") ' Note, "Now" is undefined, thus has value Nothing.
        Dim res = ExpressionEvaluator.EvaluateExpression(tree)

        Assert.Equal(True, res.ValueAsObject)
    End Sub

    <WorkItem(888305, "DevDiv/Personal")>
    <Fact>
    Public Sub CCCharEqualsChar()
        Dim tree = ParseExpression("""A""c = ""a""c")
        Dim res = ExpressionEvaluator.EvaluateExpression(tree)

#If "A"c = "a"c Then
        Assert.Equal(True, res.ValueAsObject)
#Else
        Assert.Equal(False, res.ValueAsObject)
#End If

    End Sub

    <WorkItem(888316, "DevDiv/Personal")>
    <Fact>
    Public Sub CCCast()
        Dim tree = ParseExpression("DirectCast(42, Integer)")
        Dim res = ExpressionEvaluator.EvaluateExpression(tree)
        Assert.Equal(42, res.ValueAsObject)

        tree = ParseExpression("TryCast(42, Short)")
        res = ExpressionEvaluator.EvaluateExpression(tree)
        Assert.Equal(Nothing, res.ValueAsObject)
        Assert.Equal(ERRID.ERR_TryCastOfValueType1, res.ErrorId)

        tree = ParseExpression("CType(42, UShort)")
        res = ExpressionEvaluator.EvaluateExpression(tree)
        Assert.Equal(42US, res.ValueAsObject)

        tree = ParseExpression("DirectCast(42UI, UInteger)")
        res = ExpressionEvaluator.EvaluateExpression(tree)
        Assert.Equal(42UI, res.ValueAsObject)

        tree = ParseExpression("TryCast(-420000, Long)")
        res = ExpressionEvaluator.EvaluateExpression(tree)
        Assert.Equal(Nothing, res.ValueAsObject)
        Assert.Equal(ERRID.ERR_TryCastOfValueType1, res.ErrorId)

        tree = ParseExpression("CType(420000, ULong)")
        res = ExpressionEvaluator.EvaluateExpression(tree)
        Assert.Equal(420000UL, res.ValueAsObject)

        tree = ParseExpression("DirectCast(42.2D, Decimal)")
        res = ExpressionEvaluator.EvaluateExpression(tree)
        Assert.Equal(42.2D, res.ValueAsObject)

        tree = ParseExpression("TryCast(-42.222, Single)")
        res = ExpressionEvaluator.EvaluateExpression(tree)
        Assert.Equal(Nothing, res.ValueAsObject)
        Assert.Equal(ERRID.ERR_TryCastOfValueType1, res.ErrorId)

        tree = ParseExpression("CType(-42, SByte)")
        res = ExpressionEvaluator.EvaluateExpression(tree)
        Assert.Equal(CSByte(-42), res.ValueAsObject)

        tree = ParseExpression("DirectCast(CByte(42), Byte)")
        res = ExpressionEvaluator.EvaluateExpression(tree)
        Assert.Equal(CByte(42), res.ValueAsObject)

        tree = ParseExpression("TryCast(""4"", Char)")
        res = ExpressionEvaluator.EvaluateExpression(tree)
        Assert.Equal(Nothing, res.ValueAsObject)
        Assert.Equal(ERRID.ERR_TryCastOfValueType1, res.ErrorId)
    End Sub

    <WorkItem(906041, "DevDiv/Personal")>
    <Fact>
    Public Sub CCCastObject()
        Dim tree = ParseExpression("DirectCast(Nothing, Object)")
        Dim res = ExpressionEvaluator.EvaluateExpression(tree)
        Assert.Equal(Nothing, res.ValueAsObject)
        Assert.False(res.IsBad)
    End Sub

End Class
