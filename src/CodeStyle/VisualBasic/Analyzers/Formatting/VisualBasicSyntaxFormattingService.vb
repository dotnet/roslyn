' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.Shared.Collections
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic.Formatting
    Friend Class VisualBasicSyntaxFormattingService
        Inherits AbstractSyntaxFormattingService

        Private ReadOnly _lazyExportedRules As Lazy(Of IEnumerable(Of IFormattingRule))

        Public Sub New()
            _lazyExportedRules = New Lazy(Of IEnumerable(Of IFormattingRule))(
                Function()
                    Return New IFormattingRule() {
                        New StructuredTriviaFormattingRule(),
                        New ElasticTriviaFormattingRule(),
                        New AdjustSpaceFormattingRule(),
                        New AlignTokensFormattingRule(),
                        New NodeBasedFormattingRule()
                    }
                End Function)
        End Sub

        Public Overrides Function GetDefaultFormattingRules() As IEnumerable(Of IFormattingRule)
            Return _lazyExportedRules.Value
        End Function

        Protected Overrides Function CreateAggregatedFormattingResult(node As SyntaxNode, results As IList(Of AbstractFormattingResult), Optional formattingSpans As SimpleIntervalTree(Of TextSpan) = Nothing) As IFormattingResult
            Return New AggregatedFormattingResult(node, results, formattingSpans)
        End Function

        Protected Overrides Function Format(root As SyntaxNode, options As AnalyzerConfigOptions, formattingRules As IEnumerable(Of IFormattingRule), token1 As SyntaxToken, token2 As SyntaxToken, cancellationToken As CancellationToken) As AbstractFormattingResult
            Return New VisualBasicFormatEngine(root, options, formattingRules, token1, token2).Format(cancellationToken)
        End Function
    End Class
End Namespace
