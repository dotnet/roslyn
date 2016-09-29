' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports My.Resources

Namespace BasicAnalyzers
    ''' <summary>
    ''' Analyzer to demonstrate code block wide analysis.
    ''' It computes and reports diagnostics for unused parameters in methods.
    ''' It performs code block wide analysis to detect such unused parameters and reports diagnostics for them in the code block end action.
    ''' <para>
    ''' The analyzer registers:
    ''' (a) A code block start action, which initializes per-code block mutable state. We mark all parameters as unused at start of analysis.
    ''' (b) A code block syntax node action, which identifes parameter references and marks the corresponding parameter as used.
    ''' (c) A code block end action, which reports diagnostics based on the final state, for all parameters which are unused.
    ''' </para>
    ''' </summary>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class CodeBlockStartedAnalyzer
        Inherits DiagnosticAnalyzer

#Region "Descriptor fields"
        Friend Shared ReadOnly Title As LocalizableString = New LocalizableResourceString(NameOf(Resources.CodeBlockStartedAnalyzerTitle), Resources.ResourceManager, GetType(Resources))
        Friend Shared ReadOnly MessageFormat As LocalizableString = New LocalizableResourceString(NameOf(Resources.CodeBlockStartedAnalyzerMessageFormat), Resources.ResourceManager, GetType(Resources))
        Friend Shared ReadOnly Description As LocalizableString = New LocalizableResourceString(NameOf(Resources.CodeBlockStartedAnalyzerDescription), Resources.ResourceManager, GetType(Resources))

        Friend Shared Rule As New DiagnosticDescriptor(DiagnosticIds.CodeBlockStartedAnalyzerRuleId, Title, MessageFormat, DiagnosticCategories.Stateful, DiagnosticSeverity.Warning, isEnabledByDefault:=True, description:=Description)
#End Region

        Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(Rule)
            End Get
        End Property

        Public Overrides Sub Initialize(context As AnalysisContext)
            context.RegisterCodeBlockStartAction(Of SyntaxKind)(
                Sub(startCodeBlockContext)
                    ' We only care about method bodies.
                    If startCodeBlockContext.OwningSymbol.Kind <> SymbolKind.Method Then
                        Return
                    End If

                    ' We only care about methods with parameters.
                    Dim method = DirectCast(startCodeBlockContext.OwningSymbol, IMethodSymbol)
                    If method.Parameters.IsEmpty Then
                        Return
                    End If

                    ' Initialize local mutable state in the start action.
                    Dim analyzer = New UnusedParametersAnalyzer(method)

                    ' Register an intermediate non-end action that accesses and modifies the state.
                    startCodeBlockContext.RegisterSyntaxNodeAction(AddressOf analyzer.AnalyzeSyntaxNode, SyntaxKind.IdentifierName)

                    ' Register an end action to report diagnostics based on the final state.
                    startCodeBlockContext.RegisterCodeBlockEndAction(AddressOf analyzer.CodeBlockEndAction)
                End Sub)
        End Sub

        Private Class UnusedParametersAnalyzer

#Region "Per-CodeBlock mutable state"
            Private ReadOnly _unusedParameters As HashSet(Of IParameterSymbol)
            Private ReadOnly _unusedParameterNames As HashSet(Of String)
#End Region

#Region "State intialization"
            Public Sub New(method As IMethodSymbol)
                ' Initialization: Assume all parameters are unused.
                Dim parameters = method.Parameters.Where(Function(p) Not p.IsImplicitlyDeclared AndAlso p.Locations.Length > 0)
                _unusedParameters = New HashSet(Of IParameterSymbol)(parameters)
                _unusedParameterNames = New HashSet(Of String)(parameters.Select(Function(p) p.Name))
            End Sub
#End Region

#Region "Intermediate actions"
            Public Sub AnalyzeSyntaxNode(context As SyntaxNodeAnalysisContext)
                ' Check if we have any pending unreferenced parameters.
                If _unusedParameters.Count = 0 Then
                    Return
                End If

                ' Syntactic check to avoid invoking GetSymbolInfo for every identifier.
                Dim identifier = DirectCast(context.Node, IdentifierNameSyntax)
                If Not _unusedParameterNames.Contains(identifier.Identifier.ValueText) Then
                    Return
                End If

                ' Mark parameter as used.
                Dim parmeter = TryCast(context.SemanticModel.GetSymbolInfo(identifier, context.CancellationToken).Symbol, IParameterSymbol)
                If parmeter IsNot Nothing AndAlso _unusedParameters.Contains(parmeter) Then
                    _unusedParameters.Remove(parmeter)
                    _unusedParameterNames.Remove(parmeter.Name)
                End If
            End Sub
#End Region

#Region "End action"
            Public Sub CodeBlockEndAction(context As CodeBlockAnalysisContext)
                ' Report diagnostics for unused parameters.
                For Each parameter In _unusedParameters
                    Dim diag = Diagnostic.Create(Rule, parameter.Locations(0), parameter.Name, parameter.ContainingSymbol.Name)
                    context.ReportDiagnostic(diag)
                Next
            End Sub
#End Region

        End Class
    End Class
End Namespace
