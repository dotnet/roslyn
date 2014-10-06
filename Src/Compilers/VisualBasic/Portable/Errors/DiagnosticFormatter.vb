' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Globalization
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' The Diagnostic class allows formatting of Visual Basic diagnostics. 
    ''' </summary>
    Public Class VBDiagnosticFormatter
        Inherits DiagnosticFormatter

        Protected Sub New()
        End Sub

        Friend Overrides Function FormatSourceSpan(span As LinePositionSpan, formatter As IFormatProvider) As String
            Return String.Format("({0}) ", span.Start.Line + 1)
        End Function

        ''' <summary>
        ''' Gets the current DiagnosticFormatter instance.
        ''' </summary>
        Public Shared Shadows ReadOnly Instance As VBDiagnosticFormatter = New VBDiagnosticFormatter()
    End Class
End Namespace