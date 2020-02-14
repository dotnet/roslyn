﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic

Namespace Microsoft.CodeAnalysis.Diagnostics.VisualBasic
    ''' <summary>
    ''' DiagnosticAnalyzer for VB compiler's syntax/semantic/compilation diagnostics.
    ''' </summary>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicCompilerDiagnosticAnalyzer
        Inherits CompilerDiagnosticAnalyzer

        Friend Overrides ReadOnly Property MessageProvider As CommonMessageProvider
            Get
                Return CodeAnalysis.VisualBasic.MessageProvider.Instance
            End Get
        End Property

        Friend Overrides Function GetSupportedErrorCodes() As ImmutableArray(Of Integer)
            Dim errorCodes As Array = [Enum].GetValues(GetType(ERRID))
            Dim builder = ImmutableArray.CreateBuilder(Of Integer)
            For Each errorCode As Integer In errorCodes

                ' these errors are not supported by live analysis
                If errorCode = ERRID.ERR_TypeRefResolutionError3 OrElse
                   errorCode = ERRID.ERR_MissingRuntimeHelper OrElse
                   errorCode = ERRID.ERR_CannotGotoNonScopeBlocksWithClosure Then
                    Continue For
                End If

                If errorCode > ERRID.ERR_None AndAlso errorCode < ERRID.ERRWRN_NextAvailable Then
                    builder.Add(errorCode)
                End If
            Next

            Return builder.ToImmutable
        End Function
    End Class
End Namespace
