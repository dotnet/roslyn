' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.Shared.Collections
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.Diagnostics

#If Not CODE_STYLE Then
Imports System.Composition
Imports Microsoft.CodeAnalysis.Host.Mef
#End If

Namespace Microsoft.CodeAnalysis.VisualBasic.Formatting
#If Not CODE_STYLE Then
    <ExportLanguageService(GetType(ISyntaxFormattingService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicSyntaxFormattingService
#Else
    Friend Class VisualBasicSyntaxFormattingService
#End If
        Inherits AbstractSyntaxFormattingService

        Private ReadOnly _rules As ImmutableList(Of AbstractFormattingRule)

#If CODE_STYLE Then
        Public Shared ReadOnly Instance As New VisualBasicSyntaxFormattingService
#End If

#If Not CODE_STYLE Then
        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
#Else
        Public Sub New()
#End If
            _rules = ImmutableList.Create(Of AbstractFormattingRule)(
                New StructuredTriviaFormattingRule(),
                New ElasticTriviaFormattingRule(),
                New AdjustSpaceFormattingRule(),
                New AlignTokensFormattingRule(),
                New NodeBasedFormattingRule(),
                DefaultOperationProvider.Instance)
        End Sub

        Public Overrides Function GetDefaultFormattingRules() As IEnumerable(Of AbstractFormattingRule)
            Return _rules
        End Function

        Public Overrides Function GetFormattingOptions(options As AnalyzerConfigOptions) As SyntaxFormattingOptions
            Return VisualBasicSyntaxFormattingOptions.Create(options)
        End Function

        Protected Overrides Function CreateAggregatedFormattingResult(node As SyntaxNode, results As IList(Of AbstractFormattingResult), Optional formattingSpans As SimpleIntervalTree(Of TextSpan, TextSpanIntervalIntrospector) = Nothing) As IFormattingResult
            Return New AggregatedFormattingResult(node, results, formattingSpans)
        End Function

        Protected Overrides Function Format(root As SyntaxNode, options As SyntaxFormattingOptions, formattingRules As IEnumerable(Of AbstractFormattingRule), startToken As SyntaxToken, endToken As SyntaxToken, cancellationToken As CancellationToken) As AbstractFormattingResult
            Return New VisualBasicFormatEngine(root, options, formattingRules, startToken, endToken).Format(cancellationToken)
        End Function
    End Class
End Namespace
