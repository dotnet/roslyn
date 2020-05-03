' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.ObjectModel
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    Friend NotInheritable Class VisualBasicCompileResult
        Inherits CompileResult

        Private ReadOnly _method As MethodSymbol

        Public Sub New(
            assembly As Byte(),
            method As MethodSymbol,
            formatSpecifiers As ReadOnlyCollection(Of String))

            MyBase.New(assembly, method.ContainingType.MetadataName, method.MetadataName, formatSpecifiers)
            _method = method
        End Sub

        Public Overrides Function GetCustomTypeInfo(ByRef payload As ReadOnlyCollection(Of Byte)) As Guid
            payload = _method.GetCustomTypeInfoPayload()
            Return If(payload Is Nothing, Nothing, CustomTypeInfo.PayloadTypeId)
        End Function
    End Class

End Namespace
