' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Reflection
Imports Microsoft.CodeAnalysis.Scripting.Hosting

Namespace Microsoft.CodeAnalysis.VisualBasic.Scripting.Hosting

    Public Class VisualBasicObjectFormatter
        Inherits ObjectFormatter

        Public Shared ReadOnly Property Instance As New VisualBasicObjectFormatter()

        Private Shared ReadOnly _impl As ObjectFormatter = New VisualBasicObjectFormatterImpl()

        Private Sub New()
        End Sub

        Public Overrides Function FormatObject(obj As Object, options As PrintOptions) As String
            Return _impl.FormatObject(obj, options)
        End Function

        Public Overrides Function FormatUnhandledException(e As Exception) As String
            Return _impl.FormatUnhandledException(e)
        End Function
    End Class

End Namespace
