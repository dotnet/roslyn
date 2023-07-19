' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic.Formatting
    Partial Friend Class TriviaDataFactory
        Private MustInherit Class AbstractLineBreakTrivia
            Inherits Whitespace

            Protected ReadOnly _original As String
            Protected ReadOnly _newString As String

            Public Sub New(options As SyntaxFormattingOptions,
                           original As String,
                           lineBreaks As Integer,
                           indentation As Integer,
                           elastic As Boolean)
                MyBase.New(options, lineBreaks, indentation, elastic, LanguageNames.VisualBasic)

                Me._original = original
                Me._newString = CreateStringFromState()
            End Sub

            Protected MustOverride Function CreateStringFromState() As String

            Public Overrides Function WithSpace(space As Integer, context As FormattingContext, formattingRules As ChainedFormattingRules) As TriviaData
                throw ExceptionUtilities.Unreachable
            End Function

            Public Overrides Function WithLine(line As Integer, indentation As Integer,
                                               context As FormattingContext,
                                               formattingRules As ChainedFormattingRules,
                                               cancellationToken As CancellationToken) As TriviaData
                throw ExceptionUtilities.Unreachable
            End Function

            Public Overrides ReadOnly Property ContainsChanges As Boolean
                Get
                    Return Not Me._original.Equals(Me._newString) OrElse Me.TreatAsElastic
                End Get
            End Property

            Public Overrides Function GetTextChanges(textSpan As TextSpan) As IEnumerable(Of TextChange)
                Return SpecializedCollections.SingletonEnumerable(New TextChange(textSpan, Me._newString))
            End Function

            Public Overrides Sub Format(context As FormattingContext,
                                        formattingRules As ChainedFormattingRules,
                                        formattingResultApplier As Action(Of Integer, TokenStream, TriviaData),
                                        cancellationToken As CancellationToken,
                                        Optional tokenPairIndex As Integer = TokenPairIndexNotNeeded)
                If Me.ContainsChanges Then
                    formattingResultApplier(tokenPairIndex, context.TokenStream, Me)
                End If
            End Sub
        End Class
    End Class
End Namespace
