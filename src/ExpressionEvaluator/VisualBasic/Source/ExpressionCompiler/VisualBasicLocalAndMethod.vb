' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    Friend NotInheritable Class VisualBasicLocalAndMethod : Inherits LocalAndMethod

        Public Sub New(localName As String, localDisplayName As String, methodName As String, flags As DkmClrCompilationResultFlags)
            MyBase.New(localName, localDisplayName, methodName, flags)
        End Sub

        Public Overrides Function GetCustomTypeInfo() As CustomTypeInfo
            Return Nothing
        End Function
    End Class

End Namespace
