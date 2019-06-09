' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.Shared.Collections
Imports Microsoft.CodeAnalysis.Text

#If Not CODE_STYLE Then
Imports System.Composition
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Options
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

        Protected Overrides Function CreateAggregatedFormattingResult(node As SyntaxNode, results As IList(Of AbstractFormattingResult), Optional formattingSpans As SimpleIntervalTree(Of TextSpan) = Nothing) As IFormattingResult
            Return New AggregatedFormattingResult(node, results, formattingSpans)
        End Function

        Protected Overrides Function Format(root As SyntaxNode, optionSet As OptionSet, formattingRules As IEnumerable(Of AbstractFormattingRule), token1 As SyntaxToken, token2 As SyntaxToken, cancellationToken As CancellationToken) As AbstractFormattingResult
            Return New VisualBasicFormatEngine(root, optionSet, formattingRules, token1, token2).Format(cancellationToken)
        End Function
    End Class
End Namespace
