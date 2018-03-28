' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Text
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ObjectBrowser.Mocks
    Public Class MockObjectBrowserDescription
        Implements IVsObjectBrowserDescription3

        Private ReadOnly _builder As New StringBuilder()

        Public Function AddDescriptionText3(pText As String, obdSect As VSOBDESCRIPTIONSECTION, pHyperJump As IVsNavInfo) As Integer Implements IVsObjectBrowserDescription3.AddDescriptionText3
            If pText = vbLf Then
                _builder.AppendLine()
            Else
                _builder.Append(pText)
            End If

            Return VSConstants.S_OK
        End Function

        Public Function ClearDescriptionText() As Integer Implements IVsObjectBrowserDescription3.ClearDescriptionText
            Throw New NotSupportedException()
        End Function

        Public Overrides Function ToString() As String
            Return _builder.ToString()
        End Function
    End Class
End Namespace
