' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.TaskList

Namespace Microsoft.CodeAnalysis.VisualBasic.TaskList
    <ExportLanguageService(GetType(ITaskListService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicTaskListService
        Inherits AbstractTaskListService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides Sub AppendTaskListItems(
                commentDescriptors As ImmutableArray(Of TaskListItemDescriptor),
                document As SyntacticDocument,
                trivia As SyntaxTrivia,
                items As ArrayBuilder(Of TaskListItem))
            If PreprocessorHasComment(trivia) Then
                Dim commentTrivia = trivia.GetStructure().DescendantTrivia().First(Function(t) t.RawKind = SyntaxKind.CommentTrivia)

                AppendTaskListItemsOnSingleLine(commentDescriptors, document, commentTrivia.ToFullString(), commentTrivia.FullSpan.Start, items)
                Return
            End If

            If IsSingleLineComment(trivia) Then
                ProcessMultilineComment(commentDescriptors, document, trivia, postfixLength:=0, items)
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

        Private Shared Function GetFirstCharacterIndex(message As String, Optional start As Integer = 0) As Integer
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

        Private Shared Function GetFirstNonWhitespace(message As String, start As Integer) As Integer
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
