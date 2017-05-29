' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Semantics
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class BoundTypeArray
        Public Overrides Sub Accept(visitor As OperationVisitor)
            Throw New NotImplementedException()
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Throw New NotImplementedException()
        End Function

        Protected Overrides Function ExpressionKind() As OperationKind
            Return CodeAnalysis.OperationKind.TypeArray
        End Function


        'Public Sub New(syntax As SyntaxNode, type As TypeSymbol, Optional hasErrors As Boolean = False)
        '    Me.New(syntax, Nothing, type, hasErrors)
        'End Sub



    End Class
End Namespace
