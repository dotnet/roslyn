' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.TodoComments

Namespace Microsoft.CodeAnalysis.VisualBasic.TodoComments
    <ExportLanguageServiceFactory(GetType(ITodoCommentService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicTodoCommentServiceFactory
        Implements ILanguageServiceFactory

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Public Function CreateLanguageService(languageServices As HostLanguageServices) As ILanguageService Implements ILanguageServiceFactory.CreateLanguageService
            Return New VisualBasicTodoCommentService(languageServices.WorkspaceServices.Workspace)
        End Function

    End Class

    Friend Class VisualBasicTodoCommentService
        Inherits AbstractTodoCommentService

        Public Sub New(workspace As Workspace)
            MyBase.New(workspace)
        End Sub

        Protected Overrides Sub AppendTodoComments(commentDescriptors As IList(Of TodoCommentDescriptor), document As SyntacticDocument, trivia As SyntaxTrivia, todoList As List(Of TodoComment))
            If PreprocessorHasComment(trivia) Then
                Dim commentTrivia = trivia.GetStructure().DescendantTrivia().First(Function(t) t.RawKind = SyntaxKind.CommentTrivia)

                AppendTodoCommentInfoFromSingleLine(commentDescriptors, document, commentTrivia.ToFullString(), commentTrivia.FullSpan.Start, todoList)
                Return
            End If

            If IsSingleLineComment(trivia) Then
                ProcessMultilineComment(commentDescriptors, document, trivia, postfixLength:=0, todoList:=todoList)
                Return
            End If

            Throw ExceptionUtilities.Unreachable
        End Sub

        Protected Overrides Function GetNormalizedText(message As String) As String
            Return SyntaxFacts.MakeHalfWidthIdentifier(message)
        End Function

        Protected Overrides Function IsIdentifierCharacter(ch As Char) As Boolean
            Return SyntaxFacts.IsIdentifierPartCharacter(ch)
        End Function

        Protected Overrides Function GetCommentStartingIndex(message As String) As Integer
            ' 3 for REM
            Dim index = GetFirstCharacterIndex(message)
            If index >= message.Length OrElse
                       index > message.Length - 3 Then
                Return index
            End If

            Dim remText = message.Substring(index, "REM".Length)
            If SyntaxFacts.GetKeywordKind(remText) = SyntaxKind.REMKeyword Then
                Return GetFirstCharacterIndex(message, index + remText.Length)
            End If

            Return index
        End Function

        Private Function GetFirstCharacterIndex(message As String, Optional start As Integer = 0) As Integer
            Dim index = GetFirstNonWhitespace(message, start)

            Dim singleQuote = 0
            For i = index To message.Length - 1 Step 1
                If IsSingleQuote(message(i)) AndAlso singleQuote < 3 Then
                    singleQuote = singleQuote + 1
                Else
                    If singleQuote = 1 OrElse singleQuote = 3 Then
                        Return GetFirstNonWhitespace(message, i)
                    Else
                        Return index
                    End If
                End If
            Next

            Return message.Length
        End Function

        Private Function GetFirstNonWhitespace(message As String, start As Integer) As Integer
            For i = start To message.Length - 1 Step 1
                If Not SyntaxFacts.IsWhitespace(message(i)) Then
                    Return i
                End If
            Next

            Return message.Length
        End Function

        Protected Overrides Function IsMultilineComment(trivia As SyntaxTrivia) As Boolean
            ' vb doesn't have multiline comment
            Return False
        End Function

        Protected Overrides Function IsSingleLineComment(trivia As SyntaxTrivia) As Boolean
            Return trivia.RawKind = SyntaxKind.CommentTrivia OrElse trivia.RawKind = SyntaxKind.DocumentationCommentTrivia
        End Function

        Protected Overrides Function PreprocessorHasComment(trivia As SyntaxTrivia) As Boolean
            Return SyntaxFacts.IsPreprocessorDirective(CType(trivia.RawKind, SyntaxKind)) AndAlso
                           trivia.GetStructure().DescendantTrivia().Any(Function(t) t.RawKind = SyntaxKind.CommentTrivia)
        End Function

        ' TODO: remove this if SyntaxFacts.IsSingleQuote become public
        Private Const s_DWCH_SQ As Char = ChrW(&HFF07)      '// DW single quote
        Private Const s_DWCH_LSMART_Q As Char = ChrW(&H2018S)      '// DW left single smart quote
        Private Const s_DWCH_RSMART_Q As Char = ChrW(&H2019S)      '// DW right single smart quote

        Private Shared Function IsSingleQuote(c As Char) As Boolean
            ' // Besides the half width and full width ', we also check for Unicode
            ' // LEFT SINGLE QUOTATION MARK and RIGHT SINGLE QUOTATION MARK because
            ' // IME editors paste them in. This isn't really technically correct
            ' // because we ignore the left-ness or right-ness, but see VS 170991
            Return c = "'"c OrElse (c >= s_DWCH_LSMART_Q AndAlso (c = s_DWCH_SQ Or c = s_DWCH_LSMART_Q Or c = s_DWCH_RSMART_Q))
        End Function
    End Class
End Namespace
