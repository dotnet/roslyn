' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend NotInheritable Class BindingDiagnosticBag
        Inherits BindingDiagnosticBag(Of AssemblySymbol)

        Private Shared ReadOnly s_poolWithBoth As ObjectPool(Of BindingDiagnosticBag) = New ObjectPool(Of BindingDiagnosticBag)(Function() New BindingDiagnosticBag(s_poolWithBoth, New DiagnosticBag(), New HashSet(Of AssemblySymbol)()))
        Private Shared ReadOnly s_poolWithDiagnosticsOnly As ObjectPool(Of BindingDiagnosticBag) = New ObjectPool(Of BindingDiagnosticBag)(Function() New BindingDiagnosticBag(s_poolWithDiagnosticsOnly, New DiagnosticBag(), dependenciesBag:=Nothing))
        Private Shared ReadOnly s_poolWithDependenciesOnly As ObjectPool(Of BindingDiagnosticBag) = New ObjectPool(Of BindingDiagnosticBag)(Function() New BindingDiagnosticBag(s_poolWithDependenciesOnly, diagnosticBag:=Nothing, New HashSet(Of AssemblySymbol)()))
        Private Shared ReadOnly s_poolWithConcurrent As ObjectPool(Of BindingDiagnosticBag) = New ObjectPool(Of BindingDiagnosticBag)(Function() New BindingDiagnosticBag(s_poolWithConcurrent, New DiagnosticBag(), New ConcurrentSet(Of AssemblySymbol)()))

        Public Shared ReadOnly Discarded As New BindingDiagnosticBag(Nothing, Nothing)

        Private ReadOnly _pool As ObjectPool(Of BindingDiagnosticBag)

        Private Sub New(diagnosticBag As DiagnosticBag, dependenciesBag As ICollection(Of AssemblySymbol))
            MyBase.New(diagnosticBag, dependenciesBag)
        End Sub

        Private Sub New(pool As ObjectPool(Of BindingDiagnosticBag), diagnosticBag As DiagnosticBag, dependenciesBag As ICollection(Of AssemblySymbol))
            MyBase.New(diagnosticBag, dependenciesBag)
            _pool = pool
        End Sub

        Friend Shared Function GetInstance() As BindingDiagnosticBag
            Return s_poolWithBoth.Allocate()
        End Function

        Friend Shared Function GetInstance(withDiagnostics As Boolean, withDependencies As Boolean) As BindingDiagnosticBag
            If withDependencies Then
                If withDiagnostics Then
                    Return GetInstance()
                End If

                Return s_poolWithDependenciesOnly.Allocate()

            ElseIf withDiagnostics Then
                Return s_poolWithDiagnosticsOnly.Allocate()
            Else
                Return Discarded
            End If
        End Function

        Friend Shared Function GetInstance(template As BindingDiagnosticBag) As BindingDiagnosticBag
            Return GetInstance(withDiagnostics:=template.AccumulatesDiagnostics, withDependencies:=template.AccumulatesDependencies)
        End Function

        ''' <summary>
        ''' Get an instance suitable for concurrent additions to both underlying bags.
        ''' </summary>
        Friend Shared Function GetConcurrentInstance() As BindingDiagnosticBag
            Return s_poolWithConcurrent.Allocate()
        End Function

        Friend ReadOnly Property IsEmpty As Boolean
            Get
                Return (DiagnosticBag Is Nothing OrElse DiagnosticBag.IsEmptyWithoutResolution) AndAlso DependenciesBag.IsNullOrEmpty()
            End Get
        End Property

        Friend Overrides Sub Free()
            If _pool IsNot Nothing Then
                Clear()
                _pool.Free(Me)
            Else
                MyBase.Free()
            End If
        End Sub

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

        Friend Overloads Function Add(code As ERRID, location As Location) As DiagnosticInfo
            Dim info = ErrorFactory.ErrorInfo(code)
            Add(info, location)
            Return info
        End Function

        Friend Overloads Function Add(code As ERRID, location As Location, ParamArray args As Object()) As DiagnosticInfo
            Dim info = ErrorFactory.ErrorInfo(code, args)
            Add(info, location)
            Return info
        End Function

        Friend Overloads Sub Add(info As DiagnosticInfo, location As Location)
            DiagnosticBag?.Add(New VBDiagnostic(info, location))
        End Sub

    End Class
End Namespace
