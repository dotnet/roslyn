' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Diagnostics.VisualBasic
Imports Microsoft.CodeAnalysis.Test.Utilities.LanguageServer
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageServer

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.LanguageServer
    Public Class VisualBasicLspBuildOnlyDiagnosticsTests
        Inherits AbstractLspBuildOnlyDiagnosticsTests

        Protected Overrides ReadOnly Property ErrorCodeType As Type
            Get
                Return GetType(ERRID)
            End Get
        End Property

        Protected Overrides ReadOnly Property LspBuildOnlyDiagnosticsType As Type
            Get
                Return GetType(VisualBasicLspBuildOnlyDiagnostics)
            End Get
        End Property

        Protected Overrides ReadOnly Property ExpectedDiagnosticCodes As Immutable.ImmutableArray(Of String)
            Get
                Dim errorCodes = [Enum].GetValues(GetType(ERRID))
                Dim supported = New VisualBasicCompilerDiagnosticAnalyzer().GetSupportedErrorCodes()

                Dim builder = ImmutableArray.CreateBuilder(Of String)
                For Each errorCode As Integer In errorCodes
                    If Not supported.Contains(errorCode) AndAlso
                        errorCode > ERRID.ERR_None AndAlso
                        errorCode < ERRID.WRN_NextAvailable Then
                        builder.Add("BC" & errorCode.ToString("D5"))
                    End If
                Next

                Return builder.ToImmutable()
            End Get
        End Property
    End Class
End Namespace
