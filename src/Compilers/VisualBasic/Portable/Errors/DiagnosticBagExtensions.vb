' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Module DiagnosticBagExtensions

        ''' <summary>
        ''' Add a diagnostic to the bag.
        ''' </summary>
        ''' <param name = "diagnostics"></param>
        ''' <param name = "code"></param>
        ''' <param name = "location"></param>
        ''' <returns></returns>
        <System.Runtime.CompilerServices.Extension()>
        Friend Function Add(diagnostics As DiagnosticBag, code As ERRID, location As Location) As DiagnosticInfo
            Dim info = ErrorFactory.ErrorInfo(code)
            Dim diag = New VBDiagnostic(info, location)
            diagnostics.Add(diag)
            Return info
        End Function

        ''' <summary>
        ''' Add a diagnostic to the bag.
        ''' </summary>
        ''' <param name = "diagnostics"></param>
        ''' <param name = "code"></param>
        ''' <param name = "location"></param>
        ''' <param name = "args"></param>
        ''' <returns></returns>
        <System.Runtime.CompilerServices.Extension()>
        Friend Function Add(diagnostics As DiagnosticBag, code As ERRID, location As Location, ParamArray args As Object()) As DiagnosticInfo
            Dim info = ErrorFactory.ErrorInfo(code, args)
            Dim diag = New VBDiagnostic(info, location)
            diagnostics.Add(diag)
            Return info
        End Function

        <System.Runtime.CompilerServices.Extension()>
        Friend Sub Add(diagnostics As DiagnosticBag, info As DiagnosticInfo, location As Location)
            Dim diag = New VBDiagnostic(info, location)
            diagnostics.Add(diag)
        End Sub

        ''' <summary>
        ''' Appends diagnostics from useSiteDiagnostics into diagnostics and returns True if there were any errors.
        ''' </summary>
        <System.Runtime.CompilerServices.Extension()>
        Friend Function Add(
            diagnostics As DiagnosticBag,
            node As VisualBasicSyntaxNode,
            useSiteDiagnostics As IReadOnlyCollection(Of DiagnosticInfo)
        ) As Boolean
            Return Not useSiteDiagnostics.IsNullOrEmpty AndAlso diagnostics.Add(node.GetLocation, useSiteDiagnostics)
        End Function

        <System.Runtime.CompilerServices.Extension()>
        Friend Function Add(
            diagnostics As DiagnosticBag,
            node As BoundNode,
            useSiteDiagnostics As IReadOnlyCollection(Of DiagnosticInfo)
        ) As Boolean
            Return Not useSiteDiagnostics.IsNullOrEmpty AndAlso diagnostics.Add(node.Syntax.GetLocation, useSiteDiagnostics)
        End Function

        <System.Runtime.CompilerServices.Extension()>
        Friend Function Add(
            diagnostics As DiagnosticBag,
            node As SyntaxNodeOrToken,
            useSiteDiagnostics As IReadOnlyCollection(Of DiagnosticInfo)
        ) As Boolean
            Return Not useSiteDiagnostics.IsNullOrEmpty AndAlso diagnostics.Add(node.GetLocation, useSiteDiagnostics)
        End Function

        <System.Runtime.CompilerServices.Extension()>
        Friend Function Add(
            diagnostics As DiagnosticBag,
            location As Location,
            useSiteDiagnostics As IReadOnlyCollection(Of DiagnosticInfo)
        ) As Boolean

            If useSiteDiagnostics.IsNullOrEmpty Then
                Return False
            End If

            For Each info In useSiteDiagnostics
                Debug.Assert(info.Severity = DiagnosticSeverity.Error)
                Dim diag As New VBDiagnostic(info, location)
                diagnostics.Add(diag)
            Next

            Return True
        End Function

    End Module

End Namespace
