' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Testing

Public Class VisualBasicProjectState
    Inherits ProjectState

    Public Sub New(name As String)
        MyBase.New(name, defaultPrefix:="Test", defaultExtension:="vb")
    End Sub

    Public Overrides ReadOnly Property Language As String
        Get
            Return LanguageNames.VisualBasic
        End Get
    End Property
End Class
