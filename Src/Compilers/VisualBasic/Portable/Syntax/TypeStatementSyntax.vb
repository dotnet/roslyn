' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax
    Partial Public Class TypeStatementSyntax
        Public ReadOnly Property Arity As Integer
            Get
                Return If(Me.TypeParameterList Is Nothing, 0, Me.TypeParameterList.Parameters.Count)
            End Get
        End Property
    End Class
End Namespace
