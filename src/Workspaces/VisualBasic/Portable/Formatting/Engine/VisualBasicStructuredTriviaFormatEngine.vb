﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Formatting

#If CODE_STYLE Then
Imports OptionSet = Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions
#Else
Imports Microsoft.CodeAnalysis.Options
#End If

Namespace Microsoft.CodeAnalysis.VisualBasic.Formatting
    Partial Friend Class VisualBasicStructuredTriviaFormatEngine
        Inherits AbstractFormatEngine

        Public Shared Function FormatTrivia(trivia As SyntaxTrivia,
                                      initialColumn As Integer,
                                      optionSet As OptionSet,
                                      formattingRules As ChainedFormattingRules,
                                      cancellationToken As CancellationToken) As IFormattingResult
            Dim root = trivia.GetStructure()
            Dim formatter = New VisualBasicStructuredTriviaFormatEngine(trivia, initialColumn, optionSet, formattingRules, root.GetFirstToken(includeZeroWidth:=True), root.GetLastToken(includeZeroWidth:=True))
            Return formatter.Format(cancellationToken)
        End Function

        Private Sub New(trivia As SyntaxTrivia,
                       initialColumn As Integer,
                       optionSet As OptionSet,
                       formattingRules As ChainedFormattingRules,
                       token1 As SyntaxToken,
                       token2 As SyntaxToken)
            MyBase.New(TreeData.Create(trivia, initialColumn),
                       optionSet, formattingRules, token1, token2)
        End Sub

        Protected Overrides Function CreateTriviaFactory() As AbstractTriviaDataFactory
            Return New TriviaDataFactory(Me.TreeData, Me.OptionSet)
        End Function

        Protected Overrides Function CreateFormattingContext(tokenStream As TokenStream, cancellationToken As CancellationToken) As FormattingContext
            Return New FormattingContext(Me, tokenStream, LanguageNames.VisualBasic)
        End Function

        Protected Overrides Function CreateNodeOperations(cancellationToken As CancellationToken) As NodeOperations
            ' ignore all node operations for structured trivia since it is not possible for this to have any impact currently.
            Return NodeOperations.Empty
        End Function

        Protected Overrides Function CreateFormattingResult(tokenStream As TokenStream) As AbstractFormattingResult
            Return New FormattingResult(Me.TreeData, tokenStream, Me.SpanToFormat)
        End Function
    End Class
End Namespace
