' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports My.Resources

Namespace BasicAnalyzers
    ''' <summary>
    ''' Analyzer to demonstrate compilation-wide analysis.
    ''' <para>
    ''' Analysis scenario:
    ''' (a) You have an interface, which is a well-known secure interface, i.e. it is a marker for all secure types in an assembly.
    ''' (b) You have a method level attribute which marks the owning method as unsecure. An interface which has any member with such an attribute, must be considered unsecure.
    ''' (c) We want to report diagnostics for types implementing the well-known secure interface that also implement any unsecure interface.
    ''' 
    ''' Analyzer performs compilation-wide analysis to detect such violating types and reports diagnostics for them in the compilation end action.
    ''' </para>
    ''' <para>
    ''' The analyzer performs this analysis by registering:
    ''' (a) A compilation start action, which initializes per-compilation state:
    '''     (i) Immutable state: We fetch and store the type symbols for the well-known secure interface type and unsecure method attribute type in the compilation.
    '''     (ii) Mutable state: We maintain a set of all types implementing well-known secure interface type and set of all interface types with an unsecure method.
    ''' (b) A compilation symbol action, which identifies all named types that implement the well-known secure interface, and all method symbols that have the unsecure method attribute.
    ''' (c) A compilation end action which reports diagnostics for types implementing the well-known secure interface that also implementing any unsecure interface.
    ''' </para>
    ''' </summary>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class CompilationStartedAnalyzerWithCompilationWideAnalysis
        Inherits DiagnosticAnalyzer

#Region "Descriptor fields"
        Friend Shared ReadOnly Title As LocalizableString = New LocalizableResourceString(NameOf(Resources.CompilationStartedAnalyzerWithCompilationWideAnalysisTitle), Resources.ResourceManager, GetType(Resources))
        Friend Shared ReadOnly MessageFormat As LocalizableString = New LocalizableResourceString(NameOf(Resources.CompilationStartedAnalyzerWithCompilationWideAnalysisMessageFormat), Resources.ResourceManager, GetType(Resources))
        Friend Shared ReadOnly Description As LocalizableString = New LocalizableResourceString(NameOf(Resources.CompilationStartedAnalyzerWithCompilationWideAnalysisDescription), Resources.ResourceManager, GetType(Resources))

        Friend Shared Rule As New DiagnosticDescriptor(DiagnosticIds.CompilationStartedAnalyzerWithCompilationWideAnalysisRuleId, Title, MessageFormat, DiagnosticCategories.Stateful, DiagnosticSeverity.Warning, isEnabledByDefault:=True, description:=Description)
#End Region

        Public Const UnsecureMethodAttributeName As String = "MyNamespace.UnsecureMethodAttribute"
        Public Const SecureTypeInterfaceName As String = "MyNamespace.ISecureType"

        Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(Rule)
            End Get
        End Property

        Public Overrides Sub Initialize(context As AnalysisContext)
            context.RegisterCompilationStartAction(
                Sub(compilationContext)
                    ' Check if the attribute type marking unsecure methods is defined.
                    Dim unsecureMethodAttributeType = compilationContext.Compilation.GetTypeByMetadataName(UnsecureMethodAttributeName)
                    If unsecureMethodAttributeType Is Nothing Then
                        Return
                    End If

                    ' Check if the interface type marking secure types is defined.
                    Dim secureTypeInterfaceType = compilationContext.Compilation.GetTypeByMetadataName(SecureTypeInterfaceName)
                    If secureTypeInterfaceType Is Nothing Then
                        Return
                    End If

                    ' Initialize state in the start action.
                    Dim analyzer = New CompilationAnalyzer(unsecureMethodAttributeType, secureTypeInterfaceType)

                    ' Register an intermediate non-end action that accesses and modifies the state.
                    compilationContext.RegisterSymbolAction(AddressOf analyzer.AnalyzeSymbol, SymbolKind.NamedType, SymbolKind.Method)

                    ' Register an end action to report diagnostics based on the final state.
                    compilationContext.RegisterCompilationEndAction(AddressOf analyzer.CompilationEndAction)
                End Sub)
        End Sub

        Private Class CompilationAnalyzer

#Region "Per-Compilation immutable state"
            Private ReadOnly _unsecureMethodAttributeType As INamedTypeSymbol
            Private ReadOnly _secureTypeInterfaceType As INamedTypeSymbol
#End Region

#Region "Per-Compilation mutable state"
            ''' <summary>
            ''' List of secure types in the compilation implementing interface <see cref="SecureTypeInterfaceName"/>.
            ''' </summary>
            Private _secureTypes As List(Of INamedTypeSymbol)

            ''' <summary>
            ''' Set of unsecure interface types in the compilation that have methods with an attribute of <see cref="_unsecureMethodAttributeType"/>.
            ''' </summary>
            Private _interfacesWithUnsecureMethods As HashSet(Of INamedTypeSymbol)
#End Region

#Region "State intialization"
            Public Sub New(unsecureMethodAttributeType As INamedTypeSymbol, secureTypeInterfaceType As INamedTypeSymbol)
                _unsecureMethodAttributeType = unsecureMethodAttributeType
                _secureTypeInterfaceType = secureTypeInterfaceType

                _secureTypes = Nothing
                _interfacesWithUnsecureMethods = Nothing
            End Sub
#End Region

#Region "Intermediate actions"
            Public Sub AnalyzeSymbol(context As SymbolAnalysisContext)
                Select Case context.Symbol.Kind
                    Case SymbolKind.NamedType
                        ' Check if the symbol implements "_secureTypeInterfaceType".
                        Dim namedType = DirectCast(context.Symbol, INamedTypeSymbol)
                        If namedType.AllInterfaces.Contains(_secureTypeInterfaceType) Then
                            _secureTypes = If(_secureTypes, New List(Of INamedTypeSymbol)())
                            _secureTypes.Add(namedType)
                        End If

                        Exit Select

                    Case SymbolKind.Method
                        ' Check if this is an interface method with "_unsecureMethodAttributeType" attribute.
                        Dim method = DirectCast(context.Symbol, IMethodSymbol)
                        If method.ContainingType.TypeKind = TypeKind.Interface AndAlso
                            method.GetAttributes().Any(Function(a) a.AttributeClass.Equals(_unsecureMethodAttributeType)) Then
                            _interfacesWithUnsecureMethods = If(_interfacesWithUnsecureMethods, New HashSet(Of INamedTypeSymbol)())
                            _interfacesWithUnsecureMethods.Add(method.ContainingType)
                        End If

                        Exit Select
                End Select
            End Sub
#End Region

#Region "End action"
            Public Sub CompilationEndAction(context As CompilationAnalysisContext)
                If _interfacesWithUnsecureMethods Is Nothing OrElse _secureTypes Is Nothing Then
                    ' No violating types.
                    Return
                End If

                ' Report diagnostic for violating named types.
                For Each secureType In _secureTypes
                    For Each unsecureInterface In _interfacesWithUnsecureMethods
                        If secureType.AllInterfaces.Contains(unsecureInterface) Then
                            Dim diag = Diagnostic.Create(Rule, secureType.Locations(0), secureType.Name, SecureTypeInterfaceName, unsecureInterface.Name)
                            context.ReportDiagnostic(diag)
                            Exit For
                        End If
                    Next
                Next
            End Sub
#End Region

        End Class
    End Class
End Namespace
