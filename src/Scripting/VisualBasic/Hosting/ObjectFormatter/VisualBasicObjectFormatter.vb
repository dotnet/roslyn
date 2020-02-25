﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Scripting.Hosting

Namespace Microsoft.CodeAnalysis.VisualBasic.Scripting.Hosting

    Public NotInheritable Class VisualBasicObjectFormatter
        Inherits ObjectFormatter

        Public Shared ReadOnly Property Instance As New VisualBasicObjectFormatter()

        Private Shared ReadOnly s_impl As ObjectFormatter = New VisualBasicObjectFormatterImpl()

        Private Sub New()
        End Sub

        Public Overrides Function FormatObject(obj As Object, options As PrintOptions) As String
            Return s_impl.FormatObject(obj, options)
        End Function

        Public Overrides Function FormatException(e As Exception) As String
            Return s_impl.FormatException(e)
        End Function
    End Class

End Namespace
