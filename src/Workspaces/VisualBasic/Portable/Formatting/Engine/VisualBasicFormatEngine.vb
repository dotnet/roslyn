' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService

Namespace Microsoft.CodeAnalysis.VisualBasic.Formatting
    Partial Friend Class VisualBasicFormatEngine
        Inherits AbstractFormatEngine

        Public Sub New(node As SyntaxNode,
                       options As SyntaxFormattingOptions,
                       formattingRules As IEnumerable(Of AbstractFormattingRule),
                       startToken As SyntaxToken,
                       endToken As SyntaxToken)
            MyBase.New(TreeData.Create(node),
                       options,
                       formattingRules,
                       startToken,
                       endToken)
        End Sub

        Friend Overrides ReadOnly Property HeaderFacts As IHeaderFacts = VisualBasicHeaderFacts.Instance

        Protected Overrides Function CreateTriviaFactory() As AbstractTriviaDataFactory
            Return New TriviaDataFactory(Me.TreeData, Me.Options)
        End Function

        Protected Overrides Function CreateFormattingResult(tokenStream As TokenStream) As AbstractFormattingResult
            Contract.ThrowIfNull(tokenStream)

            Return New FormattingResult(Me.TreeData, tokenStream, Me.SpanToFormat)
        End Function
    End Class
End Namespace
