' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Formatting
    Partial Friend Class VisualBasicFormatEngine
        Inherits AbstractFormatEngine

        Public Sub New(node As SyntaxNode,
                       optionSet As AnalyzerConfigOptions,
                       formattingRules As IEnumerable(Of IFormattingRule),
                       token1 As SyntaxToken,
                       token2 As SyntaxToken)
            MyBase.New(TreeData.Create(node),
                       optionSet,
                       formattingRules,
                       token1,
                       token2,
                       TaskExecutor.Concurrent)
        End Sub

        Protected Overrides Function CreateTriviaFactory() As AbstractTriviaDataFactory
            Return New TriviaDataFactory(Me.TreeData, Me.Options)
        End Function

        Protected Overrides Function CreateFormattingResult(tokenStream As TokenStream) As AbstractFormattingResult
            Contract.ThrowIfNull(tokenStream)

            Return New FormattingResult(Me.TreeData, tokenStream, Me.SpanToFormat, Me.TaskExecutor)
        End Function
    End Class
End Namespace
