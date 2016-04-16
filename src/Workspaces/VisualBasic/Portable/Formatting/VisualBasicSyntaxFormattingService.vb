' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Shared.Collections
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic.Formatting
    <ExportLanguageService(GetType(ISyntaxFormattingService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicSyntaxFormattingService
        Inherits AbstractSyntaxFormattingService

        Private ReadOnly _lazyExportedRules As Lazy(Of IEnumerable(Of IFormattingRule))

        <ImportingConstructor>
        Public Sub New(<ImportMany> rules As IEnumerable(Of Lazy(Of IFormattingRule, OrderableLanguageMetadata)))
            Me._lazyExportedRules = New Lazy(Of IEnumerable(Of IFormattingRule))(
                Function()
                    Return ExtensionOrderer.Order(rules).Where(Function(x) x.Metadata.Language = LanguageNames.VisualBasic).Select(Function(x) x.Value).Concat(New DefaultOperationProvider()).ToImmutableArray()
                End Function)
        End Sub

        Public Overrides Function GetDefaultFormattingRules() As IEnumerable(Of IFormattingRule)
            Return _lazyExportedRules.Value
        End Function

        Protected Overrides Function CreateAggregatedFormattingResult(node As SyntaxNode, results As IList(Of AbstractFormattingResult), Optional formattingSpans As SimpleIntervalTree(Of TextSpan) = Nothing) As IFormattingResult
            Return New AggregatedFormattingResult(node, results, formattingSpans)
        End Function

        Protected Overrides Function FormatAsync(root As SyntaxNode, optionSet As OptionSet, formattingRules As IEnumerable(Of IFormattingRule), token1 As SyntaxToken, token2 As SyntaxToken, cancellationToken As CancellationToken) As Task(Of AbstractFormattingResult)
            Return New VisualBasicFormatEngine(root, optionSet, formattingRules, token1, token2).FormatAsync(cancellationToken)
        End Function
    End Class
End Namespace
