' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.AutomaticEndConstructCorrection
    Friend Class AutomaticEndConstructSet
        Private Shared s_set As HashSet(Of String) = New HashSet(Of String)(CaseInsensitiveComparison.Comparer) _
                From {"structure", "enum", "interface", "class", "module", "namespace", "sub", "function", "get", "set"}

        Public Shared Function Contains(keyword As String) As Boolean
            Return s_set.Contains(keyword)
        End Function
    End Class
End Namespace
