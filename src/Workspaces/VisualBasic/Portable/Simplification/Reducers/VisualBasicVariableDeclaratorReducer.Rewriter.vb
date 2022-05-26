' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification
    Partial Friend Class VisualBasicVariableDeclaratorReducer
        Private Class Rewriter
            Inherits AbstractReductionRewriter

            Public Sub New(pool As ObjectPool(Of IReductionRewriter))
                MyBase.New(pool)
            End Sub

            Public Overrides Function VisitVariableDeclarator(node As VariableDeclaratorSyntax) As SyntaxNode
                Return SimplifyNode(
                    node,
                    newNode:=MyBase.VisitVariableDeclarator(node),
                    parentNode:=node,
                    simplifyFunc:=s_simplifyVariableDeclarator)
            End Function
        End Class
    End Class
End Namespace
