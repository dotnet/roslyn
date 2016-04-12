' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.PropertyPages
    Public Class HiddenIfMissingPropertyControlData
        Inherits PropertyControlData
        Public Sub New(id As Integer, name As String, formControl As Control)
            MyBase.New(id, name, formControl)
        End Sub

        Public Overrides Sub InitPropertyValue()
            MyBase.InitPropertyValue()

            If (IsMissing) Then
                IsHidden = True
            End If
        End Sub
    End Class
End Namespace