' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.AutomaticEndConstructCorrection
    Friend Class AutomaticEndConstructSet
        Private Shared ReadOnly s_set As HashSet(Of String) = New HashSet(Of String)(CaseInsensitiveComparison.Comparer) _
                From {"structure", "enum", "interface", "class", "module", "namespace", "sub", "function", "get", "set"}

        Public Shared Function Contains(keyword As String) As Boolean
            Return s_set.Contains(keyword)
        End Function
    End Class
End Namespace
