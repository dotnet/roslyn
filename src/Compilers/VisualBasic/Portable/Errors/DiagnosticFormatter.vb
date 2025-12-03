' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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
            Return "(" & (span.Start.Line + 1).ToString(Globalization.CultureInfo.InvariantCulture) & ") "
        End Function

        ''' <summary>
        ''' Gets the current DiagnosticFormatter instance.
        ''' </summary>
        Public Shared Shadows ReadOnly Property Instance As New VisualBasicDiagnosticFormatter()

        Friend Overrides Function HasDefaultHelpLinkUri(diagnostic As Diagnostic) As Boolean
            Return diagnostic.Descriptor.HelpLinkUri = ErrorFactory.GetHelpLink(CType(diagnostic.Code, ERRID))
        End Function
    End Class
End Namespace
