' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
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
