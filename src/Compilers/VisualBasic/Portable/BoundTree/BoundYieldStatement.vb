﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Partial Class BoundYieldStatement

        ''' <summary>
        ''' Suppresses RValue validation when constructing the node. 
        ''' Must be used _only_ when performing lambda inference where RValue inconsistency on this node is intentionally allowed.
        ''' If such node makes into a regular bound tree it will be eventually rewritten (all Yields are rewritten at some point)
        ''' and that will trigger validation.
        ''' </summary>
        Friend Sub New(syntax As SyntaxNode, expression As BoundExpression, hasErrors As Boolean, returnTypeIsBeingInferred As Boolean)
            MyBase.New(BoundKind.YieldStatement, syntax, hasErrors OrElse expression.NonNullAndHasErrors())

            Debug.Assert(expression IsNot Nothing, "Field 'expression' cannot be null (use Null=""allow"" in BoundNodes.xml to remove this check)")

            Me._Expression = expression

#If DEBUG Then
            If Not returnTypeIsBeingInferred Then
                Validate()
            End If
#End If
        End Sub

#If DEBUG Then
        Private Sub Validate()
            Expression.AssertRValue()
        End Sub
#End If

    End Class

End Namespace
