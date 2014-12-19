' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Globalization
Imports System.Runtime.InteropServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Friend Enum GeneratedNameKind
        None = 0
        HoistedMeField
        HoistedSynthesizedLocalField
        HoistedUserVariableField
        IteratorCurrentField
        IteratorInitialThreadIdField
        IteratorParameterProxyField
        StateMachineAwaiterField
        StateMachineStateField
        StateMachineHoistedUserVariableField
        StaticLocalField
    End Enum

    Partial Friend Class GeneratedNames
        Friend Shared Function GetKind(name As String) As GeneratedNameKind
            If name.StartsWith(StringConstants.HoistedMeName, StringComparison.Ordinal) Then
                Return GeneratedNameKind.HoistedMeField
            ElseIf name.StartsWith(StringConstants.StateMachineStateFieldName, StringComparison.Ordinal) Then
                Return GeneratedNameKind.StateMachineStateField
            ElseIf name.StartsWith(StringConstants.StaticLocalFieldNamePrefix, StringComparison.Ordinal) Then
                Return GeneratedNameKind.StaticLocalField
            ElseIf name.StartsWith(StringConstants.HoistedSynthesizedLocalPrefix, StringComparison.Ordinal) Then
                Return GeneratedNameKind.HoistedSynthesizedLocalField
            ElseIf name.StartsWith(StringConstants.HoistedUserVariablePrefix, StringComparison.Ordinal) Then
                Return GeneratedNameKind.HoistedUserVariableField
            ElseIf name.StartsWith(StringConstants.IteratorCurrentFieldName, StringComparison.Ordinal) Then
                Return GeneratedNameKind.IteratorCurrentField
            ElseIf name.StartsWith(StringConstants.IteratorInitialThreadIdName, StringComparison.Ordinal)
                Return GeneratedNameKind.IteratorInitialThreadIdField
            ElseIf name.StartsWith(StringConstants.IteratorParameterProxyPrefix, StringComparison.Ordinal) Then
                Return GeneratedNameKind.IteratorParameterProxyField
            ElseIf name.StartsWith(StringConstants.StateMachineAwaiterFieldPrefix, StringComparison.Ordinal) Then
                Return GeneratedNameKind.StateMachineAwaiterField
            ElseIf name.StartsWith(StringConstants.StateMachineHoistedUserVariablePrefix, StringComparison.Ordinal) Then
                Return GeneratedNameKind.StateMachineHoistedUserVariableField
            End If

            Return GeneratedNameKind.None
        End Function
    End Class

End Namespace