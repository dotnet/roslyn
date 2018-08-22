Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeFixes.Suppression
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Options

Namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.Suppression
    <ExportSuppressionFixProvider(PredefinedCodeFixProviderNames.ConfigureSeverity, LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicConfigureSeverityLevel
        Inherits ConfigureSeverityLevelCodeFixProvider

        Public Sub New()
            MyBase.New(diagnosticToOptionVB, LanguageNames.VisualBasic)
        End Sub

        ' TO-DO: Reduce degree of tight-coupling and hard-coded options
        Private Shared ReadOnly diagnosticToOptionVB As Dictionary(Of String, [Option](Of CodeStyleOption(Of Boolean))) = New Dictionary(Of String, [Option](Of CodeStyleOption(Of Boolean)))() From
        {
        }
    End Class
End Namespace
