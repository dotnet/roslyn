' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
