' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports Microsoft.CodeAnalysis.Syntax.InternalSyntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
    Partial Friend Class BinaryExpressionSyntax
        Implements IBinaryExpressionSyntax

        Private ReadOnly Property IBinaryExpressionSyntax_Left As GreenNode Implements IBinaryExpressionSyntax.Left
            Get
                Return Left
            End Get
        End Property

        Private ReadOnly Property IBinaryExpressionSyntax_OperatorToken As GreenNode Implements IBinaryExpressionSyntax.OperatorToken
            Get
                Return OperatorToken
            End Get
        End Property

        Private ReadOnly Property IBinaryExpressionSyntax_Right As GreenNode Implements IBinaryExpressionSyntax.Right
            Get
                Return Right
            End Get
        End Property

        Public Sub BaseWriteTo(writer As TextWriter, leading As Boolean, trailing As Boolean) Implements IBinaryExpressionSyntax.BaseWriteTo
            MyBase.WriteTo(writer, leading, trailing)
        End Sub

        Protected Overrides Sub WriteTo(writer As TextWriter, leading As Boolean, trailing As Boolean)
            BinaryExpressionSyntaxHelpers.WriteTo(Me, writer, leading, trailing)
        End Sub
    End Class
End Namespace