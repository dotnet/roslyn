' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports My.Resources

Namespace BasicAnalyzers
    ''' <summary>
    ''' Analyzer for reporting syntax node diagnostics.
    ''' It reports diagnostics for implicitly typed local variables, recommending explicit type specification.
    ''' </summary>
    ''' <remarks>
    ''' For analyzers that requires analyzing symbols or syntax nodes across compilation, see <see cref="CompilationStartedAnalyzer"/> and <see cref="CompilationStartedAnalyzerWithCompilationWideAnalysis"/>.
    ''' For analyzers that requires analyzing symbols or syntax nodes across a code block, see <see cref="CodeBlockStartedAnalyzer"/>.
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class SyntaxNodeAnalyzer
        Inherits DiagnosticAnalyzer

#Region "Descriptor fields"
        Friend Shared ReadOnly Title As LocalizableString = New LocalizableResourceString(NameOf(Resources.SyntaxNodeAnalyzerTitle), Resources.ResourceManager, GetType(Resources))
        Friend Shared ReadOnly MessageFormat As LocalizableString = New LocalizableResourceString(NameOf(Resources.SyntaxNodeAnalyzerMessageFormat), Resources.ResourceManager, GetType(Resources))
        Friend Shared ReadOnly Description As LocalizableString = New LocalizableResourceString(NameOf(Resources.SyntaxNodeAnalyzerDescription), Resources.ResourceManager, GetType(Resources))

        Friend Shared Rule As New DiagnosticDescriptor(SyntaxNodeAnalyzerRuleId, Title, MessageFormat, Stateless, DiagnosticSeverity.Warning, isEnabledByDefault:=True, description:=Description)
#End Region

        Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(Rule)
            End Get
        End Property

        Public Overrides Sub Initialize(context As AnalysisContext)
            context.RegisterSyntaxNodeAction(AddressOf AnalyzeSyntaxNode, SyntaxKind.VariableDeclarator)
        End Sub

        Private Shared Sub AnalyzeSyntaxNode(context As SyntaxNodeAnalysisContext)
            ' Find implicitly typed variable declarations.
            Dim declaration = DirectCast(context.Node, VariableDeclaratorSyntax)
            If declaration.AsClause Is Nothing Then
                For Each variable In declaration.Names
                    ' For all such locals, report a diagnostic.
                    Dim diag = Diagnostic.Create(Rule, variable.GetLocation(), variable.Identifier.ValueText)
                    context.ReportDiagnostic(diag)
                Next
            End If
        End Sub
    End Class
End Namespace
