' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.CodeCleanup.Providers

    <ExportCodeCleanupProvider(PredefinedCodeCleanupProviderNames.FixIncorrectTokens, LanguageNames.VisualBasic), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeCleanupProviderNames.AddMissingTokens, Before:=PredefinedCodeCleanupProviderNames.Format)>
    Friend Class FixIncorrectTokensCodeCleanupProvider
        Inherits AbstractTokensCodeCleanupProvider

        Private Const s_ASCII_LSMART_Q As Char = ChrW(&H91S)          '// ASCII left single smart quote
        Private Const s_ASCII_RSMART_Q As Char = ChrW(&H92S)          '// ASCII right single smart quote
        Private Const s_UNICODE_LSMART_Q As Char = ChrW(&H2018S)      '// UNICODE left single smart quote
        Private Const s_UNICODE_RSMART_Q As Char = ChrW(&H2019S)      '// UNICODE right single smart quote
        Private Const s_CH_STRGHT_Q As Char = ChrW(&H27S)             '// UNICODE straight quote
        Private Shared ReadOnly s_smartSingleQuotes As Char() = New Char() {s_ASCII_LSMART_Q, s_ASCII_RSMART_Q, s_UNICODE_LSMART_Q, s_UNICODE_RSMART_Q}

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Public Overrides ReadOnly Property Name As String
            Get
                Return PredefinedCodeCleanupProviderNames.FixIncorrectTokens
            End Get
        End Property

        Protected Overrides Function GetRewriterAsync(document As Document, root As SyntaxNode, spans As ImmutableArray(Of TextSpan), workspace As Workspace, cancellationToken As CancellationToken) As Task(Of Rewriter)
            Return FixIncorrectTokensRewriter.CreateAsync(document, spans, cancellationToken)
        End Function

        Private Class FixIncorrectTokensRewriter
            Inherits AbstractTokensCodeCleanupProvider.Rewriter

            Private ReadOnly _document As Document
            Private ReadOnly _modifiedSpan As TextSpan
            Private ReadOnly _semanticModel As SemanticModel

            Private Sub New(document As Document,
                            semanticModel As SemanticModel,
                            spans As ImmutableArray(Of TextSpan),
                            modifiedSpan As TextSpan,
                            cancellationToken As CancellationToken)
                MyBase.New(spans, cancellationToken)

                _document = document
                _semanticModel = semanticModel
                _modifiedSpan = modifiedSpan
            End Sub

            Public Shared Async Function CreateAsync(document As Document, spans As ImmutableArray(Of TextSpan), cancellationToken As CancellationToken) As Task(Of Rewriter)
                Dim modifiedSpan = spans.Collapse()
                Dim semanticModel = If(document Is Nothing,
                    Nothing,
                    Await document.GetSemanticModelForSpanAsync(modifiedSpan, cancellationToken).ConfigureAwait(False))

                Return New FixIncorrectTokensRewriter(document, semanticModel, spans, modifiedSpan, cancellationToken)
            End Function

            Public Overrides Function VisitTrivia(trivia As SyntaxTrivia) As SyntaxTrivia
                Dim newTrivia = MyBase.VisitTrivia(trivia)

                ' convert fullwidth single quotes into halfwidth single quotes.
                If newTrivia.Kind = SyntaxKind.CommentTrivia Then
                    Dim triviaText = newTrivia.ToString()
                    If triviaText.Length > 0 AndAlso s_smartSingleQuotes.Contains(triviaText(0)) Then
                        triviaText = s_CH_STRGHT_Q + triviaText.Substring(1)
                        Return SyntaxFactory.CommentTrivia(triviaText)
                    End If
                End If

                Return newTrivia
            End Function

            Public Overrides Function VisitIdentifierName(node As IdentifierNameSyntax) As SyntaxNode
                Dim newIdentifierName = DirectCast(MyBase.VisitIdentifierName(node), IdentifierNameSyntax)

                ' VB Language specification Section 7.3 for Primitive Types states:
                '       The primitive types are identified through keywords, which are aliases for predefined types in the System namespace.
                '       A primitive type is completely indistinguishable from the type it aliases: writing the reserved word Byte is exactly
                '       the same as writing System.Byte.
                '
                ' Language specification defines the following primitive type mappings:
                ' -------------------------------------------------------------------
                '       Keyword     -->     Predefined type in the System namespace
                ' -------------------------------------------------------------------
                '       Byte        -->     Byte
                '       SByte       -->     SByte
                '   *   UShort      -->     UInt16
                '   *   Short       -->     Int16
                '   *   UInteger    -->     UInt32
                '   *   Integer     -->     Int32
                '   *   ULong       -->     UInt64
                '   *   Long        -->     Int64
                '       Single      -->     Single
                '       Double      -->     Double
                '       Decimal     -->     Decimal
                '       Boolean     -->     Boolean
                '   *   Date        -->     DateTime
                '       Char        -->     Char
                '       String      -->     String
                '
                '   * - Keyword string differs from the predefined type name
                '
                ' Here we rewrite the above * marked Keyword identifier tokens to their corresponding predefined type names when following conditions are met:
                ' (a) It occurs as the RIGHT child of a qualified name "LEFT.RIGHT"
                ' (b) LEFT child of the qualified name binds to the "System" namespace symbol or an alias to it.

                If Not _underStructuredTrivia Then
                    Dim parent = TryCast(node.Parent, QualifiedNameSyntax)
                    If parent IsNot Nothing AndAlso _semanticModel IsNot Nothing Then
                        Dim symbol = _semanticModel.GetSymbolInfo(parent.Left, _cancellationToken).Symbol
                        If symbol IsNot Nothing AndAlso symbol.IsNamespace AndAlso String.Equals(DirectCast(symbol, INamespaceSymbol).MetadataName, "System", StringComparison.Ordinal) Then
                            Dim id = newIdentifierName.Identifier
                            Dim newValueText As String
                            Select Case id.ValueText.ToUpperInvariant()
                                Case "USHORT"
                                    newValueText = "UInt16"
                                Case "SHORT"
                                    newValueText = "Int16"
                                Case "UINTEGER"
                                    newValueText = "UInt32"
                                Case "INTEGER"
                                    newValueText = "Int32"
                                Case "ULONG"
                                    newValueText = "UInt64"
                                Case "LONG"
                                    newValueText = "Int64"
                                Case "DATE"
                                    newValueText = "DateTime"
                                Case Else
                                    Return newIdentifierName
                            End Select

                            Return newIdentifierName.ReplaceToken(id, CreateIdentifierToken(id, newValueText))
                        End If
                    End If
                End If

                Return newIdentifierName
            End Function

