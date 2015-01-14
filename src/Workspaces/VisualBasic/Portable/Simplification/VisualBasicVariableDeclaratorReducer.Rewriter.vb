' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Threading
Imports Microsoft.CodeAnalysis.Options

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification
    Partial Friend Class VisualBasicVariableDeclaratorReducer
        Private Class Rewriter
            Inherits AbstractExpressionRewriter

            Public Sub New(optionSet As OptionSet, cancellationToken As CancellationToken)
                MyBase.New(optionSet, cancellationToken)
            End Sub

            Public Overrides Function VisitVariableDeclarator(node As VariableDeclaratorSyntax) As SyntaxNode
                Return SimplifyNode(
                    node,
                    newNode:=MyBase.VisitVariableDeclarator(node),
                    parentNode:=node,
                    simplifyFunc:=AddressOf SimplifyVariableDeclarator)
            End Function
        End Class
    End Class
End Namespace
