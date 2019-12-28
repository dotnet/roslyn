' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

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
                Return (DiagnosticBag Is Nothing OrElse DiagnosticBag.IsEmptyWithoutResolution) AndAlso (DependenciesBag Is Nothing OrElse DependenciesBag.Count = 0)
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

    End Class
End Namespace
