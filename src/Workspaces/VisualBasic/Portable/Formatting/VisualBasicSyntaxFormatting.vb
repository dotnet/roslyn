' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Shared.Collections
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic.Formatting
    Friend Class VisualBasicSyntaxFormatting
        Inherits AbstractSyntaxFormatting

        Public Shared ReadOnly Instance As New VisualBasicSyntaxFormatting

        Private ReadOnly _rules As ImmutableArray(Of AbstractFormattingRule) = ImmutableArray.Create(Of AbstractFormattingRule)(
            New StructuredTriviaFormattingRule(),
            New ElasticTriviaFormattingRule(),
            New AdjustSpaceFormattingRule(),
            New AlignTokensFormattingRule(),
            New NodeBasedFormattingRule(),
            DefaultOperationProvider.Instance)

        Public Overrides Function GetDefaultFormattingRules() As ImmutableArray(Of AbstractFormattingRule)
            Return _rules
        End Function

        Public Overrides ReadOnly Property DefaultOptions As SyntaxFormattingOptions
            Get
                Return VisualBasicSyntaxFormattingOptions.Default
            End Get
        End Property

        Public Overrides Function GetFormattingOptions(options As IOptionsReader) As SyntaxFormattingOptions
            Return New VisualBasicSyntaxFormattingOptions(options, fallbackOptions:=Nothing)
        End Function

        Protected Overrides Function CreateAggregatedFormattingResult(node As SyntaxNode, results As IList(Of AbstractFormattingResult), Optional formattingSpans As TextSpanMutableIntervalTree = Nothing) As IFormattingResult
            Return New AggregatedFormattingResult(node, results, formattingSpans)
        End Function

        Protected Overrides Function Format(root As SyntaxNode, options As SyntaxFormattingOptions, formattingRules As ImmutableArray(Of AbstractFormattingRule), startToken As SyntaxToken, endToken As SyntaxToken, cancellationToken As CancellationToken) As AbstractFormattingResult
            Return New VisualBasicFormatEngine(root, options, formattingRules, startToken, endToken).Format(cancellationToken)
        End Function
    End Class
End Namespace
