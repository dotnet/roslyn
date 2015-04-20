' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    Public Module TestTypeExtensions

        <Extension>
        Public Function GetTypeName(type As System.Type, Optional dynamicFlags As Boolean() = Nothing, Optional escapeKeywordIdentifiers As Boolean = False) As String
            Return type.GetTypeName(ResultProviderTestBase.MakeDynamicFlagsCustomTypeInfo(dynamicFlags).GetCustomTypeInfo(), escapeKeywordIdentifiers)
        End Function

        <Extension>
        Public Function GetTypeName(type As System.Type, typeInfo As DkmClrCustomTypeInfo, Optional escapeKeywordIdentifiers As Boolean = False) As String
            Return VisualBasicFormatter.Instance.GetTypeName(New TypeAndCustomInfo(CType(type, TypeImpl), typeInfo), escapeKeywordIdentifiers)
        End Function

    End Module

End Namespace
