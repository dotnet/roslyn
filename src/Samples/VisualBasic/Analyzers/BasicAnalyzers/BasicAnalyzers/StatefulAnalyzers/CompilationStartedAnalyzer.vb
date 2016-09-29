' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports My.Resources

Namespace BasicAnalyzers
    ''' <summary>
    ''' Analyzer to demonstrate analysis within a compilation defining certain well-known symbol(s).
    ''' It computes and reports diagnostics for all public implementations of an interface, which is only supposed to be implemented internally.
    ''' <para>
    ''' The analyzer registers:
    ''' (a) A compilation start action, which initializes per-compilation immutable state. We fetch and store the type symbol for the interface type in the compilation.
    ''' (b) A compilation symbol action, which identifies all named types implementing this interface, and reports diagnostics for all but internal allowed well known types.
    ''' </para>
    ''' </summary>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class CompilationStartedAnalyzer
        Inherits DiagnosticAnalyzer

#Region "Descriptor fields"
        Friend Shared ReadOnly Title As LocalizableString = New LocalizableResourceString(NameOf(Resources.CompilationStartedAnalyzerTitle), Resources.ResourceManager, GetType(Resources))
        Friend Shared ReadOnly MessageFormat As LocalizableString = New LocalizableResourceString(NameOf(Resources.CompilationStartedAnalyzerMessageFormat), Resources.ResourceManager, GetType(Resources))
        Friend Shared ReadOnly Description As LocalizableString = New LocalizableResourceString(NameOf(Resources.CompilationStartedAnalyzerDescription), Resources.ResourceManager, GetType(Resources))

        Friend Shared Rule As New DiagnosticDescriptor(DiagnosticIds.CompilationStartedAnalyzerRuleId, Title, MessageFormat, DiagnosticCategories.Stateful, DiagnosticSeverity.Warning, isEnabledByDefault:=True, description:=Description)
#End Region

        Public Const DontInheritInterfaceTypeName As String = "MyInterfaces.Interface"
        Public Const AllowedInternalImplementationTypeName As String = "MyInterfaces.MyInterfaceImpl"

        Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(Rule)
            End Get
        End Property

        Public Overrides Sub Initialize(context As AnalysisContext)
            context.RegisterCompilationStartAction(
                Sub(compilationContext)
                    ' We only care about compilations where interface type "DontInheritInterfaceTypeName" is available.
                    Dim interfaceType = compilationContext.Compilation.GetTypeByMetadataName(DontInheritInterfaceTypeName)
                    If interfaceType Is Nothing Then
                        Return
                    End If

                    ' Register an action that accesses the immutable state and reports diagnostics.
                    compilationContext.RegisterSymbolAction(
                        Sub(symbolContext)
                            AnalyzeSymbol(symbolContext, interfaceType)
                        End Sub,
                        SymbolKind.NamedType)
                End Sub)
        End Sub

        Public Sub AnalyzeSymbol(context As SymbolAnalysisContext, interfaceType As INamedTypeSymbol)
            ' Check if the symbol implements the interface type
            Dim namedType = DirectCast(context.Symbol, INamedTypeSymbol)
            If namedType.Interfaces.Contains(interfaceType) AndAlso
                Not namedType.ToDisplayString(SymbolDisplayFormat.VisualBasicErrorMessageFormat).Equals(AllowedInternalImplementationTypeName) Then

                Dim diag = Diagnostic.Create(Rule, namedType.Locations(0), namedType.Name, DontInheritInterfaceTypeName)
                context.ReportDiagnostic(diag)
            End If
        End Sub

    End Class
End Namespace
