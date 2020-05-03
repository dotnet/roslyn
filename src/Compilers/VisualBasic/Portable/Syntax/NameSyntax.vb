' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax

    Partial Public Class NameSyntax

        Public ReadOnly Property Arity As Integer
            Get
                If TypeOf Me Is GenericNameSyntax Then
                    Return DirectCast(Me, GenericNameSyntax).TypeArgumentList.Arguments.Count
                End If

                Return 0
            End Get
        End Property

    End Class

End Namespace
