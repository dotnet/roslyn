Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

<DiagnosticAnalyzer(LanguageNames.VisualBasic)>
Public Class $safeitemname$
    Inherits DiagnosticAnalyzer

    Public Const DiagnosticId = "$safeitemname$"
    Friend Shared ReadOnly Title As LocalizableString = "$safeitemname$ Title"
    Friend Shared ReadOnly MessageFormat As LocalizableString = "$safeitemname$ '{0}'"
    Friend Const Category = "$safeitemname$ Category"

    Friend Shared Rule As New DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, True)

    Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
        Get
            Return ImmutableArray.Create(Rule)
        End Get
    End Property

    Public Overrides Sub Initialize(context As AnalysisContext)
    End Sub
End Class