' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    Partial Friend Class VisualBasicStructuredTriviaFormatEngine
        Inherits AbstractFormatEngine

        Public Shared Function FormatTrivia(trivia As SyntaxTrivia,
                                      initialColumn As Integer,
                                      optionSet As OptionSet,
                                      formattingRules As ChainedFormattingRules,
                                      cancellationToken As CancellationToken) As IFormattingResult
            Dim root = trivia.GetStructure()
            Dim formatter = New VisualBasicStructuredTriviaFormatEngine(trivia, initialColumn, optionSet, formattingRules, root.GetFirstToken(includeZeroWidth:=True), root.GetLastToken(includeZeroWidth:=True))
            Return formatter.FormatAsync(cancellationToken).WaitAndGetResult(cancellationToken)
        End Function

        Private Sub New(trivia As SyntaxTrivia,
                       initialColumn As Integer,
                       optionSet As OptionSet,
                       formattingRules As ChainedFormattingRules,
                       token1 As SyntaxToken,
                       token2 As SyntaxToken)
            MyBase.New(TreeData.Create(trivia, initialColumn),
                       optionSet, formattingRules, token1, token2, TaskExecutor.Synchronous)
        End Sub

        Protected Overrides Function CreateTriviaFactory() As AbstractTriviaDataFactory
            Return New TriviaDataFactory(Me.TreeData, Me.OptionSet)
        End Function

        Protected Overrides Function CreateFormattingContext(tokenStream As TokenStream, cancellationToken As CancellationToken) As FormattingContext
            Return New FormattingContext(Me, tokenStream, LanguageNames.VisualBasic)
        End Function

        Protected Overrides Function CreateNodeOperationTasks(cancellationToken As CancellationToken) As NodeOperations
            ' ignore all node operations for structured trivia since it is not possible for this to have any impact currently.
            Return NodeOperations.Empty
        End Function

        Protected Overrides Function CreateFormattingResult(tokenStream As TokenStream) As AbstractFormattingResult
            Return New FormattingResult(Me.TreeData, tokenStream, Me.SpanToFormat, Me.TaskExecutor)
        End Function
    End Class
End Namespace
