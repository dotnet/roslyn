' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.ObjectModel
Imports Microsoft.CodeAnalysis.ExpressionEvaluator

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    Friend NotInheritable Class VisualBasicCompileResult : Inherits CompileResult

        Public Sub New(
            assembly As Byte(),
            typeName As String,
            methodName As String,
            formatSpecifiers As ReadOnlyCollection(Of String))

            MyBase.New(assembly, typeName, methodName, formatSpecifiers)
        End Sub

        Public Overrides Function GetCustomTypeInfo() As CustomTypeInfo
            Return Nothing
        End Function
    End Class

End Namespace
