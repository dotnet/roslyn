' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports My.Resources

Namespace BasicAnalyzers
    ''' <summary>
    ''' Analyzer for reporting compilation diagnostics.
    ''' It reports diagnostics for analyzer diagnostics that have been suppressed for the entire compilation.
    ''' </summary>
    ''' <remarks>
    ''' For analyzers that requires analyzing symbols or syntax nodes across compilation, see <see cref="CompilationStartedAnalyzer"/> and <see cref="CompilationStartedAnalyzerWithCompilationWideAnalysis"/>.
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class CompilationAnalyzer
        Inherits DiagnosticAnalyzer

#Region "Descriptor fields"
        Friend Shared ReadOnly Title As LocalizableString = New LocalizableResourceString(NameOf(Resources.CompilationAnalyzerTitle), Resources.ResourceManager, GetType(Resources))
        Friend Shared ReadOnly MessageFormat As LocalizableString = New LocalizableResourceString(NameOf(Resources.CompilationAnalyzerMessageFormat), Resources.ResourceManager, GetType(Resources))
        Friend Shared ReadOnly Description As LocalizableString = New LocalizableResourceString(NameOf(Resources.CompilationAnalyzerDescription), Resources.ResourceManager, GetType(Resources))

        Friend Shared Rule As New DiagnosticDescriptor(DiagnosticIds.CompilationAnalyzerRuleId, Title, MessageFormat, DiagnosticCategories.Stateless, DiagnosticSeverity.Warning, isEnabledByDefault:=True, description:=Description)
#End Region

        Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(Rule)
            End Get
        End Property

        Public Overrides Sub Initialize(context As AnalysisContext)
            context.RegisterCompilationAction(AddressOf AnalyzeCompilation)
        End Sub

        Private Shared Sub AnalyzeCompilation(context As CompilationAnalysisContext)
            ' Get all the suppressed analyzer diagnostic IDs.
            Dim suppressedAnalyzerDiagnosticIds = GetSuppressedAnalyzerDiagnosticIds(context.Compilation.Options.SpecificDiagnosticOptions)

            For Each suppressedDiagnosticId In suppressedAnalyzerDiagnosticIds
                ' For all such suppressed diagnostic IDs, produce a diagnostic.
                Dim diag = Diagnostic.Create(Rule, Location.None, suppressedDiagnosticId)
                context.ReportDiagnostic(diag)
            Next
        End Sub

        Private Shared Iterator Function GetSuppressedAnalyzerDiagnosticIds(specificOptions As ImmutableDictionary(Of String, ReportDiagnostic)) As IEnumerable(Of String)
            For Each kvp In specificOptions
                If kvp.Value = ReportDiagnostic.Suppress Then
                    Dim intId As Integer
                    If kvp.Key.StartsWith("CS", StringComparison.OrdinalIgnoreCase) AndAlso Integer.TryParse(kvp.Key.Substring(2), intId) Then
                        Continue For
                    End If

                    If kvp.Key.StartsWith("BC", StringComparison.OrdinalIgnoreCase) AndAlso Integer.TryParse(kvp.Key.Substring(2), intId) Then
                        Continue For
                    End If

                    Yield kvp.Key
                End If
            Next
        End Function
    End Class
End Namespace