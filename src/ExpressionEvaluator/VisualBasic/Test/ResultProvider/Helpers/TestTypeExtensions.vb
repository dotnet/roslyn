' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator.UnitTests

    Public Module TestTypeExtensions

        <Extension>
        Public Function GetTypeName(type As System.Type, Optional dynamicFlags As Boolean() = Nothing, Optional escapeKeywordIdentifiers As Boolean = False) As String
            Return type.GetTypeName(DynamicFlagsCustomTypeInfo.Create(dynamicFlags).GetCustomTypeInfo(), escapeKeywordIdentifiers)
        End Function

        <Extension>
        Public Function GetTypeName(type As System.Type, typeInfo As DkmClrCustomTypeInfo, Optional escapeKeywordIdentifiers As Boolean = False) As String
            Dim sawInvalidIdentifier As Boolean = Nothing
            Dim result = VisualBasicFormatter.Instance.GetTypeName(New TypeAndCustomInfo(CType(type, TypeImpl), typeInfo), escapeKeywordIdentifiers, sawInvalidIdentifier)
            Assert.False(sawInvalidIdentifier)
            Return result
        End Function

    End Module

End Namespace
