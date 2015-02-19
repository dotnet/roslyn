' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.ExpressionEvaluator

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    Public Module TestTypeExtensions

        <Extension>
        Public Function GetTypeName(type As Type, Optional escapeKeywordIdentifiers As Boolean = False) As String
            Return VisualBasicFormatter.Instance.GetTypeName(CType(type, TypeImpl), escapeKeywordIdentifiers)
        End Function

    End Module

End Namespace
