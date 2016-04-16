' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports My.Resources

Namespace BasicAnalyzers
    ''' <summary>
    ''' Analyzer for reporting code block diagnostics.
    ''' It reports diagnostics for all redundant methods which have an empty method body and are not virtual/override.
    ''' </summary>
    ''' <remarks>
    ''' For analyzers that requires analyzing symbols or syntax nodes across a code block, see <see cref="CodeBlockStartedAnalyzer"/>.
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class CodeBlockAnalyzer
        Inherits DiagnosticAnalyzer

#Region "Descriptor fields"
        Friend Shared ReadOnly Title As LocalizableString = New LocalizableResourceString(NameOf(Resources.CodeBlockAnalyzerTitle), Resources.ResourceManager, GetType(Resources))
        Friend Shared ReadOnly MessageFormat As LocalizableString = New LocalizableResourceString(NameOf(Resources.CodeBlockAnalyzerMessageFormat), Resources.ResourceManager, GetType(Resources))
        Friend Shared ReadOnly Description As LocalizableString = New LocalizableResourceString(NameOf(Resources.CodeBlockAnalyzerDescription), Resources.ResourceManager, GetType(Resources))

        Friend Shared Rule As New DiagnosticDescriptor(DiagnosticIds.CodeBlockAnalyzerRuleId, Title, MessageFormat, DiagnosticCategories.Stateless, DiagnosticSeverity.Warning, isEnabledByDefault:=True, description:=Description)
#End Region

        Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(Rule)
            End Get
        End Property

        Public Overrides Sub Initialize(context As AnalysisContext)
            context.RegisterCodeBlockAction(AddressOf CodeBlockAction)
        End Sub

        Private Shared Sub CodeBlockAction(codeBlockContext As CodeBlockAnalysisContext)
            ' We only care about method bodies.
            If codeBlockContext.OwningSymbol.Kind <> SymbolKind.Method Then
                Return
            End If

            ' Report diagnostic for void non-virtual methods with empty method bodies.
            Dim method = DirectCast(codeBlockContext.OwningSymbol, IMethodSymbol)
            Dim block = TryCast(codeBlockContext.CodeBlock, MethodBlockBaseSyntax)
            If method.ReturnsVoid AndAlso Not method.IsVirtual AndAlso block?.Statements.Count = 0 Then
                Dim tree = block.SyntaxTree
                Dim location = method.Locations.First(Function(l) tree.Equals(l.SourceTree))
                Dim diag = Diagnostic.Create(Rule, location, method.Name)
                codeBlockContext.ReportDiagnostic(diag)
            End If
        End Sub
    End Class
End Namespace