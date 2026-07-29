' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.IO
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics

Namespace BasicAnalyzers
    ''' <summary>
    ''' Analyzer for reporting syntax tree diagnostics, that require some semantic analysis.
    ''' It reports diagnostics for all source files which have at least one declaration diagnostic.
    ''' </summary>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class SemanticModelAnalyzer
        Inherits DiagnosticAnalyzer

#Region "Descriptor fields"
        Friend Shared ReadOnly Title As LocalizableString = "Source file declaration diagnostics count"
        Friend Shared ReadOnly MessageFormat As LocalizableString = "Source file '{0}' has '{1}' declaration diagnostic(s)."
        Friend Shared ReadOnly Description As LocalizableString = "Source file declaration diagnostic count."

        Friend Shared Rule As New DiagnosticDescriptor(DiagnosticIds.SemanticModelAnalyzerRuleId, Title, MessageFormat, DiagnosticCategories.Stateless, DiagnosticSeverity.Warning, isEnabledByDefault:=True, description:=Description)
#End Region

        Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(Rule)
            End Get
        End Property

        Public Overrides Sub Initialize(context As AnalysisContext)
            context.RegisterSemanticModelAction(AddressOf AnalyzeSemanticModel)
        End Sub

        Private Shared Sub AnalyzeSemanticModel(context As SemanticModelAnalysisContext)
            ' Find just those source files with declaration diagnostics.
            Dim diagnosticsCount = context.SemanticModel.GetDeclarationDiagnostics().Length
            If diagnosticsCount > 0 Then
                ' For all such files, produce a diagnostic.
                Dim diag = Diagnostic.Create(Rule, Location.None, Path.GetFileName(context.SemanticModel.SyntaxTree.FilePath), diagnosticsCount)
                context.ReportDiagnostic(diag)
            End If
        End Sub
    End Class
End Namespace
