' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic.Formatting
    Partial Friend Class TriviaDataFactory
        Private Class FormattedComplexTrivia
            Inherits TriviaDataWithList

            Private ReadOnly _formatter As VisualBasicTriviaFormatter
            Private ReadOnly _textChanges As IList(Of TextChange)

            Public Sub New(context As FormattingContext,
                           formattingRules As ChainedFormattingRules,
                           token1 As SyntaxToken,
                           token2 As SyntaxToken,
                           lineBreaks As Integer,
                           spaces As Integer,
                           originalString As String,
                           cancellationToken As CancellationToken)
                MyBase.New(context.OptionSet, LanguageNames.VisualBasic)

                Contract.ThrowIfNull(context)
                Contract.ThrowIfNull(formattingRules)
                Contract.ThrowIfNull(originalString)

                Me.LineBreaks = Math.Max(0, lineBreaks)
                Me.Spaces = Math.Max(0, spaces)

                Dim lines = Me.LineBreaks

                _formatter = New VisualBasicTriviaFormatter(context, formattingRules, token1, token2, originalString, Math.Max(0, lines), Me.Spaces)
                _textChanges = _formatter.FormatToTextChanges(cancellationToken)
            End Sub

            Public Overrides ReadOnly Property TreatAsElastic() As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property IsWhitespaceOnlyTrivia() As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property ContainsChanges() As Boolean
                Get
                    Return Me._textChanges.Count > 0
                End Get
            End Property

            Public Overrides Function GetTextChanges(span As TextSpan) As IEnumerable(Of TextChange)
                Return Me._textChanges
            End Function

            Public Overrides Function GetTriviaList(cancellationToken As CancellationToken) As List(Of SyntaxTrivia)
                Return _formatter.FormatToSyntaxTrivia(cancellationToken)
            End Function

            Public Overrides Sub Format(context As FormattingContext,
                                        formattingRules As ChainedFormattingRules,
                                        formattingResultApplier As Action(Of Integer, TriviaData),
                                        cancellationToken As CancellationToken,
                                        Optional tokenPairIndex As Integer = TokenPairIndexNotNeeded)
                Throw New NotImplementedException()
            End Sub

            Public Overrides Function WithIndentation(indentation As Integer, context As FormattingContext, formattingRules As ChainedFormattingRules, cancellationToken As CancellationToken) As TriviaData
                Throw New NotImplementedException()
            End Function

            Public Overrides Function WithLine(line As Integer, indentation As Integer, context As FormattingContext, formattingRules As ChainedFormattingRules, cancellationToken As CancellationToken) As TriviaData
                Throw New NotImplementedException()
            End Function

            Public Overrides Function WithSpace(space As Integer, context As FormattingContext, formattingRules As ChainedFormattingRules) As TriviaData
                Throw New NotImplementedException()
            End Function
        End Class
    End Class
End Namespace
