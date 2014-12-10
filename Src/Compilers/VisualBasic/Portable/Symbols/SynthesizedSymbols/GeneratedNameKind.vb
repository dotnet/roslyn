' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Globalization
Imports System.Runtime.InteropServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Friend Enum GeneratedNameKind
        None = 0
        AwaiterField
        HoistedLocalField
        HoistedSynthesizedLocalField
    End Enum

    Partial Friend Class GeneratedNames
        Friend Shared Function GetKind(name As String) As GeneratedNameKind
            If name.Contains(StringConstants.StateMachineAwaiterFieldPrefix) Then
                Return GeneratedNameKind.AwaiterField
            ElseIf name.Contains(StringConstants.HoistedSynthesizedLocalPrefix) Then
                Return GeneratedNameKind.HoistedLocalField
            End If

            Return GeneratedNameKind.None
        End Function
    End Class

End Namespace