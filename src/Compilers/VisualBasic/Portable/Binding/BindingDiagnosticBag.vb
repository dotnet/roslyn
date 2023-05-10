' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend NotInheritable Class BindingDiagnosticBagFactory
        Private Shared ReadOnly s_reportUseSiteDiagnostic As Func(Of DiagnosticInfo, DiagnosticBag, Location, Boolean) =
            Function(diagnosticInfo, diagnosticBag, location) As Boolean
                Debug.Assert(diagnosticInfo.Severity = DiagnosticSeverity.Error)
                diagnosticBag.Add(New VBDiagnostic(diagnosticInfo, location))
                Return True
            End Function

        Public Shared Function NewBag() As BindingDiagnosticBag
            Return New BindingDiagnosticBag(usePool:=False, s_reportUseSiteDiagnostic)
        End Function

        Private Shared Function NewBag(usePool As Boolean) As BindingDiagnosticBag
            Return New BindingDiagnosticBag(usePool, s_reportUseSiteDiagnostic)
        End Function

        Public Shared Function NewBag(diagnosticBag As DiagnosticBag) As BindingDiagnosticBag
            Return New BindingDiagnosticBag(diagnosticBag, dependenciesBag:=Nothing, s_reportUseSiteDiagnostic)
        End Function

        Public Shared Function NewBag(diagnosticBag As DiagnosticBag, dependenciesBag As ICollection(Of AssemblySymbol)) As BindingDiagnosticBag
            Return New BindingDiagnosticBag(diagnosticBag, dependenciesBag, s_reportUseSiteDiagnostic)
        End Function

        Friend Shared Function GetInstance() As BindingDiagnosticBag
            Return NewBag(usePool:=True)
        End Function

        Friend Shared Function GetInstance(withDiagnostics As Boolean, withDependencies As Boolean) As BindingDiagnosticBag
            If withDependencies Then
                If withDiagnostics Then
                    Return GetInstance()
                End If

                Return BindingDiagnosticBagFactory.NewBag(diagnosticBag:=Nothing, PooledHashSet(Of AssemblySymbol).GetInstance())

            ElseIf withDiagnostics Then
                Return BindingDiagnosticBagFactory.NewBag(DiagnosticBag.GetInstance())
            Else
                Return BindingDiagnosticBag.Discarded
            End If
        End Function

        Friend Shared Function GetInstance(template As BindingDiagnosticBag) As BindingDiagnosticBag
            Return GetInstance(withDiagnostics:=template.AccumulatesDiagnostics, withDependencies:=template.AccumulatesDependencies)
        End Function

        Friend Shared Function Create(withDiagnostics As Boolean, withDependencies As Boolean) As BindingDiagnosticBag
            If withDependencies Then
                If withDiagnostics Then
                    Return BindingDiagnosticBagFactory.NewBag()
                End If

                Return BindingDiagnosticBagFactory.NewBag(diagnosticBag:=Nothing, New HashSet(Of AssemblySymbol)())

            ElseIf withDiagnostics Then
                Return BindingDiagnosticBagFactory.NewBag(New DiagnosticBag())
            Else
                Return BindingDiagnosticBag.Discarded
            End If
        End Function

        Friend Shared Function Create(template As BindingDiagnosticBag) As BindingDiagnosticBag
            Return Create(withDiagnostics:=template.AccumulatesDiagnostics, withDependencies:=template.AccumulatesDependencies)
        End Function
    End Class

    Friend Module BindingDiagnosticBagExtensions
        <Extension>
        Friend Function IsEmpty(diagnosticBag As BindingDiagnosticBag) As Boolean
            Return (diagnosticBag.DiagnosticBag Is Nothing OrElse diagnosticBag.DiagnosticBag.IsEmptyWithoutResolution) AndAlso diagnosticBag.DependenciesBag.IsNullOrEmpty()
        End Function

        <Extension>
        Friend Function Add(diagnosticBag As BindingDiagnosticBag, node As BoundNode, useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)) As Boolean
            Return diagnosticBag.Add(node.Syntax.Location, useSiteInfo)
        End Function

        <Extension>
        Friend Function Add(diagnosticBag As BindingDiagnosticBag, syntax As SyntaxNodeOrToken, useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)) As Boolean
            Return diagnosticBag.Add(syntax.GetLocation(), useSiteInfo)
        End Function

        <Extension>
        Friend Function ReportUseSite(diagnosticBag As BindingDiagnosticBag, symbol As Symbol, node As SyntaxNode) As Boolean
            Return ReportUseSite(diagnosticBag, symbol, node.Location)
        End Function

        <Extension>
        Friend Function ReportUseSite(diagnosticBag As BindingDiagnosticBag, symbol As Symbol, token As SyntaxToken) As Boolean
            Return ReportUseSite(diagnosticBag, symbol, token.GetLocation())
        End Function

        <Extension>
        Friend Function ReportUseSite(diagnosticBag As BindingDiagnosticBag, symbol As Symbol, location As Location) As Boolean
            If symbol IsNot Nothing Then
                Return diagnosticBag.Add(symbol.GetUseSiteInfo(), location)
            End If

            Return False
        End Function

        <Extension>
        Friend Sub AddAssembliesUsedByCrefTarget(diagnosticBag As BindingDiagnosticBag, symbol As Symbol)
            If diagnosticBag.DependenciesBag Is Nothing Then
                Return
            End If

            Dim ns = TryCast(symbol, NamespaceSymbol)

            If ns IsNot Nothing Then
                Debug.Assert(Not ns.IsGlobalNamespace)
                AddAssembliesUsedByNamespaceReference(diagnosticBag, ns)
            Else
                AddDependencies(diagnosticBag, If(TryCast(symbol, TypeSymbol), symbol.ContainingType))
            End If
        End Sub

        <Extension>
        Friend Sub AddDependencies(diagnosticBag As BindingDiagnosticBag, symbol As Symbol)
            If diagnosticBag.DependenciesBag Is Nothing OrElse symbol Is Nothing Then
                Return
            End If

            diagnosticBag.AddDependencies(symbol.GetUseSiteInfo())
        End Sub

        <Extension>
        Friend Sub AddAssembliesUsedByNamespaceReference(diagnosticBag As BindingDiagnosticBag, ns As NamespaceSymbol)
            If diagnosticBag.DependenciesBag Is Nothing Then
                Return
            End If

            AddAssembliesUsedByNamespaceReferenceImpl(diagnosticBag, ns)
        End Sub

        Private Sub AddAssembliesUsedByNamespaceReferenceImpl(diagnosticBag As BindingDiagnosticBag, ns As NamespaceSymbol)
            ' Treat all assemblies contributing to this namespace symbol as used
            If ns.Extent.Kind = NamespaceKind.Compilation Then
                For Each constituent In ns.ConstituentNamespaces
                    AddAssembliesUsedByNamespaceReferenceImpl(diagnosticBag, constituent)
                Next
            Else
                Dim containingAssembly As AssemblySymbol = ns.ContainingAssembly

                If containingAssembly IsNot Nothing AndAlso Not containingAssembly.IsMissing Then
                    diagnosticBag.DependenciesBag.Add(containingAssembly)
                End If
            End If
        End Sub

        <Extension>
        Friend Function Add(diagnosticBag As BindingDiagnosticBag, code As ERRID, location As Location) As DiagnosticInfo
            Dim info = ErrorFactory.ErrorInfo(code)
            Add(diagnosticBag, info, location)
            Return info
        End Function

        <Extension>
        Friend Function Add(diagnosticBag As BindingDiagnosticBag, code As ERRID, location As Location, ParamArray args As Object()) As DiagnosticInfo
            Dim info = ErrorFactory.ErrorInfo(code, args)
            Add(diagnosticBag, info, location)
            Return info
        End Function

        <Extension>
        Friend Sub Add(diagnosticBag As BindingDiagnosticBag, info As DiagnosticInfo, location As Location)
            diagnosticBag.DiagnosticBag?.Add(New VBDiagnostic(info, location))
        End Sub
    End Module
End Namespace
