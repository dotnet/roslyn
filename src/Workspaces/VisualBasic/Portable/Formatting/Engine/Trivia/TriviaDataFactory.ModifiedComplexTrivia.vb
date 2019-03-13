' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Formatting
    Partial Friend Class TriviaDataFactory
        Private Class ModifiedComplexTrivia
            Inherits TriviaDataWithList

            Private ReadOnly _original As ComplexTrivia

            Public Sub New(optionSet As OptionSet, original As ComplexTrivia, lineBreaks As Integer, space As Integer)
                MyBase.New(optionSet, LanguageNames.VisualBasic)
                Contract.ThrowIfNull(original)

                Me._original = original

                ' linebreak and space can become negative during formatting. but it should be normalized to >= 0
                ' at the end.
                Me.LineBreaks = lineBreaks
                Me.Spaces = space
            End Sub

            Public Overrides ReadOnly Property ContainsChanges() As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property TreatAsElastic() As Boolean
                Get
                    Return Me._original.TreatAsElastic
                End Get
            End Property

            Public Overrides ReadOnly Property IsWhitespaceOnlyTrivia() As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides Function WithSpace(space As Integer, context As FormattingContext, formattingRules As ChainedFormattingRules) As TriviaData
                Return Me._original.WithSpace(space, context, formattingRules)
            End Function

            Public Overrides Function WithLine(line As Integer,
                                               indentation As Integer,
                                               context As FormattingContext,
                                               formattingRules As ChainedFormattingRules,
                                               cancellationToken As CancellationToken) As TriviaData
                Return Me._original.WithLine(line, indentation, context, formattingRules, cancellationToken)
            End Function

            Public Overrides Function WithIndentation(indentation As Integer,
                                                      context As FormattingContext,
                                                      formattingRules As ChainedFormattingRules,
                                                      cancellationToken As CancellationToken) As TriviaData
                Return Me._original.WithIndentation(indentation, context, formattingRules, cancellationToken)
            End Function

            Public Overrides Sub Format(context As FormattingContext,
                                        formattingRules As ChainedFormattingRules,
                                        formattingResultApplier As Action(Of Integer, TokenStream, TriviaData),
                                        cancellationToken As CancellationToken,
                                        Optional tokenPairIndex As Integer = TokenPairIndexNotNeeded)
                Contract.ThrowIfFalse(Me.SecondTokenIsFirstTokenOnLine)

                Dim commonToken1 As SyntaxToken = Me._original.Token1
                Dim commonToken2 As SyntaxToken = Me._original.Token2

                Dim list = New TriviaList(commonToken1.TrailingTrivia, commonToken2.LeadingTrivia)
                Contract.ThrowIfFalse(list.Count > 0)

                ' okay, now, check whether we need or are able to format noisy tokens
                If CodeShapeAnalyzer.ContainsSkippedTokensOrText(list) Then
                    Return
                End If

                formattingResultApplier(
                    tokenPairIndex,
                    context.TokenStream,
                    New FormattedComplexTrivia(context, formattingRules, Me._original.Token1, Me._original.Token2, Me.LineBreaks, Me.Spaces, Me._original.OriginalString, cancellationToken))
            End Sub

            Public Overrides Function GetTextChanges(span As TextSpan) As IEnumerable(Of TextChange)
                Throw New NotImplementedException()
            End Function

            Public Overrides Function GetTriviaList(cancellationToken As CancellationToken) As List(Of SyntaxTrivia)
                Throw New NotImplementedException()
            End Function
        End Class
    End Class
End Namespace
