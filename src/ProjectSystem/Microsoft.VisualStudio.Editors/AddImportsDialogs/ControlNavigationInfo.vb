' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.AddImports
    Friend Class ControlNavigationInfo
        Public ReadOnly NextControl As Control
        Public ReadOnly PreviousControl As Control

        Public Sub New(ByVal NextControl As Control, ByVal PreviousControl As Control)
            Me.NextControl = NextControl
            Me.PreviousControl = PreviousControl
        End Sub
    End Class
End Namespace