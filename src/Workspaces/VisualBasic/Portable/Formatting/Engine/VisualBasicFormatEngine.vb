' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.Options

Namespace Microsoft.CodeAnalysis.VisualBasic.Formatting
    Partial Friend Class VisualBasicFormatEngine
        Inherits AbstractFormatEngine

        Public Sub New(node As SyntaxNode,
                       optionSet As OptionSet,
                       formattingRules As IEnumerable(Of AbstractFormattingRule),
                       token1 As SyntaxToken,
                       token2 As SyntaxToken)
            MyBase.New(TreeData.Create(node),
                       optionSet,
                       formattingRules,
                       token1,
                       token2)
        End Sub

        Protected Overrides Function CreateTriviaFactory() As AbstractTriviaDataFactory
            Return New TriviaDataFactory(Me.TreeData, Me.OptionSet)
        End Function

        Protected Overrides Function CreateFormattingResult(tokenStream As TokenStream) As AbstractFormattingResult
            Contract.ThrowIfNull(tokenStream)

            Return New FormattingResult(Me.TreeData, tokenStream, Me.SpanToFormat)
        End Function
    End Class
End Namespace
