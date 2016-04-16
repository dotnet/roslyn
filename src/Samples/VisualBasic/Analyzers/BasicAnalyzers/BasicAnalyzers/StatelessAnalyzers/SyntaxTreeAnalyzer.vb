' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports My.Resources

Namespace BasicAnalyzers
    ''' <summary>
    ''' Analyzer for reporting syntax tree diagnostics.
    ''' It reports diagnostics for all source files which have documentation comment diagnostics turned off.
    ''' </summary>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class SyntaxTreeAnalyzer
        Inherits DiagnosticAnalyzer

#Region "Descriptor fields"
        Friend Shared ReadOnly Title As LocalizableString = New LocalizableResourceString(NameOf(Resources.SyntaxTreeAnalyzerTitle), Resources.ResourceManager, GetType(Resources))
        Friend Shared ReadOnly MessageFormat As LocalizableString = New LocalizableResourceString(NameOf(Resources.SyntaxTreeAnalyzerMessageFormat), Resources.ResourceManager, GetType(Resources))
        Friend Shared ReadOnly Description As LocalizableString = New LocalizableResourceString(NameOf(Resources.SyntaxTreeAnalyzerDescription), Resources.ResourceManager, GetType(Resources))

        Friend Shared Rule As New DiagnosticDescriptor(DiagnosticIds.SyntaxTreeAnalyzerRuleId, Title, MessageFormat, DiagnosticCategories.Stateless, DiagnosticSeverity.Warning, isEnabledByDefault:=True, description:=Description)
#End Region

        Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(Rule)
            End Get
        End Property

        Public Overrides Sub Initialize(context As AnalysisContext)
            context.RegisterSyntaxTreeAction(AddressOf AnalyzeSyntaxTree)
        End Sub

        Private Shared Sub AnalyzeSyntaxTree(context As SyntaxTreeAnalysisContext)
            ' Find source files with documentation comment diagnostics turned off.
            If context.Tree.Options.DocumentationMode <> DocumentationMode.Diagnose Then
                ' For all such files, produce a diagnostic.
                Dim diag = Diagnostic.Create(Rule, Location.None, Path.GetFileName(context.Tree.FilePath))
                context.ReportDiagnostic(diag)
            End If
        End Sub
    End Class
End Namespace
