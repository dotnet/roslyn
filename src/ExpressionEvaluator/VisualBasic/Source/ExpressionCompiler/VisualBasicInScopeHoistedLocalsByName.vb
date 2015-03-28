' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.ExpressionEvaluator

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    Friend NotInheritable Class VisualBasicInScopeHoistedLocalsByName
        Inherits InScopeHoistedLocals

        Private ReadOnly _fieldNames As ImmutableHashSet(Of String)

        Public Sub New(fieldNames As ImmutableHashSet(Of String))
            _fieldNames = fieldNames
        End Sub

        Public Overrides Function IsInScope(fieldName As String) As Boolean
            Return _fieldNames.Contains(fieldName)
        End Function
    End Class
End Namespace
