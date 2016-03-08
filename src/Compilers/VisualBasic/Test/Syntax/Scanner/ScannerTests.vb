' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Thread
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

<CLSCompliant(False)>
Public Class ScannerTests
    Inherits BasicTestBase

    Private Function ScanOnce(str As String, Optional startStatement As Boolean = False) As SyntaxToken
        Return SyntaxFactory.ParseToken(str, startStatement:=startStatement)
    End Function

    Private Function AsString(tokens As IEnumerable(Of SyntaxToken)) As String
        Dim str = String.Concat(From t In tokens Select t.ToFullString())
        Return str
    End Function

    Private Function MakeDwString(str As String) As String
        Return (From c In str Select If(c < ChrW(&H21S) OrElse c > ChrW(&H7ES), c, ChrW(AscW(c) + &HFF00US - &H20US))).ToArray
    End Function

    Private Function ScanAllCheckDw(str As String) As IEnumerable(Of SyntaxToken)
        Dim tokens = SyntaxFactory.ParseTokens(str)

        ' test that token have the same text as it was.
        Assert.Equal(str, AsString(tokens))

        ' test that we get same with doublewidth string
        Dim doubleWidthStr = MakeDwString(str)
        Dim doubleWidthTokens = ScanAllNoDwCheck(doubleWidthStr)

        Assert.Equal(tokens.Count, doubleWidthTokens.Count)

        For Each t In tokens.Zip(doubleWidthTokens, Function(t1, t2) Tuple.Create(t1, t2))
            Assert.Equal(t.Item1.Kind, t.Item2.Kind)
            Assert.Equal(t.Item1.Span, t.Item2.Span)
            Assert.Equal(t.Item1.FullSpan, t.Item2.FullSpan)
            Assert.Equal(MakeDwString(t.Item1.ToFullString()), t.Item2.ToFullString())
        Next

        Return tokens
    End Function

    Private Function ScanAllNoDwCheck(str As String) As IEnumerable(Of SyntaxToken)
        Dim tokens = SyntaxFactory.ParseTokens(str)

        ' test that token have the same text as it was.
        Assert.Equal(str, AsString(tokens))

        Return tokens
    End Function

    <Fact>
    Public Sub Scanner_EndOfText()
        Dim tk = ScanOnce("")
        Assert.Equal(SyntaxKind.EndOfFileToken, tk.Kind)
        Assert.Equal("", tk.ToFullString())

        tk = ScanOnce(" ")
        Assert.Equal(SyntaxKind.EndOfFileToken, tk.Kind)
        Assert.Equal(" ", tk.ToFullString())

        tk = ScanOnce("  ")
        Assert.Equal(SyntaxKind.EndOfFileToken, tk.Kind)
        Assert.Equal("  ", tk.ToFullString())

        tk = ScanOnce("'")
        Assert.Equal(SyntaxKind.EmptyToken, tk.Kind)
        Assert.Equal(SyntaxKind.CommentTrivia, tk.TrailingTrivia(0).Kind)
        Assert.Equal("'", tk.ToFullString())

        tk = ScanOnce("'", startStatement:=True)
        Assert.Equal(SyntaxKind.EndOfFileToken, tk.Kind)
        Assert.Equal(SyntaxKind.CommentTrivia, tk.LeadingTrivia(0).Kind)
        Assert.Equal("'", tk.ToFullString())

        tk = ScanOnce(" ' ")
        Assert.Equal(SyntaxKind.EmptyToken, tk.Kind)
        Assert.Equal(SyntaxKind.WhitespaceTrivia, tk.LeadingTrivia(0).Kind)
        Assert.Equal(SyntaxKind.CommentTrivia, tk.TrailingTrivia(0).Kind)
        Assert.Equal(" ", tk.LeadingTrivia(0).ToString())
        Assert.Equal("' ", tk.TrailingTrivia(0).ToString())
        Assert.Equal(" ' ", tk.ToFullString())

        tk = ScanOnce(" ' ", startStatement:=True)
        Assert.Equal(SyntaxKind.EndOfFileToken, tk.Kind)
        Assert.Equal(SyntaxKind.WhitespaceTrivia, tk.LeadingTrivia(0).Kind)
        Assert.Equal(SyntaxKind.CommentTrivia, tk.LeadingTrivia(1).Kind)
        Assert.Equal(" ", tk.LeadingTrivia(0).ToString())
        Assert.Equal("' ", tk.LeadingTrivia(1).ToString())
        Assert.Equal(" ' ", tk.ToFullString())
    End Sub

    <Fact>
    Public Sub Scanner_StatementTerminator()
        Dim tk = ScanOnce(vbCr)
        Assert.Equal(SyntaxKind.EmptyToken, tk.Kind)
        Assert.Equal(vbCr, tk.ToFullString())

        tk = ScanOnce(vbCr, startStatement:=True)
        Assert.Equal(SyntaxKind.EndOfFileToken, tk.Kind)
        Assert.Equal(vbCr, tk.ToFullString())

        Dim tks = ScanAllCheckDw(vbCr)
        Assert.Equal(1, tks.Count)
        Assert.Equal(SyntaxKind.EndOfFileToken, tks(0).Kind)
        Assert.Equal(vbCr, tks(0).ToFullString())

        tks = ScanAllCheckDw(" " & vbLf)
        Assert.Equal(1, tks.Count)
        Assert.Equal(SyntaxKind.EndOfFileToken, tks(0).Kind)
        Assert.Equal(" " & vbLf, tks(0).ToFullString())

        tks = ScanAllCheckDw(" A" & vbCrLf & " ")
        Assert.Equal(3, tks.Count)
        Assert.Equal(SyntaxKind.IdentifierToken, tks(0).Kind)
        Assert.Equal(" A" & vbCrLf, tks(0).ToFullString())
        Assert.Equal(SyntaxKind.StatementTerminatorToken, tks(1).Kind)
        Assert.Equal("", tks(1).ToFullString())
        Assert.Equal(SyntaxKind.EndOfFileToken, tks(2).Kind)
        Assert.Equal(" ", tks(2).ToFullString())
    End Sub

    <Fact>
    Public Sub Scanner_StartStatement()
        Dim tk = ScanOnce(vbCr, startStatement:=True)
        Assert.Equal(SyntaxKind.EndOfFileToken, tk.Kind)
        Assert.Equal(vbCr, tk.ToFullString())

        tk = ScanOnce(" " & vbLf, startStatement:=True)
        Assert.Equal(SyntaxKind.EndOfFileToken, tk.Kind)
        Assert.Equal(" " & vbLf, tk.ToFullString())

        Dim str = " " & vbCrLf & " " & vbCr & "'2  " & vbLf & " ("
        tk = ScanOnce(str, startStatement:=True)
        Assert.Equal(SyntaxKind.OpenParenToken, tk.Kind)
        Assert.Equal(str, tk.ToFullString())

        str = "'("
        tk = ScanOnce(str, startStatement:=True)
        Assert.Equal(SyntaxKind.EndOfFileToken, tk.Kind)
        Assert.Equal(str, tk.ToFullString())

        str = "'" & vbCrLf & "("
        tk = ScanOnce(str, startStatement:=False)
        Assert.Equal(SyntaxKind.EmptyToken, tk.Kind)
        Assert.Equal("'" & vbCrLf, tk.ToFullString())

        str = "'" & vbCrLf & "("
        tk = ScanOnce(str, startStatement:=True)
        Assert.Equal(SyntaxKind.OpenParenToken, tk.Kind)
        Assert.Equal(str, tk.ToFullString())

        str = "' " & vbCrLf & "  '(" & vbCrLf & "("
        tk = ScanOnce(str, startStatement:=True)
        Assert.Equal(SyntaxKind.OpenParenToken, tk.Kind)
        Assert.Equal(str, tk.ToFullString())
    End Sub

    <Fact>
    Public Sub Scanner_LineContWhenExpectingNewStatement()
        Dim tk = ScanOnce("_", startStatement:=True)
        Assert.Equal(SyntaxKind.EndOfFileToken, tk.Kind)
        Assert.Equal("_", tk.ToFullString())

        tk = ScanOnce(" _", startStatement:=True)
        Assert.Equal(SyntaxKind.EndOfFileToken, tk.Kind)
        Assert.Equal(" _", tk.ToFullString())

        tk = ScanOnce(" _ ", startStatement:=True)
        Assert.Equal(SyntaxKind.EndOfFileToken, tk.Kind)
        Assert.Equal(" _ ", tk.ToFullString())

        tk = ScanOnce(" _'", startStatement:=True)
        Assert.Equal(SyntaxKind.BadToken, tk.Kind)
        Assert.Equal(" _'", tk.ToFullString())
        Assert.Equal(30999, tk.Errors(0).Code)

        tk = ScanOnce(" _ rem", startStatement:=True)
        Assert.Equal(SyntaxKind.BadToken, tk.Kind)
        Assert.Equal(" _ rem", tk.ToFullString())
        Assert.Equal(30999, tk.Errors(0).Code)

        tk = ScanOnce(" _ abc", startStatement:=True)
        Assert.Equal(SyntaxKind.BadToken, tk.Kind)
        Assert.Equal(" _ ", tk.ToFullString())
        Assert.Equal(30203, tk.Errors(0).Code)

        Dim tks = ScanAllCheckDw(" _ rem")
        Assert.Equal(SyntaxKind.BadToken, tks(0).Kind)
        Assert.Equal(" _ rem", tks(0).ToFullString())
        Assert.Equal(30999, tks(0).Errors(0).Code)

        tk = ScanOnce("_" & vbLf, startStatement:=True)
        Assert.Equal(SyntaxKind.EndOfFileToken, tk.Kind)
        Assert.Equal("_" & vbLf, tk.ToFullString())

        tk = ScanOnce(" _" & vbLf, startStatement:=True)
        Assert.Equal(SyntaxKind.EndOfFileToken, tk.Kind)
        Assert.Equal(" _" & vbLf, tk.ToFullString())

        Dim str = " _" & vbCrLf & " _" & vbCr & "'2  " & vbLf & " ("
        tk = ScanOnce(str, startStatement:=True)
        Assert.Equal(SyntaxKind.OpenParenToken, tk.Kind)
        Assert.Equal(str, tk.ToFullString())

        str = " _" & vbCrLf & " _" & vbCrLf & "("
        tk = ScanOnce(str, startStatement:=True)
        Assert.Equal(SyntaxKind.OpenParenToken, tk.Kind)
        Assert.Equal(str, tk.ToFullString())
    End Sub

    <Fact>
    Public Sub Scanner_LineContInsideStatement()

        ' this would be a case of      )_
        ' valid _ would have been consumed by   ) 
        Dim tk = ScanOnce("_" & vbLf, False)
        Assert.Equal(SyntaxKind.BadToken, tk.Kind)
        Assert.Equal("_" + vbLf, tk.ToFullString)

        Dim Str = "'_" & vbCrLf & "("
        tk = ScanOnce(Str, False)
        Assert.Equal(SyntaxKind.EmptyToken, tk.Kind)
        Assert.Equal("'_" & vbCrLf, tk.ToFullString())

        Str = " _" & vbCrLf & "("
        tk = ScanOnce(Str, False)
        Assert.Equal(SyntaxKind.OpenParenToken, tk.Kind)
        Assert.Equal(Str, tk.ToFullString())

        ' _ is invalid here, should not be consumed by (
        Str = " _" & vbCrLf & "(" & "_" & vbCrLf & "'qq"
        tk = ScanOnce(Str, False)
        Assert.Equal(SyntaxKind.OpenParenToken, tk.Kind)
        Assert.Equal(" _" & vbCrLf & "(", tk.ToFullString())

        ' _ is valid here, but we should not go past the Eol
        Str = " _" & vbCrLf & "(" & " _" & vbCrLf & "'qq"
        tk = ScanOnce(Str, False)
        Assert.Equal(SyntaxKind.OpenParenToken, tk.Kind)
        Assert.Equal(" _" & vbCrLf & "(" & " _" & vbCrLf, tk.ToFullString())

    End Sub

    <Fact>
    Public Sub Scanner_RemComment()
        Dim str = " " & vbCrLf & " " & vbCr & "REM  " & vbLf & " ("
        Dim tk = ScanOnce(str, True)
        Assert.Equal(SyntaxKind.OpenParenToken, tk.Kind)
        Assert.Equal(str, tk.ToFullString())

        str = "A REM Hello "
        tk = ScanOnce(str, True)
        Assert.Equal(SyntaxKind.IdentifierToken, tk.Kind)
        Assert.Equal(str, tk.ToFullString())

        str = "A A REM Hello " & vbCrLf & "A Rem Hello                                                             "
        Dim tks = ScanAllCheckDw(str)
        Assert.Equal(SyntaxKind.IdentifierToken, tks(0).Kind)
        Assert.Equal("A ", tks(0).ToFullString)
        Assert.Equal(SyntaxKind.IdentifierToken, tks(1).Kind)
        Assert.NotEqual("A ", tks(1).ToFullString)
        Assert.Equal(SyntaxKind.StatementTerminatorToken, tks(2).Kind)
        Assert.Equal(SyntaxKind.IdentifierToken, tks(3).Kind)
        Assert.NotEqual("A ", tks(1).ToFullString)
        Assert.Equal(SyntaxKind.EndOfFileToken, tks(4).Kind)
        Assert.Equal(5, tks.Count)

        REM(
        str = "REM("
        tk = ScanOnce(str, True)
        Assert.Equal(SyntaxKind.EndOfFileToken, tk.Kind)
        Assert.Equal(str, tk.ToFullString())

        str = "ReM" & vbCrLf & "("
        tk = ScanOnce(str, False)
        Assert.Equal(SyntaxKind.EmptyToken, tk.Kind)
        Assert.Equal("ReM" & vbCrLf, tk.ToFullString())

        str = "rEM" & vbCrLf & "("
        tk = ScanOnce(str, True)
        Assert.Equal(SyntaxKind.OpenParenToken, tk.Kind)
        Assert.Equal(str, tk.ToFullString())

        str = "rem " & vbCrLf & "  REM(" & vbCrLf & "("
        tk = ScanOnce(str, True)
        Assert.Equal(SyntaxKind.OpenParenToken, tk.Kind)
        Assert.Equal(str, tk.ToFullString())
    End Sub

    ''' <summary>
    ''' EmptyToken is generated by the Scanner in a single-line
    ''' If or lambda with an empty statement to avoid generating
    ''' a statement terminator with leading or trailing trivia.
    ''' </summary>
    <Fact>
    Public Sub Scanner_EmptyToken()
        ' No EmptyToken required because no trivia before EOF.
        ParseTokensAndVerify("If True Then Else Return :",
                             SyntaxKind.IfKeyword,
                             SyntaxKind.TrueKeyword,
                             SyntaxKind.ThenKeyword,
                             SyntaxKind.ElseKeyword,
                             SyntaxKind.ReturnKeyword,
                             SyntaxKind.ColonToken,
                             SyntaxKind.EndOfFileToken)
        ' No EmptyToken required because no trailing trivia before EOF.
        ' (The space after the colon is leading trivia on EOF.)
        ParseTokensAndVerify("If True Then Else : ",
                             SyntaxKind.IfKeyword,
                             SyntaxKind.TrueKeyword,
                             SyntaxKind.ThenKeyword,
                             SyntaxKind.ElseKeyword,
                             SyntaxKind.ColonToken,
                             SyntaxKind.EndOfFileToken)
        ' EmptyToken required because comment is trailing
        ' trivia between the colon and EOL.
        ParseTokensAndVerify(<![CDATA[If True Then Else :'Comment
Return]]>.Value,
                             SyntaxKind.IfKeyword,
                             SyntaxKind.TrueKeyword,
                             SyntaxKind.ThenKeyword,
                             SyntaxKind.ElseKeyword,
                             SyntaxKind.ColonToken,
                             SyntaxKind.EmptyToken,
                             SyntaxKind.StatementTerminatorToken,
                             SyntaxKind.ReturnKeyword,
                             SyntaxKind.EndOfFileToken)
        ' EmptyToken required because comment is trailing
        ' trivia between the colon and EOF.
        ParseTokensAndVerify("Sub() If True Then Return : REM",
                             SyntaxKind.SubKeyword,
                             SyntaxKind.OpenParenToken,
                             SyntaxKind.CloseParenToken,
                             SyntaxKind.IfKeyword,
                             SyntaxKind.TrueKeyword,
                             SyntaxKind.ThenKeyword,
                             SyntaxKind.ReturnKeyword,
                             SyntaxKind.ColonToken,
                             SyntaxKind.EmptyToken,
                             SyntaxKind.EndOfFileToken)
        ' No EmptyToken required because colon, space, comment
        ' and EOL are all treated as multi-line leading trivia on EndKeyword.
        ParseTokensAndVerify(<![CDATA[If True Then
: 'Comment
End If]]>.Value,
                             SyntaxKind.IfKeyword,
                             SyntaxKind.TrueKeyword,
                             SyntaxKind.ThenKeyword,
                             SyntaxKind.StatementTerminatorToken,
                             SyntaxKind.EndKeyword,
                             SyntaxKind.IfKeyword,
                             SyntaxKind.EndOfFileToken)
    End Sub

    Private Sub ParseTokensAndVerify(str As String, ParamArray kinds As SyntaxKind())
        Dim tokens = SyntaxFactory.ParseTokens(str).ToArray()
        Dim result = String.Join("", tokens.Select(Function(t) t.ToFullString()))
        Assert.Equal(str, result)
        Assert.Equal(tokens.Length, kinds.Length)
        For i = 0 To tokens.Length - 1
            Assert.Equal(tokens(i).Kind, kinds(i))
        Next
    End Sub

    <Fact>
    Public Sub Scanner_DimKeyword()
        Dim Str = " " & vbCrLf & " " & vbCr & "DIM  " & vbLf & " ("
        Dim tk = ScanOnce(Str, True)
        Assert.Equal(SyntaxKind.DimKeyword, tk.Kind)
        Assert.Equal(" " & vbCrLf & " " & vbCr & "DIM  " + vbLf, tk.ToFullString)

        Str = "Dim("
        tk = ScanOnce(Str, True)
        Assert.Equal(SyntaxKind.DimKeyword, tk.Kind)
        Assert.Equal("Dim", tk.ToFullString())

        Str = "DiM" & vbCrLf & "("
        tk = ScanOnce(Str, False)
        Assert.Equal(SyntaxKind.DimKeyword, tk.Kind)
        Assert.Equal("DiM" + vbCrLf, tk.ToFullString)

        Str = "dIM" & " _" & vbCrLf & "("
        tk = ScanOnce(Str, True)
        Assert.Equal(SyntaxKind.DimKeyword, tk.Kind)
        Assert.Equal("dIM" & " _" & vbCrLf, tk.ToFullString())

        Str = "dim " & vbCrLf & "  DIMM" & vbCrLf & "("
        Dim tks = ScanAllNoDwCheck(Str)

        Assert.Equal(SyntaxKind.DimKeyword, tks(0).Kind)
        Assert.Equal(SyntaxKind.StatementTerminatorToken, tks(1).Kind)
        Assert.Equal(SyntaxKind.IdentifierToken, tks(2).Kind)
        Assert.Equal(SyntaxKind.StatementTerminatorToken, tks(3).Kind)
        Assert.Equal(SyntaxKind.OpenParenToken, tks(4).Kind)
    End Sub

    <WorkItem(15925, "DevDiv_Projects/Roslyn")>
    <Fact>
    Public Sub StaticKeyword()
        Dim Str = " " & vbCrLf & " " & vbCr & "STATIC  " & vbLf & " ("
        Dim tk = ScanOnce(Str, True)
        Assert.Equal(SyntaxKind.StaticKeyword, tk.Kind)
        Assert.Equal(" " & vbCrLf & " " & vbCr & "STATIC  " & vbLf, tk.ToFullString())

        Str = "Static("
        tk = ScanOnce(Str, True)
        Assert.Equal(SyntaxKind.StaticKeyword, tk.Kind)
        Assert.Equal("Static", tk.ToFullString())

        Str = "StatiC" & vbCrLf & "("
        tk = ScanOnce(Str, False)
        Assert.Equal(SyntaxKind.StaticKeyword, tk.Kind)
        Assert.Equal("StatiC" & vbCrLf, tk.ToFullString())

        Str = "sTATIC" & " _" & vbCrLf & "("
        tk = ScanOnce(Str, True)
        Assert.Equal(SyntaxKind.StaticKeyword, tk.Kind)
        Assert.Equal("sTATIC" & " _" & vbCrLf, tk.ToFullString())

        Str = "static " & vbCrLf & "  STATICC" & vbCrLf & "("
        Dim tks = ScanAllNoDwCheck(Str)

        Assert.Equal(SyntaxKind.StaticKeyword, tks(0).Kind)
        Assert.Equal(SyntaxKind.StatementTerminatorToken, tks(1).Kind)
        Assert.Equal(SyntaxKind.IdentifierToken, tks(2).Kind)
        Assert.Equal(SyntaxKind.StatementTerminatorToken, tks(3).Kind)
        Assert.Equal(SyntaxKind.OpenParenToken, tks(4).Kind)
    End Sub
    <Fact>
    Public Sub Scanner_FrequentKeywords()
        Dim Str = "End "
        Dim tk = ScanOnce(Str, True)
        Assert.Equal(SyntaxKind.EndKeyword, tk.Kind)
        Assert.Equal(3, tk.Span.Length)
        Assert.Equal(4, tk.FullSpan.Length)
        Assert.Equal("End ", tk.ToFullString())

        Str = "As "
        tk = ScanOnce(Str, False)
        Assert.Equal(SyntaxKind.AsKeyword, tk.Kind)
        Assert.Equal(2, tk.Span.Length)
        Assert.Equal(3, tk.FullSpan.Length)
        Assert.Equal("As ", tk.ToFullString())

        Str = "If "
        tk = ScanOnce(Str, False)
        Assert.Equal(SyntaxKind.IfKeyword, tk.Kind)
        Assert.Equal(2, tk.Span.Length)
        Assert.Equal(3, tk.FullSpan.Length)
        Assert.Equal("If ", tk.ToFullString())
    End Sub

    <Fact>
    Public Sub Scanner_PlusToken()
        Dim Str = "+"
        Dim tk = ScanOnce(Str, True)
        Assert.Equal(SyntaxKind.PlusToken, tk.Kind)
        Assert.Equal(Str, tk.ToFullString())

        Str = "+    ="
        tk = ScanOnce(Str, True)
        Assert.Equal(SyntaxKind.PlusEqualsToken, tk.Kind)
        Assert.Equal(Str, tk.ToFullString())
    End Sub

    <Fact>
    Public Sub Scanner_PowerToken()
        Dim Str = "^"
        Dim tk = ScanOnce(Str, False)
        Assert.Equal(SyntaxKind.CaretToken, tk.Kind)
        Assert.Equal(Str, tk.ToFullString())

        Str = "^    =^"
        Dim tks = ScanAllCheckDw(Str)
        Assert.Equal(SyntaxKind.CaretEqualsToken, tks(0).Kind)
        Assert.Equal(SyntaxKind.CaretToken, tks(1).Kind)
    End Sub

    <Fact>
    Public Sub Scanner_GreaterThanToken()
        Dim Str = ">  "
        Dim tk = ScanOnce(Str, False)
        Assert.Equal(SyntaxKind.GreaterThanToken, tk.Kind)
        Assert.Equal(Str, tk.ToFullString())

        Str = "  >= 'qqqq"
        tk = ScanOnce(Str, True)
        Assert.Equal(SyntaxKind.GreaterThanEqualsToken, tk.Kind)
        Assert.Equal(Str, tk.ToFullString())

        Str = "  >  ="
        tk = ScanOnce(Str, True)
        Assert.Equal(SyntaxKind.GreaterThanEqualsToken, tk.Kind)
        Assert.Equal(Str, tk.ToFullString())

        'Str = " >" & vbCrLf & "="
        'tk = ScanOnce(Str, True)
        'Assert.Equal(NodeKind.GreaterToken, tk.Kind)
        'Assert.Equal(" >" & vbCrLf, tk.ToFullString())

        'Str = ">" & " _" & vbCrLf & "="
        'tk = ScanOnce(Str, True)
        'Assert.Equal(NodeKind.GreaterToken, tk.Kind)
        'Assert.Equal(">" & " _" & vbCrLf, tk.ToFullString())
    End Sub

    <Fact>
    Public Sub Scanner_LessThanToken()
        Dim Str = ">  <"
        Dim tks = ScanAllCheckDw(Str)
        Assert.Equal(SyntaxKind.GreaterThanToken, tks(0).Kind)
        Assert.Equal(SyntaxKind.LessThanToken, tks(1).Kind)

        Str = "<<<<%"
        tks = ScanAllCheckDw(Str)
        Assert.Equal(SyntaxKind.LessThanLessThanToken, tks(0).Kind)
        Assert.Equal(SyntaxKind.LessThanToken, tks(1).Kind)
        Assert.Equal(SyntaxKind.LessThanToken, tks(2).Kind)
        Assert.Equal(SyntaxKind.BadToken, tks(3).Kind)
        Assert.Equal(SyntaxKind.EndOfFileToken, tks(4).Kind)

        Str = " <   << <% "
        tks = ScanAllCheckDw(Str)
        Assert.Equal(SyntaxKind.LessThanLessThanToken, tks(0).Kind)
        Assert.Equal(SyntaxKind.LessThanToken, tks(1).Kind)
        Assert.Equal(SyntaxKind.LessThanToken, tks(2).Kind)
        Assert.Equal(SyntaxKind.BadToken, tks(3).Kind)
        Assert.Equal(SyntaxKind.EndOfFileToken, tks(4).Kind)
    End Sub

    <Fact>
    Public Sub Scanner_ShiftLeftToken()
        Dim Str = "<<<<="
        Dim tks = ScanAllCheckDw(Str)
        Assert.Equal(SyntaxKind.LessThanLessThanToken, tks(0).Kind)
        Assert.Equal(SyntaxKind.LessThanLessThanEqualsToken, tks(1).Kind)
        Assert.Equal(SyntaxKind.EndOfFileToken, tks(2).Kind)

        'Str = "<" & vbLf & " < < = "
        'tks = ScanAllCheckDw(Str)
        'Assert.Equal(NodeKind.LessToken, tks(0).Kind)
        'Assert.Equal(NodeKind.LeftShiftEqualsToken, tks(1).Kind)
        'Assert.Equal(NodeKind.EndOfTextToken, tks(2).Kind)

        '' left shift does not allow implicit line continuation
        'Str = "<<" & vbLf & "<<="
        'tks = ScanAllCheckDw(Str)
        'Assert.Equal(NodeKind.LeftShiftToken, tks(0).Kind)
        'Assert.Equal(NodeKind.StatementTerminatorToken, tks(1).Kind)
        'Assert.Equal(NodeKind.LeftShiftEqualsToken, tks(2).Kind)
        'Assert.Equal(NodeKind.EndOfTextToken, tks(3).Kind)
    End Sub

    <Fact>
    Public Sub Scanner_NotEqualsToken()
        Dim Str = "<>"
        Dim tks = ScanAllCheckDw(Str)
        Assert.Equal(SyntaxKind.LessThanGreaterThanToken, tks(0).Kind)
        Assert.Equal(SyntaxKind.EndOfFileToken, tks(1).Kind)

        Str = "<>="
        tks = ScanAllCheckDw(Str)
        Assert.Equal(SyntaxKind.LessThanGreaterThanToken, tks(0).Kind)
        Assert.Equal(SyntaxKind.EqualsToken, tks(1).Kind)
        Assert.Equal(SyntaxKind.EndOfFileToken, tks(2).Kind)

        'Str = "<" & vbLf & " > "
        'tks = ScanAllCheckDw(Str)
        'Assert.Equal(NodeKind.LessToken, tks(0).Kind)
        'Assert.Equal(NodeKind.GreaterToken, tks(1).Kind)
        'Assert.Equal(NodeKind.EndOfTextToken, tks(2).Kind)

        '' left shift does not allow implicit line continuation
        'Str = "<    > <" & " _" & vbLf & ">"
        'tks = ScanAllCheckDw(Str)
        'Assert.Equal(NodeKind.NotEqualToken, tks(0).Kind)
        'Assert.Equal(NodeKind.LessToken, tks(1).Kind)
        'Assert.Equal(NodeKind.GreaterToken, tks(2).Kind)
        'Assert.Equal(NodeKind.EndOfTextToken, tks(3).Kind)
    End Sub

    Private Sub CheckCharTkValue(tk As SyntaxToken, expected As Char)
        Dim val = DirectCast(tk.Value, Char)
        Assert.Equal(expected, val)
    End Sub

    <Fact>
    Public Sub Scanner_CharLiteralToken()
        Dim Str = <text>"Q"c</text>.Value
        Dim tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.CharacterLiteralToken, tk.Kind)
        Assert.Equal(Str, tk.ToFullString())
        CheckCharTkValue(tk, "Q"c)

        Str = <text>""""c</text>.Value
        tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.CharacterLiteralToken, tk.Kind)
        Assert.Equal(Str, tk.ToFullString())
        CheckCharTkValue(tk, """"c)

        Str = <text>""c</text>.Value
        tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.BadToken, tk.Kind)
        Assert.Equal(30004, tk.GetSyntaxErrorsNoTree()(0).Code)
        Assert.Equal(Str, tk.ToFullString())

        Str = <text>"""c</text>.Value
        tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.StringLiteralToken, tk.Kind)
        Assert.Equal(30648, tk.GetSyntaxErrorsNoTree()(0).Code)
        Assert.Equal(Str, tk.ToFullString())
        CheckStrTkValue(tk, """c")

        Str = <text>"QQ"c</text>.Value
        tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.BadToken, tk.Kind)
        Assert.Equal(30004, tk.GetSyntaxErrorsNoTree()(0).Code)
        Assert.Equal(Str, tk.ToFullString())

        Str = <text>"Q"c "Q"c"Q"c   "Q"c _
"Q"c
""""c</text>.Value

        Dim doubleWidthStr = MakeDwString(Str)
        Dim tks = ScanAllNoDwCheck(doubleWidthStr)
        Assert.Equal(10, tks.Count)
        Assert.Equal(True, tks.Any(Function(t) t.ContainsDiagnostics))
    End Sub

    Private Sub CheckStrTkValue(tk As SyntaxToken, expected As String)
        Dim str = DirectCast(tk.Value, String)
        Assert.Equal(expected, str)
    End Sub

    <Fact>
    Public Sub Scanner_StringLiteralToken()
        Dim Str = <text>""</text>.Value
        Dim tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.StringLiteralToken, tk.Kind)
        Assert.Equal(Str, tk.ToFullString())
        CheckStrTkValue(tk, "")

        Str = <text>"Q"</text>.Value
        tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.StringLiteralToken, tk.Kind)
        Assert.Equal(Str, tk.ToFullString())
        CheckStrTkValue(tk, "Q")

        Str = <text>""""</text>.Value
        tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.StringLiteralToken, tk.Kind)
        Assert.Equal(Str, tk.ToFullString())
        CheckStrTkValue(tk, """")

        Str = <text>""""""""</text>.Value
        tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.StringLiteralToken, tk.Kind)
        Assert.Equal(Str, tk.ToFullString())
        CheckStrTkValue(tk, """""""")

        Str = <text>"""" """"</text>.Value
        Dim tks = ScanAllCheckDw(Str)
        Assert.Equal(SyntaxKind.StringLiteralToken, tks(0).Kind)
        Assert.Equal(SyntaxKind.StringLiteralToken, tks(1).Kind)

        Str = <text>"AA"
"BB"</text>.Value
        tks = ScanAllCheckDw(Str)
        Assert.Equal(SyntaxKind.StringLiteralToken, tks(0).Kind)
        Assert.Equal(SyntaxKind.StatementTerminatorToken, tks(1).Kind)
        Assert.Equal(SyntaxKind.StringLiteralToken, tks(2).Kind)
    End Sub

    <Fact>
    Public Sub Scanner_IntegerLiteralToken()
        Dim Str = "42"
        Dim tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.IntegerLiteralToken, tk.Kind)
        Assert.Equal(LiteralBase.Decimal, tk.GetBase())
        Assert.Equal(42, tk.Value)

        Str = " 42 "
        tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.IntegerLiteralToken, tk.Kind)
        Assert.Equal(LiteralBase.Decimal, tk.GetBase())
        Assert.Equal(42, tk.Value)
        Assert.Equal(" 42 ", tk.ToFullString())

        Str = " 4_2 "
        tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.IntegerLiteralToken, tk.Kind)
        Assert.Equal(LiteralBase.Decimal, tk.GetBase())
        Assert.Equal(42, tk.Value)
        Assert.Equal(" 4_2 ", tk.ToFullString())
        Assert.Equal("error BC36716: Visual Basic 14.0 does not support digit separators.", tk.Errors().Single().ToString())

        Str = " &H42L "
        tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.IntegerLiteralToken, tk.Kind)
        Assert.Equal(LiteralBase.Hexadecimal, tk.GetBase())
        Assert.Equal(&H42L, tk.Value)
        Assert.Equal(" &H42L ", tk.ToFullString())

        Str = " &H4_2L "
        tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.IntegerLiteralToken, tk.Kind)
        Assert.Equal(LiteralBase.Hexadecimal, tk.GetBase())
        Assert.Equal(&H42L, tk.Value)
        Assert.Equal(" &H4_2L ", tk.ToFullString())

        Str = " &H42L &H42& "
        Dim tks = ScanAllCheckDw(Str)
        Assert.Equal(SyntaxKind.IntegerLiteralToken, tks(0).Kind)
        Assert.Equal(LiteralBase.Hexadecimal, tks(1).GetBase())
        Assert.Equal(&H42L, tks(1).Value)
        Assert.Equal(TypeCharacter.Long, tks(1).GetTypeCharacter())

        Str = " &B1010L "
        tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.IntegerLiteralToken, tk.Kind)
        Assert.Equal(LiteralBase.Binary, tk.GetBase())
        Assert.Equal(&HAL, tk.Value)
        Assert.Equal(" &B1010L ", tk.ToFullString())
        Assert.Equal("error BC36716: Visual Basic 14.0 does not support binary literals.", tk.Errors().Single().ToString())

        Str = " &B1_0_1_0L "
        tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.IntegerLiteralToken, tk.Kind)
        Assert.Equal(LiteralBase.Binary, tk.GetBase())
        Assert.Equal(&HAL, tk.Value)
        Assert.Equal(" &B1_0_1_0L ", tk.ToFullString())
        Assert.Equal(2, tk.Errors().Count)
        Assert.Equal("error BC36716: Visual Basic 14.0 does not support digit separators.", tk.Errors()(0).ToString())
        Assert.Equal("error BC36716: Visual Basic 14.0 does not support binary literals.", tk.Errors()(1).ToString())
    End Sub

    <Fact>
    Public Sub Scanner_FloatingLiteralToken()
        Dim Str = "4.2"
        Dim tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.FloatingLiteralToken, tk.Kind)
        Assert.Equal(4.2, tk.Value)

        Str = " 0.42 "
        tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.FloatingLiteralToken, tk.Kind)
        Assert.Equal(0.42, tk.Value)
        Assert.IsType(Of Double)(tk.Value)
        Assert.Equal(" 0.42 ", tk.ToFullString())

        Str = " 0_0.4_2 "
        tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.FloatingLiteralToken, tk.Kind)
        Assert.Equal(0.42, tk.Value)
        Assert.IsType(Of Double)(tk.Value)
        Assert.Equal(" 0_0.4_2 ", tk.ToFullString())

        Str = " 0.42# "
        tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.FloatingLiteralToken, tk.Kind)
        Assert.Equal(0.42, tk.Value)
        Assert.IsType(Of Double)(tk.Value)
        Assert.Equal(" 0.42# ", tk.ToFullString())

        Str = " 0.42R "
        tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.FloatingLiteralToken, tk.Kind)
        Assert.Equal(0.42, tk.Value)
        Assert.IsType(Of Double)(tk.Value)
        Assert.Equal(" 0.42R ", tk.ToFullString())

        Str = " 0.42! "
        tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.FloatingLiteralToken, tk.Kind)
        Assert.Equal(0.42!, tk.Value)
        Assert.IsType(Of Single)(tk.Value)
        Assert.Equal(" 0.42! ", tk.ToFullString())

        Str = " 0.42F "
        tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.FloatingLiteralToken, tk.Kind)
        Assert.Equal(0.42F, tk.Value)
        Assert.IsType(Of Single)(tk.Value)
        Assert.Equal(" 0.42F ", tk.ToFullString())

        Str = " .42 42# "
        Dim tks = ScanAllCheckDw(Str)
        Assert.Equal(SyntaxKind.FloatingLiteralToken, tks(1).Kind)
        Assert.Equal(42.0#, tks(1).Value)
        Assert.Equal(0.42, tks(0).Value)
        Assert.IsType(Of Double)(tks(1).Value)
        Assert.Equal(TypeCharacter.Double, tks(1).GetTypeCharacter())
    End Sub

    <Fact>
    Public Sub Scanner_DecimalLiteralToken()
        Dim Str = "4.2D"
        Dim tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.DecimalLiteralToken, tk.Kind)
        Assert.Equal(TypeCharacter.DecimalLiteral, tk.GetTypeCharacter())
        Assert.Equal(4.2D, tk.Value)

        Str = " 0.42@ "
        tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.DecimalLiteralToken, tk.Kind)
        Assert.Equal(0.42@, tk.Value)
        Assert.Equal(" 0.42@ ", tk.ToFullString())

        Str = " .42D 4242424242424242424242424242@ "
        Dim tks = ScanAllCheckDw(Str)
        Assert.Equal(SyntaxKind.DecimalLiteralToken, tks(1).Kind)
        Assert.Equal(4242424242424242424242424242D, tks(1).Value)
        Assert.Equal(0.42D, tks(0).Value)
        Assert.Equal(TypeCharacter.Decimal, tks(1).GetTypeCharacter())
    End Sub

    <WorkItem(538543, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538543")>
    <Fact>
    Public Sub Scanner_DecimalLiteralExpToken()
        Dim Str = "1E1D"
        Dim tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.DecimalLiteralToken, tk.Kind)
        Assert.Equal(TypeCharacter.DecimalLiteral, tk.GetTypeCharacter())
        Assert.Equal(10D, tk.Value)
    End Sub

    <Fact>
    Public Sub Scanner_Overflow()

        Dim Str = "2147483647I"
        Dim tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.IntegerLiteralToken, tk.Kind)
        Assert.Equal(2147483647I, CInt(tk.Value))

        Str = "2147483648I"
        tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.IntegerLiteralToken, tk.Kind)
        Assert.Equal(30036, tk.GetSyntaxErrorsNoTree()(0).Code)
        Assert.Equal(0, CInt(tk.Value))

        Str = "&H7FFFFFFFI"
        tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.IntegerLiteralToken, tk.Kind)
        Assert.Equal(&H7FFFFFFFI, CInt(tk.Value))

        Str = "&HFFFFFFFFI"
        tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.IntegerLiteralToken, tk.Kind)
        Assert.Equal(&HFFFFFFFFI, tk.Value)

        Str = "&HFFFFFFFFS"
        tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.IntegerLiteralToken, tk.Kind)
        Assert.Equal(30036, tk.GetSyntaxErrorsNoTree()(0).Code)
        Assert.Equal(0, CInt(tk.Value))

        Str = "&B111111111111111111111111111111111I"
        tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.IntegerLiteralToken, tk.Kind)
        Assert.Equal(30036, tk.GetSyntaxErrorsNoTree()(0).Code)
        Assert.Equal(0, CInt(tk.Value))

        Str = "&B11111111111111111111111111111111UI"
        tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.IntegerLiteralToken, tk.Kind)
        Assert.Equal(&HFFFFFFFFUI, CUInt(tk.Value))

        Str = "&B1111111111111111111111111111111I"
        tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.IntegerLiteralToken, tk.Kind)
        Assert.Equal(&H7FFFFFFFI, CInt(tk.Value))

        Str = "1.7976931348623157E+308d"
        tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.DecimalLiteralToken, tk.Kind)
        Assert.Equal(30036, tk.GetSyntaxErrorsNoTree()(0).Code)
        Assert.Equal(0D, tk.Value)

        Str = "1.797693134862315456489789797987987897897987987E+308F"
        tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.FloatingLiteralToken, tk.Kind)
        Assert.Equal(30036, tk.GetSyntaxErrorsNoTree()(0).Code)
        Assert.Equal(0.0F, tk.Value)
    End Sub

    <Fact>
    Public Sub Scanner_UnderscoreWrongLocation()
        Dim Str = "_1"
        Dim tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.IdentifierToken, tk.Kind)
        Assert.Equal(0, tk.GetSyntaxErrorsNoTree().Count())

        Str = "1_"
        tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.IntegerLiteralToken, tk.Kind)
        Assert.Equal(30035, tk.GetSyntaxErrorsNoTree()(0).Code)
        Assert.Equal(0, CInt(tk.Value))

        Str = "&H_1"
        tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.IntegerLiteralToken, tk.Kind)
        Assert.Equal(30035, tk.GetSyntaxErrorsNoTree()(0).Code)
        Assert.Equal(0, CInt(tk.Value))

        Str = "&H1_"
        tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.IntegerLiteralToken, tk.Kind)
        Assert.Equal(30035, tk.GetSyntaxErrorsNoTree()(0).Code)
        Assert.Equal(0, CInt(tk.Value))

        Str = "1_.1"
        tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.FloatingLiteralToken, tk.Kind)
        Assert.Equal(30035, tk.GetSyntaxErrorsNoTree()(0).Code)
        Assert.Equal(0, CInt(tk.Value))

        Str = "1.1_"
        tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.FloatingLiteralToken, tk.Kind)
        Assert.Equal(30035, tk.GetSyntaxErrorsNoTree()(0).Code)
        Assert.Equal(0, CInt(tk.Value))
    End Sub

    <Fact>
    Public Sub Scanner_DateLiteralToken()
        Dim Str = "#10/10/2010#"
        Dim tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.DateLiteralToken, tk.Kind)
        Assert.Equal(#10/10/2010#, tk.Value)

        Str = "#10/10/1#"
        tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.DateLiteralToken, tk.Kind)
        Assert.Equal(#10/10/0001#, tk.Value)

        Str = "#10/10/101#"
        tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.DateLiteralToken, tk.Kind)
        Assert.Equal(#10/10/0101#, tk.Value)

        Str = "#10/10/0#"
        tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.BadToken, tk.Kind)
        Assert.Equal(31085, tk.GetSyntaxErrorsNoTree()(0).Code)
        Assert.Equal("#10/10/0#", tk.ToFullString())

        Str = " #10/10/2010 10:10:00 PM# "
        tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.DateLiteralToken, tk.Kind)
        Assert.Equal(#10/10/2010 10:10:00 PM#, tk.Value)

        Str = "x = #10/10/2010##10/10/2010 10:10:00 PM# "
        Dim tks = ScanAllCheckDw(Str)
        Assert.Equal(#10/10/2010#, tks(2).Value)
        Assert.Equal(#10/10/2010 10:10:00 PM#, tks(3).Value)
    End Sub

    <Fact>
    Public Sub Scanner_DateLiteralTokenWithYearFirst()
        Dim text = "#1984-10-12#"
        Dim token = ScanOnce(text)
        Assert.Equal(SyntaxKind.DateLiteralToken, token.Kind)
        Assert.Equal(#10/12/1984#, token.Value)

        ' May use slash as separator in dates.
        text = "#1984/10/12#"
        token = ScanOnce(text)
        Assert.Equal(SyntaxKind.DateLiteralToken, token.Kind)
        Assert.Equal(#10/12/1984#, token.Value)

        ' Years must be four digits.
        text = "#84-10-12#"
        token = ScanOnce(text)
        Assert.Equal(SyntaxKind.BadToken, token.Kind)
        Assert.Equal(31085, token.GetSyntaxErrorsNoTree()(0).Code)

        text = "#84/10/12#"
        token = ScanOnce(text)
        Assert.Equal(SyntaxKind.BadToken, token.Kind)
        Assert.Equal(31085, token.GetSyntaxErrorsNoTree()(0).Code)

        ' Months may be one digit.
        text = "#2010-4-12#"
        token = ScanOnce(text)
        Assert.Equal(SyntaxKind.DateLiteralToken, token.Kind)
        Assert.Equal(#4/12/2010#, token.Value)

        ' Days may be one digit.
        text = "#1955/11/5#"
        token = ScanOnce(text)
        Assert.Equal(SyntaxKind.DateLiteralToken, token.Kind)
        Assert.Equal(#11/5/1955#, token.Value)

        ' Time only.
        text = " #09:45:01# "
        token = ScanOnce(text)
        Assert.Equal(SyntaxKind.DateLiteralToken, token.Kind)
        Assert.Equal(#1/1/1 9:45:01 AM#, token.Value)

        ' Date and time.
        text = " #   2010-04-12    9:00   # "
        token = ScanOnce(text)
        Assert.Equal(SyntaxKind.DateLiteralToken, token.Kind)
        Assert.Equal(#4/12/2010 9:00:00 AM#, token.Value)

        text = " #2010/04/12 9:00# "
        token = ScanOnce(text)
        Assert.Equal(SyntaxKind.DateLiteralToken, token.Kind)
        Assert.Equal(#4/12/2010 9:00:00 AM#, token.Value)

        text = "x = #2010-04-12##2010-04-12 09:00:00 # "
        Dim tokens = ScanAllCheckDw(text)
        Assert.Equal(#4/12/2010#, tokens(2).Value)
        Assert.Equal(#4/12/2010 9:00:00 AM#, tokens(3).Value)

        text = "#01984/10/12#"
        token = ScanOnce(text)
        Assert.Equal(SyntaxKind.BadToken, token.Kind)
        Assert.Equal(31085, token.GetSyntaxErrorsNoTree()(0).Code)

        text = "#984/10/12#"
        token = ScanOnce(text)
        Assert.Equal(SyntaxKind.BadToken, token.Kind)
        Assert.Equal(31085, token.GetSyntaxErrorsNoTree()(0).Code)

        text = "#1984/10/#"
        token = ScanOnce(text)
        Assert.Equal(SyntaxKind.BadToken, token.Kind)
        Assert.Equal(31085, token.GetSyntaxErrorsNoTree()(0).Code)

        text = "#1984//12#"
        token = ScanOnce(text)
        Assert.Equal(SyntaxKind.BadToken, token.Kind)
        Assert.Equal(31085, token.GetSyntaxErrorsNoTree()(0).Code)

        text = "#1984/10-12#"
        token = ScanOnce(text)
        Assert.Equal(SyntaxKind.BadToken, token.Kind)
        Assert.Equal(31085, token.GetSyntaxErrorsNoTree()(0).Code)

        text = "#1984-10/12#"
        token = ScanOnce(text)
        Assert.Equal(SyntaxKind.BadToken, token.Kind)
        Assert.Equal(31085, token.GetSyntaxErrorsNoTree()(0).Code)
    End Sub

    <Fact, WorkItem(529782, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529782")>
    Public Sub DateAndDecimalCultureIndependentTokens()
        Dim SavedCultureInfo = CurrentThread.CurrentCulture
        Try
            CurrentThread.CurrentCulture = New System.Globalization.CultureInfo("de-DE", False)

            Dim Str = "4.2"
            Dim tk = ScanOnce(Str)
            Assert.Equal(SyntaxKind.FloatingLiteralToken, tk.Kind)
            Assert.Equal(4.2, tk.Value)

            Str = "4.2F"
            tk = ScanOnce(Str)
            Assert.Equal(SyntaxKind.FloatingLiteralToken, tk.Kind)
            Assert.Equal(4.2F, tk.Value)
            Assert.IsType(Of Single)(tk.Value)

            Str = "4.2R"
            tk = ScanOnce(Str)
            Assert.Equal(SyntaxKind.FloatingLiteralToken, tk.Kind)
            Assert.Equal(4.2R, tk.Value)
            Assert.IsType(Of Double)(tk.Value)

            Str = "4.2D"
            tk = ScanOnce(Str)
            Assert.Equal(SyntaxKind.DecimalLiteralToken, tk.Kind)
            Assert.Equal(4.2D, tk.Value)
            Assert.IsType(Of Decimal)(tk.Value)

            Str = "#8/23/1970 3:35:39AM#"
            tk = ScanOnce(Str)
            Assert.Equal(SyntaxKind.DateLiteralToken, tk.Kind)
            Assert.Equal(#8/23/1970 3:35:39 AM#, tk.Value)
        Finally
            CurrentThread.CurrentCulture = SavedCultureInfo
        End Try
    End Sub

    <Fact>
    Public Sub Scanner_BracketedIdentToken()
        Dim Str = "[Foo123]"
        Dim tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.IdentifierToken, tk.Kind)
        Assert.True(tk.IsBracketed)
        Assert.Equal("Foo123", tk.ValueText)
        Assert.Equal("Foo123", tk.Value)
        Assert.Equal("[Foo123]", tk.ToFullString())

        Str = "[__]"
        tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.IdentifierToken, tk.Kind)
        Assert.True(tk.IsBracketed)
        Assert.Equal("__", tk.ValueText)
        Assert.Equal("[__]", tk.ToFullString())

        Str = "[Foo ]"
        tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.BadToken, tk.Kind)
        Assert.Equal(30034, tk.GetSyntaxErrorsNoTree()(0).Code)
        Assert.Equal("[Foo ", tk.ToFullString())

        Str = "[]"
        tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.BadToken, tk.Kind)
        Assert.Equal(30203, tk.GetSyntaxErrorsNoTree()(0).Code)
        Assert.Equal("[]", tk.ToFullString())

        Str = "[_]"
        tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.BadToken, tk.Kind)
        Assert.Equal(30203, tk.GetSyntaxErrorsNoTree()(0).Code)
        Assert.Equal("[_]", tk.ToFullString())
    End Sub

    <Fact>
    Public Sub Scanner_StringLiteralValueText()
        Dim str = """Hello, World!"""

        Dim tk = ScanOnce(str)

        Assert.Equal(SyntaxKind.StringLiteralToken, tk.Kind)
        Assert.Equal("Hello, World!", tk.ValueText)
        Assert.Equal("""Hello, World!""", tk.ToFullString())
    End Sub

    <Fact>
    Public Sub Scanner_MultiLineStringLiteral()
        Dim text =
<text>"Hello,
World!"</text>.Value

        Dim token = ScanOnce(text)

        Assert.Equal(SyntaxKind.StringLiteralToken, token.Kind)
        Assert.Equal("Hello," & vbLf & "World!", token.ValueText)
        Assert.Equal("""Hello," & vbLf & "World!""", token.ToString())
    End Sub

    Private Function Repeat(str As String, num As Integer) As String
        Dim arr(num - 1) As String
        For i As Integer = 0 To num - 1
            arr(i) = str
        Next
        Return String.Join("", arr)
    End Function


    <Fact>
    Public Sub Scanner_BufferTest()
        For i As Integer = 0 To 12
            Dim TokenStr = New String("+"c, i)
            Dim tks = ScanAllCheckDw(TokenStr)
            Assert.Equal(i + 1, tks.Count)

            TokenStr = Repeat(" SomeIdentifier        ", i)
            tks = ScanAllCheckDw(TokenStr)
            Assert.Equal(i + 1, tks.Count)

            ' trying to place space after "someIdent" on ^2 boundary
            Dim identLen = Math.Max(1, CInt(2 ^ i) - 11)
            TokenStr = Repeat("X", identLen) & " someIdent " & Repeat("X", identLen + 11)
            tks = ScanAllNoDwCheck(TokenStr)
            Assert.Equal(4, tks.Count)
        Next


        For i As Integer = 100 To 5000 Step 250
            Dim TokenStr = New String("+"c, i)
            Dim tks = ScanAllCheckDw(TokenStr)
            Assert.Equal(i + 1, tks.Count)

            TokenStr = Repeat(" SomeIdentifier ", i)
            tks = ScanAllCheckDw(TokenStr)
            Assert.Equal(i + 1, tks.Count)
        Next
    End Sub

    <Fact>
    Public Sub Scanner_Bug866445()
        Dim x = &HFF00110001020408L
        Dim Str = "&HFF00110001020408L"
        Dim tk = ScanOnce(Str)
        Assert.Equal(SyntaxKind.IntegerLiteralToken, tk.Kind)
    End Sub

    <Fact>
    Public Sub Bug869260()
        Dim tk = ScanOnce(ChrW(0))
        Assert.Equal(SyntaxKind.BadToken, tk.Kind)
        Assert.Equal(CInt(ERRID.ERR_IllegalChar), tk.GetSyntaxErrorsNoTree(0).Code)
    End Sub

    <Fact>
    Public Sub Bug869081()
        ParseAndVerify(<![CDATA[
            <Obsolete()> _
        _
        _
        _
        _
            <CLSCompliant(False)> Class Class1
            End Class
        ]]>)
    End Sub

    <Fact>
    Public Sub Bug658441()
        ParseAndVerify(<![CDATA[
#If False Then
#If False Then
# _
#End If
# _
End If
#End If
        ]]>)
    End Sub

    <WorkItem(538747, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538747")>
    <Fact>
    Public Sub OghamSpacemark()
        ParseAndVerify(<![CDATA[
Module M 
End Module

        ]]>)
    End Sub

    <WorkItem(531175, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531175")>
    <Fact>
    Public Sub Bug17703()
        ParseAndVerify(<![CDATA[
Dim x = <
”'

        ]]>,
                       <errors>
                           <error id="31151" message="Element is missing an end tag." start="9" end="23"/>
                           <error id="31146" message="XML name expected." start="10" end="10"/>
                           <error id="31146" message="XML name expected." start="10" end="10"/>
                           <error id="30249" message="'=' expected." start="10" end="10"/>
                           <error id="31164" message="Expected matching closing double quote for XML attribute value." start="23" end="23"/>
                           <error id="31165" message="Expected beginning &lt; for an XML tag." start="23" end="23"/>
                           <error id="30636" message="'>' expected." start="23" end="23"/>
                       </errors>)
    End Sub

    <WorkItem(530916, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530916")>
    <Fact>
    Public Sub Bug17189()
        ParseAndVerify(<![CDATA[
a<
-'
-
        ]]>,
        <errors>
            <error id="30689" message="Statement cannot appear outside of a method body." start="1" end="17"/>
            <error id="30800" message="Method arguments must be enclosed in parentheses." start="2" end="17"/>
            <error id="31151" message="Element is missing an end tag." start="2" end="17"/>
            <error id="31177" message="White space cannot appear here." start="3" end="4"/>
            <error id="31169" message="Character '-' (&amp;H2D) is not allowed at the beginning of an XML name." start="4" end="5"/>
            <error id="31146" message="XML name expected." start="5" end="5"/>
            <error id="30249" message="'=' expected." start="5" end="5"/>
            <error id="31163" message="Expected matching closing single quote for XML attribute value." start="17" end="17"/>
            <error id="31165" message="Expected beginning '&lt;' for an XML tag." start="17" end="17"/>
            <error id="30636" message="'>' expected." start="17" end="17"/>
        </errors>)
    End Sub

    <WorkItem(530682, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530682")>
    <Fact>
    Public Sub Bug16698()
        ParseAndVerify(<![CDATA[#Const x = <!--
]]>,
                    Diagnostic(ERRID.ERR_BadCCExpression, "<!--"))
    End Sub

    <WorkItem(865832, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseSpecialKeywords()
        ParseAndVerify(<![CDATA[
            Module M1
                Dim x As Integer
                Sub Main
                    If True
                    End If
                End Sub
            End Module
        ]]>).
        VerifyNoWhitespaceInKeywords()
    End Sub

    <WorkItem(547317, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547317")>
    <Fact>
    Public Sub ParseHugeNumber()
        ParseAndVerify(<![CDATA[
Module M
    Sub Main     
 Dim x = CompareDouble(-7.92281625142643E337593543950335D)
    End Sub 
EndModule


        ]]>,
        <errors>
            <error id="30625" message="'Module' statement must end with a matching 'End Module'." start="1" end="9"/>
            <error id="30036" message="Overflow." start="52" end="85"/>
            <error id="30188" message="Declaration expected." start="100" end="109"/>
        </errors>).
        VerifyNoWhitespaceInKeywords()
    End Sub

    <WorkItem(547317, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547317")>
    <Fact>
    Public Sub ParseHugeNumberLabel()
        ParseAndVerify(<![CDATA[
Module M
    Sub Main     
 
678901234567890123456789012345678901234567456789012345678901234567890123456789012345
    End Sub 
EndModule

        ]]>,
        <errors>
            <error id="30625" message="'Module' statement must end with a matching 'End Module'." start="1" end="9"/>
            <error id="30801" message="Labels that are numbers must be followed by colons." start="30" end="114"/>
            <error id="30036" message="Overflow." start="30" end="114"/>
            <error id="30188" message="Declaration expected." start="128" end="137"/>
        </errors>).
        VerifyNoWhitespaceInKeywords()
    End Sub

    <WorkItem(926612, "DevDiv/Personal")>
    <Fact>
    Public Sub ScanMultilinesTriviaWithCRLFs()
        ParseAndVerify(<![CDATA[Option Compare Text

Public Class Assembly001bDll
    Sub main()
        Dim Asb As System.Reflection.Assembly
        Asb = System.Reflection.Assembly.GetExecutingAssembly()


        
        
        

        apcompare(Left(CurDir(), 1) & ":\School\assembly001bdll.dll", Asb.Location, "location")

    End Sub
End Class]]>)
    End Sub

    <Fact>
    Public Sub IsWhiteSpace()
        Assert.False(SyntaxFacts.IsWhitespace("A"c))
        Assert.True(SyntaxFacts.IsWhitespace(" "c))
        Assert.True(SyntaxFacts.IsWhitespace(ChrW(9)))
        Assert.False(SyntaxFacts.IsWhitespace(ChrW(0)))
        Assert.False(SyntaxFacts.IsWhitespace(ChrW(128)))
        Assert.False(SyntaxFacts.IsWhitespace(ChrW(129)))
        Assert.False(SyntaxFacts.IsWhitespace(ChrW(127)))
        Assert.True(SyntaxFacts.IsWhitespace(ChrW(160)))
        Assert.True(SyntaxFacts.IsWhitespace(ChrW(12288)))
        Assert.True(SyntaxFacts.IsWhitespace(ChrW(8192)))
        Assert.True(SyntaxFacts.IsWhitespace(ChrW(8203)))
    End Sub

    <Fact>
    Public Sub IsNewline()
        Assert.True(SyntaxFacts.IsNewLine(ChrW(13)))
        Assert.True(SyntaxFacts.IsNewLine(ChrW(10)))
        Assert.True(SyntaxFacts.IsNewLine(ChrW(133)))
        Assert.True(SyntaxFacts.IsNewLine(ChrW(8232)))
        Assert.True(SyntaxFacts.IsNewLine(ChrW(8233)))
        Assert.False(SyntaxFacts.IsNewLine(ChrW(132)))
        Assert.False(SyntaxFacts.IsNewLine(ChrW(160)))
        Assert.False(SyntaxFacts.IsNewLine(" "c))
        Assert.Equal(String.Empty, SyntaxFacts.MakeHalfWidthIdentifier(String.Empty))
        Assert.Null(SyntaxFacts.MakeHalfWidthIdentifier(Nothing))
        Assert.Equal("ABC", SyntaxFacts.MakeHalfWidthIdentifier("ABC"))
        Assert.Equal(ChrW(65280), SyntaxFacts.MakeHalfWidthIdentifier(ChrW(65280)))
        Assert.NotEqual(ChrW(65281), SyntaxFacts.MakeHalfWidthIdentifier(ChrW(65281)))
        Assert.Equal(1, SyntaxFacts.MakeHalfWidthIdentifier(ChrW(65281)).Length)
    End Sub

    <Fact>
    Public Sub MakeHalfWidthIdentifier()
        Assert.Equal(String.Empty, SyntaxFacts.MakeHalfWidthIdentifier(String.Empty))
        Assert.Equal(Nothing, SyntaxFacts.MakeHalfWidthIdentifier(Nothing))
        Assert.Equal("ABC", SyntaxFacts.MakeHalfWidthIdentifier("ABC"))
        Assert.Equal(ChrW(65280), SyntaxFacts.MakeHalfWidthIdentifier(ChrW(65280)))
        Assert.NotEqual(ChrW(65281), SyntaxFacts.MakeHalfWidthIdentifier(ChrW(65281)))
        Assert.Equal(1, SyntaxFacts.MakeHalfWidthIdentifier(ChrW(65281)).Length)
    End Sub
End Class
