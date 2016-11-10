' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Globalization
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' The Diagnostic class allows formatting of Visual Basic diagnostics. 
    ''' </summary>
    Public Class VisualBasicDiagnosticFormatter
        Inherits DiagnosticFormatter

        Protected Sub New()
        End Sub

        Friend Overrides Function FormatSourceSpan(span As LinePositionSpan, formatter As IFormatProvider) As String
            Return "(" & (span.Start.Line + 1).ToString() & ") "
        End Function

        ''' <summary>
        ''' Gets the current DiagnosticFormatter instance.
        ''' </summary>
        Public Shared Shadows ReadOnly Property Instance As New VisualBasicDiagnosticFormatter()
    End Class
End Namespace
