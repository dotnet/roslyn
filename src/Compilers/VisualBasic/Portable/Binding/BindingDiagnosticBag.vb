' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    ' PROTOTYPE(UsedAssemblyReferences) Consider if it makes sense to move this type into its own file
    Friend Module BindingDiagnosticBagExtensions

        <System.Runtime.CompilerServices.Extension()>
        Friend Function Add(diagnostics As CodeAnalysis.BindingDiagnosticBag, code As ERRID, location As Location) As DiagnosticInfo
            Dim info = ErrorFactory.ErrorInfo(code)
            diagnostics.DiagnosticBag?.Add(New VBDiagnostic(info, location))
            Return info
        End Function

        <System.Runtime.CompilerServices.Extension()>
        Friend Function Add(diagnostics As CodeAnalysis.BindingDiagnosticBag, code As ERRID, location As Location, ParamArray args As Object()) As DiagnosticInfo
            Dim info = ErrorFactory.ErrorInfo(code, args)
            diagnostics.DiagnosticBag?.Add(New VBDiagnostic(info, location))
            Return info
        End Function

        <System.Runtime.CompilerServices.Extension()>
        Friend Sub Add(diagnostics As CodeAnalysis.BindingDiagnosticBag, info As DiagnosticInfo, location As Location)
            diagnostics.DiagnosticBag?.Add(New VBDiagnostic(info, location))
        End Sub

    End Module

    Friend NotInheritable Class BindingDiagnosticBag
        Inherits BindingDiagnosticBag(Of AssemblySymbol)

        Public Shared ReadOnly Discarded As New BindingDiagnosticBag(Nothing, Nothing)

        Public Sub New()
            MyBase.New(usePool:=False)
        End Sub

        Private Sub New(usePool As Boolean)
            MyBase.New(usePool)
        End Sub

        Public Sub New(diagnosticBag As DiagnosticBag)
            MyBase.New(diagnosticBag, dependenciesBag:=Nothing)
        End Sub

        Public Sub New(diagnosticBag As DiagnosticBag, dependenciesBag As ICollection(Of AssemblySymbol))
            MyBase.New(diagnosticBag, dependenciesBag)
        End Sub

        Friend Shared Function GetInstance() As BindingDiagnosticBag
            Return New BindingDiagnosticBag(usePool:=True)
        End Function

        Friend ReadOnly Property IsEmpty As Boolean
            Get
                Return (DiagnosticBag Is Nothing OrElse DiagnosticBag.IsEmptyWithoutResolution) AndAlso DependenciesBag.IsNullOrEmpty()
            End Get
        End Property

        Protected Overrides Function ReportUseSiteDiagnostic(diagnosticInfo As DiagnosticInfo, diagnosticBag As DiagnosticBag, location As Location) As Boolean
            Debug.Assert(diagnosticInfo.Severity = DiagnosticSeverity.Error)
            diagnosticBag.Add(New VBDiagnostic(diagnosticInfo, location))
            Return True
        End Function

        Friend Overloads Function Add(node As BoundNode, useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)) As Boolean
            Return Add(node.Syntax.Location, useSiteInfo)
        End Function

        Friend Overloads Function Add(syntax As SyntaxNodeOrToken, useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)) As Boolean
            Return Add(syntax.GetLocation(), useSiteInfo)
        End Function

        Friend Function ReportUseSite(symbol As Symbol, node As SyntaxNode) As Boolean
            Return ReportUseSite(symbol, node.Location)
        End Function

        Friend Function ReportUseSite(symbol As Symbol, token As SyntaxToken) As Boolean
            Return ReportUseSite(symbol, token.GetLocation())
        End Function

        Friend Function ReportUseSite(symbol As Symbol, location As Location) As Boolean
            If symbol IsNot Nothing Then
                Return Add(symbol.GetUseSiteInfo(), location)
            End If

            Return False
        End Function

        Friend Sub AddAssembliesUsedByCrefTarget(symbol As Symbol)
            If DependenciesBag Is Nothing Then
                Return
            End If

            Dim ns = TryCast(symbol, NamespaceSymbol)

            If ns IsNot Nothing Then
                Debug.Assert(Not ns.IsGlobalNamespace)
                AddAssembliesUsedByNamespaceReference(ns)
            Else
                AddDependencies(If(TryCast(symbol, TypeSymbol), symbol.ContainingType))
            End If
        End Sub

        Friend Overloads Sub AddDependencies(symbol As Symbol)
            If DependenciesBag Is Nothing OrElse symbol Is Nothing Then
                Return
            End If

            AddDependencies(symbol.GetUseSiteInfo())
        End Sub

        Friend Sub AddAssembliesUsedByNamespaceReference(ns As NamespaceSymbol)
            If DependenciesBag Is Nothing Then
                Return
            End If

            AddAssembliesUsedByNamespaceReferenceImpl(ns)
        End Sub

        Private Sub AddAssembliesUsedByNamespaceReferenceImpl(ns As NamespaceSymbol)
            ' Treat all assemblies contributing to this namespace symbol as used
            If ns.Extent.Kind = NamespaceKind.Compilation Then
                For Each constituent In ns.ConstituentNamespaces
                    AddAssembliesUsedByNamespaceReferenceImpl(constituent)
                Next
            Else
                Dim containingAssembly As AssemblySymbol = ns.ContainingAssembly

                If containingAssembly IsNot Nothing AndAlso Not containingAssembly.IsMissing Then
                    DependenciesBag.Add(containingAssembly)
                End If
            End If
        End Sub

    End Class
End Namespace
