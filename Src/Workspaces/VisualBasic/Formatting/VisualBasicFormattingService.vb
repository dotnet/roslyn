Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Composition
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Shared.Collections
Imports Microsoft.CodeAnalysis.Text

#If MEF Then
Imports System.ComponentModel.Composition
#End If

Namespace Microsoft.CodeAnalysis.VisualBasic.Formatting
#If MEF Then
    <ExportLanguageService(GetType(IFormattingService), LanguageNames.VisualBasic)>
    Friend Class VisualBasicFormattingService
#Else
    Friend Class VisualBasicFormattingService
#End If
        Inherits AbstractFormattingService

        Private ReadOnly lazyExportedRules As Lazy(Of IEnumerable(Of IFormattingRule))

        Friend Sub New()
        End Sub

#If MEF Then
        <ImportingConstructor>
        Sub New(<ImportMany> rules As IEnumerable(Of Lazy(Of IFormattingRule, OrderableLanguageMetadata)))
#Else
        Sub New(rules As IEnumerable(Of Lazy(Of IFormattingRule, OrderableLanguageMetadata)))
#End If
            Me.lazyExportedRules = New Lazy(Of IEnumerable(Of IFormattingRule))(
                Function()
                    Return ExtensionOrderer.Order(rules).Where(Function(x) x.Metadata.Language = LanguageNames.VisualBasic).Select(Function(x) x.Value).Concat(New DefaultOperationProvider()).ToImmutableList()
                End Function)
        End Sub

        Sub New(exports As ExportSource)
            Me.New(exports.GetExports(Of IFormattingRule, OrderableLanguageMetadata))
        End Sub

        Public Overrides Function GetDefaultFormattingRules() As IEnumerable(Of IFormattingRule)
            Return lazyExportedRules.Value
        End Function

        Protected Overrides Function CreateAggregatedFormattingResult(node As SyntaxNode, results As IList(Of AbstractFormattingResult), Optional formattingSpans As SimpleIntervalTree(Of TextSpan) = Nothing) As IFormattingResult
            Return New AggregatedFormattingResult(node, results, formattingSpans)
        End Function

        Protected Overrides Function Format(root As SyntaxNode, optionSet As OptionSet, formattingRules As IEnumerable(Of IFormattingRule), token1 As SyntaxToken, token2 As SyntaxToken, cancellationToken As CancellationToken) As AbstractFormattingResult
            Return New VisualBasicFormatEngine(root, optionSet, formattingRules, token1, token2).Format(cancellationToken)
        End Function
    End Class
End Namespace