#Region "EndIf Rewriting"
            Public Overrides Function VisitEndBlockStatement(node As EndBlockStatementSyntax) As SyntaxNode
                Dim newStatement = DirectCast(MyBase.VisitEndBlockStatement(node), EndBlockStatementSyntax)

                Return If(newStatement.BlockKeyword.Kind = SyntaxKind.IfKeyword,
                           RewriteEndIfStatementOrDirectiveSyntax(newStatement, newStatement.EndKeyword, newStatement.BlockKeyword),
                           newStatement)
            End Function

            Public Overrides Function VisitEndIfDirectiveTrivia(node As EndIfDirectiveTriviaSyntax) As SyntaxNode
                Dim newDirective = DirectCast(MyBase.VisitEndIfDirectiveTrivia(node), EndIfDirectiveTriviaSyntax)
                Return RewriteEndIfStatementOrDirectiveSyntax(newDirective, newDirective.EndKeyword, newDirective.IfKeyword)
            End Function

            ''' <summary>
            ''' Rewrite "EndIf" to "End If" for an EndIfStatementSyntax/EndIfDirectiveSyntax node.
            ''' </summary>
            ''' <param name="curNode">Syntax node for the EndIfStatementSyntax or EndIfDirectiveSyntax to be rewritten.</param>
            ''' <param name="curEndKeyword">"End" keyword token for <paramref name="curNode"/>.</param>
            ''' <param name="curIfKeyword">"If" keyword token for <paramref name="curNode"/>.</param>
            ''' <returns>Rewritten EndIfStatementSyntax/EndIfDirectiveSyntax node.</returns>
            ''' <remarks>
            ''' This method checks for the following:
            ''' (a) Both the End keyword and If keyword, <paramref name="curEndKeyword"/> and <paramref name="curIfKeyword"/> respectively, are Missing tokens AND
            ''' (b) Descendant Trivia under the given <paramref name="curEndKeyword"/> token or <paramref name="curIfKeyword"/> token has an "EndIf" keyword token.
            ''' 
            ''' If the above conditions are met, it does the following node rewrites:
            ''' (a) Replace the missing <paramref name="curEndKeyword"/> and <paramref name="curIfKeyword"/> tokens with new "End" and "If" keywords tokens respectively.
            ''' (b) Remove the first "EndIf" keyword token from the descendant trivia and adjust the leading and trailing trivia appropriately.
            ''' </remarks>
            Private Shared Function RewriteEndIfStatementOrDirectiveSyntax(curNode As SyntaxNode, curEndKeyword As SyntaxToken, curIfKeyword As SyntaxToken) As SyntaxNode
                ' (a) Are both the curEndKeyword and curIfKeyword Missing tokens?
                If curEndKeyword.IsMissing AndAlso curIfKeyword.IsMissing Then
                    Dim endKeywordTrivia = curEndKeyword.GetAllTrivia()
                    Dim ifKeywordTrivia = curIfKeyword.GetAllTrivia()

                    If endKeywordTrivia.Any() OrElse ifKeywordTrivia.Any() Then
                        Dim endIfKeywordFound As Boolean = False
                        Dim leadingTriviaBuilder As Queue(Of SyntaxTrivia) = Nothing
                        Dim trailingTriviaBuilder As Queue(Of SyntaxTrivia) = Nothing

                        ' (b) Descendant Trivia under the given curEndKeyword token or curIfKeyword token has an "EndIf" keyword token?
                        ProcessTrivia(endKeywordTrivia, endIfKeywordFound, leadingTriviaBuilder, trailingTriviaBuilder)
                        ProcessTrivia(ifKeywordTrivia, endIfKeywordFound, leadingTriviaBuilder, trailingTriviaBuilder)

                        If endIfKeywordFound Then

                            ' Rewrites:
                            ' (a) Replace the missing curEndKeyword and curIfKeyword tokens with new "End" and "If" keywords tokens respectively.
                            ' (b) Remove the first "EndIf" keyword token from the descendant trivia and adjust the leading and trailing trivia appropriately.

                            Dim newEndKeyword = SyntaxFactory.Token(SyntaxKind.EndKeyword).WithTrailingTrivia(SyntaxFactory.WhitespaceTrivia(" "))
                            If leadingTriviaBuilder IsNot Nothing Then
                                newEndKeyword = newEndKeyword.WithLeadingTrivia(leadingTriviaBuilder)
                            End If

                            Dim newIfKeyword = SyntaxFactory.Token(SyntaxKind.IfKeyword)
                            If trailingTriviaBuilder IsNot Nothing Then
                                newIfKeyword = newIfKeyword.WithTrailingTrivia(trailingTriviaBuilder)
                            End If

                            Return curNode.ReplaceTokens(SpecializedCollections.SingletonEnumerable(curEndKeyword).Concat(curIfKeyword),
                                                      Function(o, m)
                                                          If o = curEndKeyword Then
                                                              Return newEndKeyword
                                                          ElseIf o = curIfKeyword Then
                                                              Return newIfKeyword
                                                          Else
                                                              Return o
                                                          End If
                                                      End Function)
                        End If
                    End If
                End If

                Return curNode
            End Function

            ' Process trivia looking for an "EndIf" keyword token.
            Private Shared Sub ProcessTrivia(triviaList As IEnumerable(Of SyntaxTrivia),
                                             ByRef endIfKeywordFound As Boolean,
                                             ByRef leadingTriviaBuilder As Queue(Of SyntaxTrivia),
                                             ByRef trailingTriviaBuilder As Queue(Of SyntaxTrivia))
                For Each trivia In triviaList
                    If Not endIfKeywordFound Then
                        If trivia.HasStructure Then
                            Dim structuredTrivia = DirectCast(trivia.GetStructure(), StructuredTriviaSyntax)
                            If structuredTrivia.Kind = SyntaxKind.SkippedTokensTrivia Then
                                Dim skippedTokens = DirectCast(structuredTrivia, SkippedTokensTriviaSyntax).Tokens
                                If skippedTokens.Count = 1 AndAlso skippedTokens.First.Kind = SyntaxKind.EndIfKeyword Then
                                    endIfKeywordFound = True
                                    Continue For
                                End If
                            End If
                        End If

                        ' Append the trivia to leading trivia and continue processing remaining trivia for EndIf keyword.
                        If leadingTriviaBuilder Is Nothing Then
                            leadingTriviaBuilder = New Queue(Of SyntaxTrivia)
                        End If

                        leadingTriviaBuilder.Enqueue(trivia)
                    Else
                        ' EndIf keyword was already found in a prior trivia, so just append this trivia to trailing trivia.
                        If trailingTriviaBuilder Is Nothing Then
                            trailingTriviaBuilder = New Queue(Of SyntaxTrivia)

                            ' This is the first trivia encountered after the EndIf keyword.
                            ' If this trivia is neither a WhitespaceTrivia nor an EndOfLineTrivia, then we must insert an extra WhitespaceTrivia here.
                            Select Case trivia.Kind
                                Case SyntaxKind.WhitespaceTrivia, SyntaxKind.EndOfLineTrivia
                                    Exit Select
                                Case Else
                                    trailingTriviaBuilder.Enqueue(SyntaxFactory.WhitespaceTrivia(" "))
                            End Select
                        End If

                        trailingTriviaBuilder.Enqueue(trivia)
                    End If
                Next
            End Sub
#End Region

        End Class
    End Class
End Namespace
