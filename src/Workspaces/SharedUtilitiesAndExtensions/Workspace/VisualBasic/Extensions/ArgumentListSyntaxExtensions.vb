' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions

    Friend Module ArgumentListSyntaxExtensions
        <Extension()>
        Public Function GetArgumentCount(argumentList As ArgumentListSyntax) As Integer
            Dim count = argumentList.Arguments.Count
            If count = 1 AndAlso argumentList.Arguments.Last().IsMissing AndAlso argumentList.Arguments.SeparatorCount = 0 Then
                count -= 1
            End If

            Return count
        End Function
    End Module
End Namespace
