' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.ObjectModel
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator
    Friend Module SyntaxHelpers
        Friend ReadOnly ParseOptions As VisualBasicParseOptions = VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersionFacts.CurrentVersion)

        ''' <summary>
        ''' Parse expression. Returns null if there are any errors.
        ''' </summary>
        <Extension>
        Friend Function ParseExpression(expr As String, diagnostics As DiagnosticBag, allowFormatSpecifiers As Boolean, <Out> ByRef formatSpecifiers As ReadOnlyCollection(Of String)) As ExecutableStatementSyntax
            Dim syntax = ParseDebuggerExpression(expr, consumeFullText:=Not allowFormatSpecifiers)
            diagnostics.AddRange(syntax.GetDiagnostics())
            formatSpecifiers = Nothing
            If allowFormatSpecifiers Then
                Dim builder = ArrayBuilder(Of String).GetInstance()
                If ParseFormatSpecifiers(builder, expr, syntax.Expression.FullWidth, diagnostics) AndAlso builder.Count > 0 Then
                    formatSpecifiers = New ReadOnlyCollection(Of String)(builder.ToArray())
                End If
                builder.Free()
            End If
            Return If(diagnostics.HasAnyErrors(), Nothing, syntax)
        End Function

        <Extension>
        Friend Function ParseAssignment(target As String, expr As String, diagnostics As DiagnosticBag) As AssignmentStatementSyntax
            Dim text = SourceText.From(expr)
            Dim expression = SyntaxHelpers.ParseDebuggerExpressionInternal(text, consumeFullText:=True)
            ' We're creating a SyntaxTree for just the RHS so that the Diagnostic spans for parse errors
            ' will be correct (with respect to the original input text).  If we ever expose a SemanticModel
            ' for debugger expressions, we should use this SyntaxTree.
            Dim syntaxTree = expression.CreateSyntaxTree()
            diagnostics.AddRange(syntaxTree.GetDiagnostics())

            If diagnostics.HasAnyErrors Then
                Return Nothing
            End If

            ' Any Diagnostic spans produced in binding will be offset by the length of the "target" expression text.
            ' If we want to support live squiggles in debugger windows, SemanticModel, etc, we'll want to address this.
            Dim targetSyntax = SyntaxHelpers.ParseDebuggerExpressionInternal(SourceText.From(target), consumeFullText:=True)
            Debug.Assert(Not targetSyntax.GetDiagnostics().Any(), "The target of an assignment should never contain Diagnostics if we're being allowed to assign to it in the debugger.")

            Dim assignment = InternalSyntax.SyntaxFactory.SimpleAssignmentStatement(
                targetSyntax,
                New InternalSyntax.PunctuationSyntax(SyntaxKind.EqualsToken, "=", Nothing, Nothing),
                expression)

            syntaxTree = assignment.MakeDebuggerStatementContext().CreateSyntaxTree()
            Return DirectCast(syntaxTree.GetDebuggerStatement(), AssignmentStatementSyntax)
        End Function

        ''' <summary>
        ''' Parse statement. Returns null if there are any errors.
        ''' </summary>
        <Extension>
        Friend Function ParseStatement(statement As String, diagnostics As DiagnosticBag) As StatementSyntax
            Dim syntax = ParseDebuggerStatement(statement)
            diagnostics.AddRange(syntax.GetDiagnostics())
            Return If(diagnostics.HasAnyErrors(), Nothing, syntax)
        End Function

        ''' <summary>
        ''' Return set of identifier tokens, with leading And
        ''' trailing spaces And comma separators removed.
        ''' </summary>
        ''' <remarks>
        ''' The native VB EE didn't support format specifiers.
        ''' </remarks>
        Private Function ParseFormatSpecifiers(
            builder As ArrayBuilder(Of String),
            expr As String,
            offset As Integer,
            diagnostics As DiagnosticBag) As Boolean

            Dim expectingComma = True
            Dim start = -1
            Dim n = expr.Length

            While offset < n
                Dim c = expr(offset)
                If SyntaxFacts.IsWhitespace(c) OrElse c = ","c Then
                    If start >= 0 Then
                        Dim token = expr.Substring(start, offset - start)
                        If expectingComma Then
                            ReportInvalidFormatSpecifier(token, diagnostics)
                            Return False
                        End If
                        builder.Add(token)
                        start = -1
                        expectingComma = c <> ","c
                    ElseIf c = ","c Then
                        If Not expectingComma Then
                            ReportInvalidFormatSpecifier(",", diagnostics)
                            Return False
                        End If
                        expectingComma = False
                    End If
                ElseIf start < 0 Then
                    start = offset
                End If
                offset = offset + 1
            End While

            If start >= 0 Then
                Dim token = expr.Substring(start)
                If expectingComma Then
                    ReportInvalidFormatSpecifier(token, diagnostics)
                    Return False
                End If
                builder.Add(token)
            ElseIf Not expectingComma Then
                ReportInvalidFormatSpecifier(",", diagnostics)
                Return False
            End If

            ' Verify format specifiers are valid identifiers.
            For Each token In builder
                If Not token.All(AddressOf SyntaxFacts.IsIdentifierPartCharacter) Then
                    ReportInvalidFormatSpecifier(token, diagnostics)
                    Return False
                End If
            Next

            Return True
        End Function

        Private Sub ReportInvalidFormatSpecifier(token As String, diagnostics As DiagnosticBag)
            diagnostics.Add(ERRID.ERR_InvalidFormatSpecifier, Location.None, token)
        End Sub

        ''' <summary>
        ''' Parse a debugger expression (e.g. possibly including pseudo-variables).
        ''' </summary>
        ''' <param name="text">The input string</param>
        ''' <remarks>
        ''' It would be better if this method returned ExpressionStatementSyntax, but this is the best we can do for
        ''' the time being due to issues in the binder resolving ambiguities between invocations and array access.
        ''' </remarks>
        Friend Function ParseDebuggerExpression(text As String, consumeFullText As Boolean) As PrintStatementSyntax
            Dim expression = ParseDebuggerExpressionInternal(SourceText.From(text), consumeFullText)
            Dim statement = InternalSyntax.SyntaxFactory.PrintStatement(
                New InternalSyntax.PunctuationSyntax(SyntaxKind.QuestionToken, "?", Nothing, Nothing), expression)
            Dim syntaxTree = statement.MakeDebuggerStatementContext().CreateSyntaxTree()
            Return DirectCast(syntaxTree.GetDebuggerStatement(), PrintStatementSyntax)
        End Function

        Private Function ParseDebuggerExpressionInternal(source As SourceText, consumeFullText As Boolean) As InternalSyntax.ExpressionSyntax
            Using scanner As New InternalSyntax.Scanner(source, ParseOptions, isScanningForExpressionCompiler:=True) ' NOTE: Default options should be enough
                Using p = New InternalSyntax.Parser(scanner)
                    p.GetNextToken()
                    Dim node = p.ParseExpression()
                    If consumeFullText Then node = p.ConsumeUnexpectedTokens(node)
                    Return node
                End Using
            End Using
        End Function

        Private Function ParseDebuggerStatement(text As String) As StatementSyntax
            Using scanner As New InternalSyntax.Scanner(SourceText.From(text), ParseOptions, isScanningForExpressionCompiler:=True) ' NOTE: Default options should be enough
                Using p = New InternalSyntax.Parser(scanner)
                    p.GetNextToken()
                    Dim node = p.ParseStatementInMethodBody()
                    node = p.ConsumeUnexpectedTokens(node)
                    Dim syntaxTree = node.MakeDebuggerStatementContext().CreateSyntaxTree()
                    Return syntaxTree.GetDebuggerStatement()
                End Using
            End Using
        End Function

        <Extension>
        Private Function CreateSyntaxTree(root As InternalSyntax.VisualBasicSyntaxNode) As SyntaxTree
            Return VisualBasicSyntaxTree.Create(DirectCast(root.CreateRed(Nothing, 0), VisualBasicSyntaxNode), ParseOptions)
        End Function

        <Extension>
        Private Function MakeDebuggerStatementContext(statement As InternalSyntax.StatementSyntax) As InternalSyntax.CompilationUnitSyntax
            Return InternalSyntax.SyntaxFactory.CompilationUnit(
                options:=Nothing,
                [imports]:=Nothing,
                attributes:=Nothing,
                members:=Microsoft.CodeAnalysis.Syntax.InternalSyntax.SyntaxList.List(statement),
                endOfFileToken:=InternalSyntax.SyntaxFactory.EndOfFileToken)
        End Function

        <Extension>
        Private Function GetDebuggerStatement(syntaxTree As SyntaxTree) As StatementSyntax
            Return DirectCast(DirectCast(syntaxTree.GetRoot(), CompilationUnitSyntax).Members.Single(), StatementSyntax)
        End Function

        ''' <summary>
        ''' This list is based on the statements found in StatementAnalyzer::IsSupportedStatement
        ''' (vb\language\debugger\statementanalyzer.cpp).
        ''' We'll add to that list some additional statements that can easily be supported by the new implementation
        ''' (include all compound assignments, not just "+=", etc). 
        ''' For now, we'll leave out single line If statements, as the parsing for those would require extra
        ''' complexity on the EE side (ParseStatementInMethodBody should handle them, but it doesn't...).
        ''' </summary>
        Friend Function IsSupportedDebuggerStatement(syntax As StatementSyntax) As Boolean
            Select Case syntax.Kind
                Case SyntaxKind.AddAssignmentStatement,
                     SyntaxKind.CallStatement,
                     SyntaxKind.ConcatenateAssignmentStatement,
                     SyntaxKind.DivideAssignmentStatement,
                     SyntaxKind.ExponentiateAssignmentStatement,
                     SyntaxKind.ExpressionStatement,
                     SyntaxKind.IntegerDivideAssignmentStatement,
                     SyntaxKind.LeftShiftAssignmentStatement,
                     SyntaxKind.MultiplyAssignmentStatement,
                     SyntaxKind.PrintStatement,
                     SyntaxKind.ReDimStatement,
                     SyntaxKind.ReDimPreserveStatement,
                     SyntaxKind.RightShiftAssignmentStatement,
                     SyntaxKind.SimpleAssignmentStatement,
                     SyntaxKind.SubtractAssignmentStatement
                    Return True
                Case Else
                    Return False
            End Select
        End Function

        Friend Function EscapeKeywordIdentifiers(identifier As String) As String
            If SyntaxFacts.IsKeywordKind(SyntaxFacts.GetKeywordKind(identifier)) Then
                Dim pooled = PooledStringBuilder.GetInstance()
                Dim builder = pooled.Builder
                builder.Append("["c)
                builder.Append(identifier)
                builder.Append("]"c)
                Return pooled.ToStringAndFree()
            Else
                Return identifier
            End If
        End Function
    End Module
End Namespace

