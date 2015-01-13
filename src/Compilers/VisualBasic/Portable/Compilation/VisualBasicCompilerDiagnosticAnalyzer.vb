' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
                If errorCode > ERRID.ERR_None AndAlso errorCode < ERRID.FEATUREID_First Then
                    builder.Add(errorCode)
                End If
            Next

            Return builder.ToImmutable
        End Function
    End Class
End Namespace
